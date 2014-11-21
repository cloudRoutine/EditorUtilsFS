namespace EditorUtilsFS

open System
open System.Collections.Generic
open System.Linq
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Tagging
open System.Collections.ObjectModel
open Extensions
  

[<Struct>][<NoComparison>] 
type TrackingCacheData<'Tag when 'Tag :> ITag> =
        
        val  TrackingSpan : ITrackingSpan  
        val  TrackingList : ReadOnlyCollection<Tuple<ITrackingSpan,'Tag>> 

        new ( trackingSpan,  trackingList ) =
            {   TrackingSpan = trackingSpan
                TrackingList = trackingList  }

        member self.Merge( snapshot:ITextSnapshot,  trackingCacheData:TrackingCacheData<'Tag> ) =
            let left  = self.TrackingSpan.GetSpanSafe(snapshot)
            let right = trackingCacheData.TrackingSpan.GetSpanSafe(snapshot)
       
            let span = 
                match left, right with
                | Some l, Some r -> SnapshotSpan.CreateOverarching l r
                | Some l, None   -> l
                | None  , Some r -> r
                | None  , None   -> SnapshotSpan(snapshot, 0, 0)

            let trackingSpan = 
                snapshot.CreateTrackingSpan(span.Span, SpanTrackingMode.EdgeInclusive )
            
            let equalityTool = 
                EqualityUtility.Create<Tuple<ITrackingSpan, 'Tag>>
                    ( fun (x:Tuple<ITrackingSpan, 'Tag>) 
                          (y:Tuple<ITrackingSpan, 'Tag>)   -> 
                                x.Item1.GetSpanSafe(snapshot) = y.Item1.GetSpanSafe(snapshot))
                    ( fun tuple -> tuple.Item1.GetSpanSafe(snapshot).GetHashCode())
                :> IEqualityComparer<Tuple<ITrackingSpan, 'Tag>>                     

            let tagList =
                self.TrackingList 
                |> ReadOnlyCollection.concat<Tuple<ITrackingSpan, 'Tag>>   
                                                   ( trackingCacheData.TrackingList )
                |> IEnumerable.distinct<Tuple<ITrackingSpan, 'Tag>>  ( equalityTool )
                |> Seq.toReadOnlyCollection

            TrackingCacheData( trackingSpan, tagList )


        /// <summary>
        /// Does this tracking information contain tags over the given span in it's 
        /// ITextSnapshot
        /// </summary>
        member self.ContainsCachedTags(span:SnapshotSpan ) : bool =
                let snapshot     = span.Snapshot;
                let trackingSpan = self.TrackingSpan.GetSpanSafe(snapshot);
                trackingSpan.IsSome

        /// <summary>
        /// Get the cached tags on the given ITextSnapshot
        ///
        /// If this SnapshotSpan is coming from a different snapshot which is ahead of 
        /// our current one we need to take special steps.  If we simply return nothing
        /// and go to the background the tags will flicker on screen.  
        ///
        /// To work around this we try to map the tags to the requested ITextSnapshot. If
        /// it succeeds then we use the mapped values and simultaneously kick off a background
        /// request for the correct ones
        /// </summary>
        member self.GetCachedTags (snapshot:ITextSnapshot) : ReadOnlyCollection<ITagSpan<'Tag>> =
            self.TrackingList
            |> ReadOnlyCollection.map
                ( fun tuple ->  
                    let itemSpan = tuple.Item1.GetSpanSafe(snapshot)
                    if itemSpan.IsSome 
                    then Some <| TagSpan<'Tag>( itemSpan.Value, tuple.Item2 ) 
                    else None )
            |> IEnumerable.filter ( fun tagSpan -> tagSpan <> None )
            |> IEnumerable.map    ( fun tagSpan -> tagSpan.Value :> ITagSpan<'Tag>  )
            |> IEnumerable.toReadOnlyCollection


type TagCacheState =  Empty | Partial | Complete

[<Struct>][<NoComparison>]
/// <summary>
/// This holds the set of data which is currently known from the background thread.  Data in 
/// this collection should be considered final for the given Snapshot.  It will only change
/// if the AsyncTaggerSource itself raises a Changed event (in which case we discard all 
/// background data).  
/// </summary>
type BackgroundCacheData<'Tag when 'Tag :> ITag> =
    val Snapshot : ITextSnapshot
    /// <summary>
    /// Set of line ranges for which tags are known
    /// </summary>
    val VisitedCollection : NormalizedLineRangeCollection
    /// <summary>
    /// Set of known tags
    /// </summary>
    val TagList : ReadOnlyCollection<ITagSpan<'Tag>> 

    new ( snapshot, visitedCollection, tagList ) =
        {   Snapshot            = snapshot
            VisitedCollection   = visitedCollection
            TagList             = tagList           }

    new ( lineRange:SnapshotLineRange, tagList ) =
        let snapshot = lineRange.Snapshot
        let visitedCollection = NormalizedLineRangeCollection()
        visitedCollection.Add lineRange.LineRange
        BackgroundCacheData<'Tag>(snapshot, visitedCollection, tagList )

    member self.Span 
        with get() =
            let range = self.VisitedCollection.OverarchingLineRange
            if range.IsNone then SnapshotSpan( self.Snapshot, 0, 0 ) else
            let lineRange = SnapshotLineRange( self.Snapshot                , 
                                               range.Value.StartLineNumber  , 
                                               range.Value.Count            )
            lineRange.ExtentIncludingLineBreak

    /// <summary>
    /// Determine tag cache state we have for the given SnapshotSpan
    /// </summary>
    member self.GetTagCacheState (span:SnapshotSpan) =
        // If the requested span doesn't even intersect with the overarching SnapshotSpan
        // of the cached data in the background then a more exhaustive search isn't needed
        // at this time
        let cachedSpan = self.Span
        if   not <| cachedSpan.IntersectsWith span 
        then TagCacheState.Empty 
        else 
            let lineRange = SnapshotLineRange.CreateForSpan span
            let unvisited = self.VisitedCollection.GetUnvisited lineRange.LineRange
            if   unvisited.IsSome 
            then TagCacheState.Partial 
            else TagCacheState.Complete
      
        
    /// <summary>
    /// Create a TrackingCacheData instance from this BackgroundCacheData
    /// </summary>
    member self.CreateTrackingCacheData() =
        let trackingList:ReadOnlyCollection<Tuple<ITrackingSpan, 'Tag>> = 
            self.TagList.Select ( fun tagSpan ->
                let snapshot     = tagSpan.Span.Snapshot
                let trackingSpan = snapshot.CreateTrackingSpan( tagSpan.Span.Span, SpanTrackingMode.EdgeExclusive )
                Tuple<ITrackingSpan, 'Tag>(trackingSpan, tagSpan.Tag) )
                |> IEnumerable.toReadOnlyCollection
        TrackingCacheData( self.Snapshot.CreateTrackingSpan( self.Span.Span , SpanTrackingMode.EdgeInclusive), trackingList )



[<Struct; NoComparison>]
type TagCache<'Tag when 'Tag :> ITag> =
    val BackgroundCacheData : BackgroundCacheData<'Tag> option
    val TrackingCacheData   : TrackingCacheData<'Tag>   option

    new ( backgroundCacheData, trackingCacheData ) =
        {   BackgroundCacheData = backgroundCacheData 
            TrackingCacheData   = trackingCacheData     }

    member x.IsEmpty 
        with get() =
            x.BackgroundCacheData.IsNone && x.TrackingCacheData.IsNone

    static member Empty = TagCache<'Tag>(None,None)

