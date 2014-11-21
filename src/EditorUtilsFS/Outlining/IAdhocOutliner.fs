namespace EditorUtilsFS

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open Microsoft.VisualStudio.Text.Tagging
open Microsoft.VisualStudio.Text
open Extensions

[<Struct>][<CustomEquality>][<NoComparison>]
type OutliningRegion =
    val Tag     : OutliningRegionTag
    val Span    : SnapshotSpan
    val Cookie  : int

    new ( tag, span, cookie ) =
        {   Tag     = tag
            Span    = span
            Cookie  = cookie    }

    override x.Equals other =
        match other with
        | :? OutliningRegion as o -> 
            o.Tag.GetHashCode() = x.Tag.GetHashCode()   &&
            o.Span              = x.Span                &&
            o.Cookie            = x.Cookie

        | _ -> false

    override x.GetHashCode() = 
       (pown (hash x.Tag) x.Cookie ) + (pown (hash x.Span) x.Cookie)


type IAdhocOutliner=
    abstract TextBuffer             : ITextBuffer with get
    abstract GetOutliningRegions    : SnapshotSpan -> IReadOnlyCollection<OutliningRegion>
    abstract CreateOutliningRegion  : span:SnapshotSpan -> spanTrackingMode:SpanTrackingMode 
                                        -> text:string -> hint:string -> OutliningRegion
    abstract DeleteOutliningRegion  : cookie:int -> bool
    [<CLIEvent>]
    abstract Changed                : IEvent<EventArgs>

