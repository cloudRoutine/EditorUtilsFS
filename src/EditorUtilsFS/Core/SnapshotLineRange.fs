namespace EditorUtilsFS


open System
open System.Collections.Generic
open System.Linq
open Microsoft.VisualStudio.Text

/// <summary>
/// Represents a range of lines in an ITextSnapshot.  Different from a SnapshotSpan
/// because it declaratively supports lines instead of a position range
/// </summary>
[<Struct; CustomEquality; NoComparison >]
type SnapshotLineRange =
    val Snapshot        : ITextSnapshot
    val StartLineNumber : int
    val Count           : int

    new ( snapshot, startline, count ) =
        {   Snapshot        = snapshot
            StartLineNumber = startline 
            Count           = count      }
        then 
            if startline >= snapshot.LineCount then 
                failwithf "The startline %A cannot be greater than the lenght of the Snapshot %A" 
                                    startline                                 snapshot.LineCount

    member x.Startline 
        with get() = x.Snapshot.GetLineFromLineNumber x.StartLineNumber

    member x.Start 
        with get() = x.Startline.Start

    member x.LastLineNumber 
        with get() = x.StartLineNumber + x.Count - 1 

    member x.LastLine
        with get() = x.Snapshot.GetLineFromLineNumber x.LastLineNumber

    member x.LineRange 
        with get() = LineRange()

    member x.End 
        with get() = x.LastLine.End

    member x.EndIncludingLineBreak 
        with get() = x.LastLine.EndIncludingLineBreak

    member x.Extent
        with get() = SnapshotSpan( x.Start, x.End )

    member x.ExtentIncludingLineBreak
        with get() = SnapshotSpan( x.Start, x.EndIncludingLineBreak)

    member x.Lines 
        with get() = seq{ x.StartLineNumber .. x.Count } |> Seq.map x.Snapshot.GetLineFromLineNumber

    member x.GetText() = x.Extent.GetText()

    member x.GetTextIncludingLineBreak() = x.ExtentIncludingLineBreak.GetText()

    override x.GetHashCode() = pown x.StartLineNumber x.Count
    
    override x.Equals other =
        match other with 
        | :? SnapshotLineRange as o ->  
                o.Snapshot          = x.Snapshot        &&
                o.StartLineNumber   = x.StartLineNumber &&
                o.Count             = x.Count
        | _ -> false
    
    override x.ToString() = sprintf "[{%A}-{%A}] %A" x.StartLineNumber x.LastLineNumber x.Snapshot
 
    /// <summary>
    /// Create for a single ITextSnapshotLine
    /// </summary>
    static member CreateForLine (snapshotLine:ITextSnapshotLine)  =
        SnapshotLineRange( snapshotLine.Snapshot, snapshotLine.LineNumber, 1 )
 
    /// <summary>
    /// Create for the entire ITextSnapshot
    /// </summary>                                    
    static member CreateForExtent (snapshot:ITextSnapshot) =
        SnapshotLineRange( snapshot, 0, snapshot.LineCount )                                     
 
    /// <summary>
    /// Create a SnapshotLineRange which includes the 2 lines
    /// </summary>
    static member CreateForLineRange (startLine:ITextSnapshotLine) (lastLine:ITextSnapshotLine) =
        if startLine.Snapshot <> lastLine.Snapshot then
            failwithf "The snapshot of %A does not equal the snapshot of %A" 
                startLine.Snapshot  lastLine.Snapshot
        else
            let count = lastLine.LineNumber - startLine.LineNumber + 1
            SnapshotLineRange( startLine.Snapshot, startLine.LineNumber, count )


    static member CreateForSpan (span:SnapshotSpan) =
        let startLine = span.GetStartLine() 
        let lastLine  = span.GetLastLine()
        SnapshotLineRange.CreateForLineRange startLine lastLine 

    /// <summary>
    /// Create a range for the provided ITextSnapshotLine and with at most count 
    /// length.  If count pushes the range past the end of the buffer then the 
    /// span will go to the end of the buffer
    /// </summary>
    static member CreateForLineAndMaxCount (snapshotLine:ITextSnapshotLine) (count:int) =
        let maxCount = snapshotLine.Snapshot.LineCount - snapshotLine.LineNumber
        let count' = Math.Min(count,maxCount)
        SnapshotLineRange(snapshotLine.Snapshot, snapshotLine.LineNumber, count')



    /// <summary>
    /// Create a SnapshotLineRange which includes the 2 lines
    /// </summary>
    static member CreateForLineNumberRange (snapshot:ITextSnapshot) (startLine:int) (lastLine:int) =
        if startLine >= lastLine then
            failwithf "The startline[%A] cannot be greater than the endline[%A]" 
                            startLine                               lastLine
        elif startLine >= snapshot.LineCount || lastLine >= snapshot.LineCount then None else
        Some <| SnapshotLineRange(snapshot, startLine, (lastLine - startLine) + 1)



