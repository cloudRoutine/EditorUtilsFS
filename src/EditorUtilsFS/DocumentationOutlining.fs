namespace EditorUtilsFS

open System
open System.Collections.Generic
open System.Linq
open System.Text
open System.ComponentModel.Composition
open System.Threading
open Microsoft.VisualStudio.Text.Outlining
open Microsoft.VisualStudio.Text.Tagging
open Microsoft.VisualStudio.Text.Classification
open Microsoft.VisualStudio.Utilities
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open System.Windows.Threading
open System.Windows.Media
open Extensions

module DocumentationOutlining =
    
// #region Collapse
    let startDoc = "///"
    let collapsed = "...."
    let endHide   = "///"
    let tooltip   = "showhovered"

    [<Struct>]
    type Region =
        val StartLine   : int
        val EndLine     : int


        new ( startline, (*startoffset,*) endline ) =
            {   StartLine   = startline
       ///         StartOffset = startoffset
                EndLine     = endline        }
  
  
    let validStarts = [ "///"; "//"; "*)"; "(*";]
    
    
    // #region Functions
    let lineMatch (line:ITextSnapshotLine) start = 
        line.GetText().TrimStart().StartsWith(start)

    let lineMatchAny (line:ITextSnapshotLine) ls =
        List.fold( fun acc start -> 
                       acc || lineMatch line start ) false ls

    let takeBlocks ( lines:seq<ITextSnapshotLine> ) = 
        let rec loop lines =
            seq{if Seq.isEmpty lines then () else 
                let block = lines
                            |> Seq.skipWhile ( fun ln -> not <| lineMatch ln startDoc )
                            |> Seq.takeWhile ( fun ln -> lineMatch ln startDoc )
                let rest = lines |> Seq.skip ( Seq.length block )
                yield block
                yield! loop rest}
        loop lines |> Seq.map(List.ofSeq) |> List.ofSeq


    let buildRegions (regls:ITextSnapshotLine list list) =
        let build (ls:ITextSnapshotLine list) =
            match ls with 
            | [] -> None
            | _  -> let fstline = ls.[0].LineNumber
                    let lstline = ls.[ls.Length-1].LineNumber
                    Some <| Region( fstline, lstline)
        match regls with
        | [] -> []
        | _  -> regls |> List.map build 
                      |> List.filter ( fun x -> x <> None ) 
                      |> List.map Option.get 

    let docRegions = takeBlocks >> buildRegions


    let asSnapshotSpan (region:Region) (snapshot:ITextSnapshot) =
        let startLine   = snapshot.GetLineFromLineNumber(region.StartLine)
        let endLine     = if region.StartLine= region.EndLine 
                            then startLine 
                            else snapshot.GetLineFromLineNumber(region.EndLine)
        SnapshotSpan(startLine.Start, endLine.End)

    //#endregion

    let getTags ( spans     : NormalizedSnapshotSpanCollection  )  
                ( regions   : Region list                       )
                ( snapshot  : ITextSnapshot                     ) : IEnumerable<ITagSpan<IOutliningRegionTag>> = 
        seq {
                if spans.Count = 0 then
                    yield TagSpan(SnapshotSpan(),OutliningRegionTag() :> IOutliningRegionTag) :> ITagSpan<IOutliningRegionTag>

                let currentRegions, currentSnapshot = regions, snapshot
                let snapEntire = SnapshotSpan(spans.[0].Start, spans.[spans.Count-1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive )
                let startLineNum = snapEntire.Start.GetContainingLine().LineNumber
                let endLineNum   = snapEntire.End.GetContainingLine().LineNumber

                for rg in currentRegions do 
                    if rg.StartLine <= endLineNum && rg.EndLine >= startLineNum then
                        let startLine = currentSnapshot.GetLineFromLineNumber(rg.StartLine)
                        let endLine = currentSnapshot.GetLineFromLineNumber(rg.EndLine)
                                    
                        yield TagSpan<IOutliningRegionTag>
                                (   SnapshotSpan(startLine.Start , endLine.End) ,
                                    OutliningRegionTag(false, false, collapsed, tooltip)) :> ITagSpan<IOutliningRegionTag>
            }    



    type taggerDel<'T when 'T :> ITag> = delegate of unit -> ITagger<'T>




    type OutliningTagger(buffer:ITextBuffer) as self =
        
        let _outliner = UtilFactory.CreateOutlinerTagger(buffer)

      //  let subscriptions = ResizeArray<IDisposable>()
        let tagsChanged   = Event<EventHandler<SnapshotSpanEventArgs>,SnapshotSpanEventArgs>() 
       // let mutable textBuffer = buffer
//        do 
//            self.Reparse(buffer.CurrentSnapshot)
//            buffer.Changed.Subscribe(self.BufferChanged) |> self.UnsubscribeOnDispose
        do
           
            buffer.Changed.Add(self.BufferChanged) 
            

//        member __.UnsubscribeOnDispose idisposable =
//            subscriptions.Add idisposable

        member __.ITagger = self :> ITagger<IOutliningRegionTag>

//        member __.Buffer 
//            with get() = textBuffer
//            and  set v = textBuffer <- v

        member val Buffer = buffer with get, set
        member val Snapshot = buffer.CurrentSnapshot with get, set
        member val Regions: Region list  = [] with get, set
        //member val Regions  = List<Region>()

        member __.Reparse() =
            let newSnapshot  = buffer.CurrentSnapshot

// #region oldcode            
//            ( takeBlocks buffer.CurrentSnapshot.Lines
//                |> Seq.map( fun ln ->
//                ln  |> Seq.map string
//                    |> Seq.reduce ( fun a b -> a + ", " + b )))
//                |> Seq.iter( fun x -> debug "Block -- %A"  x  )    
//            
//            debug "Attempting to reparse"
            let newRegions = if newSnapshot <> null then docRegions  newSnapshot.Lines else []
//            debug "Found >>> %A Regions <<<" newRegions.Length

            let oldSpans = self.Regions.Select(fun r -> 
                    (asSnapshotSpan r self.Snapshot).TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive).Span)

            let newSpans = newRegions.Select(fun r -> 
                    (asSnapshotSpan r newSnapshot).Span)

            let oldSpanCollection = NormalizedSpanCollection oldSpans
            let newSpanCollection = NormalizedSpanCollection newSpans
            let removed = NormalizedSpanCollection.Difference( oldSpanCollection, newSpanCollection )

            let changeStart = if removed.Count  > 0 then removed.[0].Start               else Int32.MaxValue
            let changeEnd   = if removed.Count  > 0 then removed.[removed.Count-1].End   else -1

            let changeStart'= if newSpans.Count() > 0 then 
                                Math.Min(changeStart, newSpans.ElementAt(0).Start ) else changeStart
            let changeEnd'  = if newSpans.Count() > 0 then 
                                Math.Max(changeEnd  , newSpans.ElementAt(newSpans.Count()-1).End ) else changeEnd

            self.Snapshot <- newSnapshot
            self.Regions  <- newRegions
            let eventSpan = SnapshotSpan( self.Snapshot, Span.FromBounds(changeStart',changeEnd'))

            if changeStart' <= changeEnd' then
                tagsChanged.Trigger(self, SnapshotSpanEventArgs eventSpan )


        member __.BufferChanged( args:TextContentChangedEventArgs  ) =
            if args.After <> self.Snapshot then 
                self.Reparse()
// #endregion

//        interface IDisposable with
//            member x.Dispose(): unit = 
//                subscriptions |> Seq.iter (fun x -> x.Dispose())
//                subscriptions.Clear()

        //interface ITagger<IOutliningRegionTag> with
        interface ITagger<IOutliningRegionTag> with

            member x.GetTags ( spans: NormalizedSnapshotSpanCollection): IEnumerable<ITagSpan<IOutliningRegionTag>> = 
                //_outliner.GetTags(spans)
                getTags spans self.Regions self.Snapshot
//                seq {
//                        debug "trying to yield tags"
//                        if spans.Count = 0 then
//                            yield TagSpan(SnapshotSpan(),OutliningRegionTag() :> IOutliningRegionTag) :> ITagSpan<IOutliningRegionTag>
//
//                        let currentRegions, currentSnapshot = self.Regions, self.Snapshot
//                        let snapEntire = SnapshotSpan(spans.[0].Start, spans.[spans.Count-1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive )
//                        let startLineNum = snapEntire.Start.GetContainingLine().LineNumber
//                        let endLineNum   = snapEntire.End.GetContainingLine().LineNumber
//
//                        for rg in currentRegions do 
//                            if rg.StartLine <= endLineNum && rg.EndLine >= startLineNum then
//                                let startLine = currentSnapshot.GetLineFromLineNumber(rg.StartLine)
//                                let endLine = currentSnapshot.GetLineFromLineNumber(rg.EndLine)
//                                    
//                                yield TagSpan<IOutliningRegionTag>
//                                        (   SnapshotSpan(startLine.Start , endLine.End) ,
//                                            OutliningRegionTag(false, false, collapsed, tooltip)) :> ITagSpan<IOutliningRegionTag>
//                    }

            
            [<CLIEvent>]
            member x.TagsChanged: IEvent<EventHandler<SnapshotSpanEventArgs>,SnapshotSpanEventArgs> = 
                tagsChanged.Publish
  
// #endregion
        
    [<Export(typeof<ITaggerProvider>)>]
    [<TagType(typeof<IOutliningRegionTag>)>]
    [<ContentType("F#")>]
    type OutliningTaggerProvider   () =
        interface ITaggerProvider with

            //member x.CreateTagger<'T when 'T :> ITag>(buffer: ITextBuffer): ITagger<'T> = 
            member __.CreateTagger(buffer: ITextBuffer) = 
//                debug "trying to create tagger"
                // buffer.Properties.GetOrCreateSingletonProperty<ITagger<_>>(sc)
                // UtilFactory.CreateOutlinerTagger(buffer) :?> ITagger<_>
                // OutliningTagger(buffer) :?> ITagger<_>
                let sc = Func<ITagger<'T>>( fun () -> new OutliningTagger(buffer) :> obj :?> ITagger<_> )
                buffer.Properties.GetOrCreateSingletonProperty<ITagger<_>>(sc)
            

    [<Export(typeof<EditorFormatDefinition>)>]
    [<Name("Cats")>]
    [<UserVisible(true)>]
    type CatTaggerFormat() =
        inherit  MarkerFormatDefinition()
            member __.DisplayName = "Cats"
            member __.BackgroundColor = Colors.Orange



    type BackgroundWorkerCat (cancellationToken:CancellationToken) as self =
        let _word = "cat"
        let _cancellationToken = cancellationToken

        member __.IsWord( point : SnapshotPoint ) =
            let snapshot = point.Snapshot;
            
            let rec loop cnt =
                if cnt + point.Position < snapshot.Length && cnt < _word.Length then
                    if (snapshot.GetChar(cnt + point.Position) <> _word.[cnt]) then false else

                    if (cnt < snapshot.Length && Char.IsLetter(snapshot.GetChar(cnt + point.Position))) then
                        false
                    else
                        true
                else 
                    loop (cnt+1)
            loop 0
        
        member __.AddWordsOnLine(tags:List<ITagSpan<TextMarkerTag>>, snapshotLine:ITextSnapshotLine ) =
            let snapshot = snapshotLine.Snapshot
            let tag = new TextMarkerTag("Cats")

            let rec loop cnt =
                if cnt < snapshotLine.Length then
                    //debug "Looking for cats"
                    let point = snapshot.GetPoint(cnt + snapshotLine.Start.Position)
                    if (self.IsWord(point)) then
                        let span = new SnapshotSpan(snapshot, snapshotLine.Start.Position + cnt, _word.Length);
                        tags.Add(new TagSpan<TextMarkerTag>(span, tag));
                        loop  (cnt + _word.Length)
                    else
                        loop (cnt+1)
            loop 0

        member __.GetTags(span:SnapshotSpan) =
            let tags = new List<ITagSpan<TextMarkerTag>>()
            let lineRange = SnapshotLineRange.CreateForSpan(span);
           // debug "Cat Version %A, Lines %A - %A" span.Snapshot.Version.VersionNumber lineRange.StartLineNumber lineRange.LastLineNumber

            for snapshotLine in lineRange.Lines do 
                self.AddWordsOnLine(tags, snapshotLine)
                _cancellationToken.ThrowIfCancellationRequested()

                // Cats need naps
       //     debug "Cat Nap Time"
            Thread.Sleep(TimeSpan.FromSeconds(0.10))
           // debug "Cat Wake Up"

            tags.ToReadOnlyCollectionShallow();


    type CatTagger(textView:ITextView) =
        inherit AsyncTaggerSource<string, TextMarkerTag>(textView.TextBuffer )

        member __.TryGetTagsPrompt( span:SnapshotSpan, tags:IEnumerable<ITagSpan<TextMarkerTag>>ref  ) =
            tags := null;
            false

        override __.GetDataForSnapshot( snapshot:ITextSnapshot) =
            String.Empty

        override __.GetTagsInBackground( data:string) (span:SnapshotSpan) (cancellationToken:CancellationToken ) =
            let backgroundWorker = new BackgroundWorkerCat(cancellationToken)
            backgroundWorker.GetTags(span)


    [<Export(typeof<IViewTaggerProvider>)>]
    [<ContentType("F#")>]
    [<TextViewRole(PredefinedTextViewRoles.Document)>]
    [<TagType(typeof<TextMarkerTag>)>]

    type CatTaggerProvider () =
        let _key = obj()

        //[ImportingConstructor]

        interface IViewTaggerProvider with
            member x.CreateTagger(textView: ITextView, buffer: ITextBuffer): ITagger<'T> = 
                if textView.TextBuffer <> buffer then
                    null
                else
                    let tagger = 
                        UtilFactory.CreateTagger<string, TextMarkerTag>(
                            textView.Properties,
                            _key,
                            (fun () -> CatTagger(textView) :> IAsyncTaggerSource<string, TextMarkerTag>)
                        )
                    tagger :?> ITagger<'T>

       

   




            



