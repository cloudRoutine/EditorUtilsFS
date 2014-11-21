namespace EditorUtilsFS


open System
open Microsoft.VisualStudio.Text


[<AutoOpen>]
module SpanExtensions =
    type Span with
        
        static member CreateOverarching ( left:Span) (right:Span) =
            let start   = Math.Min( left.Start, right.Start )
            let finish  = Math.Max( left.End  , right.End   )
            Span.FromBounds ( start, finish )


    type SnapshotSpan with 
        
        member x.GetStartLine() =  x.Start.GetContainingLine()
        member x.GetLastLine () =  x.End.GetContainingLine()

        static member CreateOverarching (left:SnapshotSpan)(right:SnapshotSpan) =
            if left.Snapshot <> right.Snapshot then
                failwithf "left Snapshot %A does not equal right Snapshot %A"
                            left                        right
            else
                let span = Span.CreateOverarching (left.Span) (right.Span)
                SnapshotSpan(left.Snapshot, span)

/// <summary>
/// A simple line range 
/// </summary>
[<Struct>]
type LineRange =
    val StartLineNumber   : int
    val Count             : int

    new ( startline, count ) =
        { StartLineNumber   = startline 
          Count             = count     }

    member x.LastLineNumber 
        with get() = x.StartLineNumber + x.Count - 1

    member x.LineNumbers 
        with get() = seq { x.StartLineNumber .. x.Count }

    member x.ContainsLineNumber (lineNumber:int) =
        lineNumber >= x.StartLineNumber &&
        lineNumber <= x.LastLineNumber

    member x.Contains (lineRange:LineRange) =
        x.StartLineNumber <= lineRange.StartLineNumber &&
        x.LastLineNumber  >= lineRange.LastLineNumber
    
    member x.Intersects (lineRange:LineRange) =
        (x.ContainsLineNumber(lineRange.StartLineNumber))||
        (x.ContainsLineNumber(lineRange.LastLineNumber ))||
        (x.LastLineNumber+1 = lineRange.StartLineNumber )||
        (x.StartLineNumber  = lineRange.LastLineNumber+1)

    override x.ToString() = sprintf "[{%A}-{%A}]" x.StartLineNumber x.LastLineNumber

    static member CreateFromBounds startLineNumber lastLineNumber =
        if lastLineNumber < startLineNumber then 
            failwithf "LastLineNumber %A cannot be lower than StartLineNumber %A" 
                          startLineNumber                          lastLineNumber
        else
            let count = (lastLineNumber - startLineNumber) + 1
            LineRange( startLineNumber, count )

    static member CreateOverarching (left:LineRange) (right:LineRange) =
            let startLineNumber = Math.Min(left.StartLineNumber, right.StartLineNumber);
            let lastLineNumber  = Math.Max(left.LastLineNumber, right.LastLineNumber);
            LineRange.CreateFromBounds startLineNumber lastLineNumber

