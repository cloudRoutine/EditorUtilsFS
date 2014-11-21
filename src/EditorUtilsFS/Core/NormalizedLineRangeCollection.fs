namespace EditorUtilsFS


open System
open System.Collections.Generic
open System.Linq


/// <summary>
/// The goal of this collection is to efficiently track the set of LineRange values that have 
/// been visited for a given larger LineRange.  The order in which, or original granualarity
/// of visits is less important than the overall range which is visited.  
/// 
/// For example if both ranges 1-3 and 2-5 are visited then the collection will only record
/// that 1-5 is visited. 
/// </summary>
type NormalizedLineRangeCollection( visited:IEnumerable<LineRange>) as self =
    
    let list = List<LineRange>()

    do  
        visited |> Seq.iter ( fun lineRange -> list.Add lineRange )

    new() = NormalizedLineRangeCollection(Enumerable.Empty())

    member __.List with get() = list

    member __.FindInsertionPoint (startLineNumber:int) =
        let rec loop cnt =
            if cnt < list.Count then 
                match startLineNumber with
                | x when x <= list.[cnt].StartLineNumber -> cnt
                | x  -> loop cnt+1
            else -1
        loop 0

    /// <summary>
    /// This is the helper method for Add which will now collapse elements that intersect.   We only have
    /// to look at the item before the insert and all items after and not the entire collection.
    /// </summary>
    member __.CollapseIntersecting (index:int) =
            // It's possible this new LineRange actually intersects with the LineRange before
            // the insertion point.  LineRange values are ordered by start line.  Hence the LineRange
            // before could have an extent which intersects the new LineRange but not the previous 
            // LineRange at this index.  Do a quick check for this and if it's true just start the
            // collapse one index backwards
            let lineRange = list.[index]
            if index > 0 && list.[index - 1].Intersects(lineRange) then
                self.CollapseIntersecting(index - 1)

            let current = index + 1
            let removeCount = 0

            let rec loop current removeCount =
                if current < list.Count then
                    let currentLineRange = list.[current]
                    if not <| lineRange.Intersects currentLineRange then ()
                    else 
                        let lineRange' = LineRange.CreateOverarching lineRange currentLineRange
                        list.[index] <- lineRange'
                        loop (current+1) (removeCount+1)
            loop current removeCount

            if (removeCount > 0) then list.RemoveRange(index + 1, removeCount)


    member __.Add(lineRange:LineRange) =
        let index = self.FindInsertionPoint lineRange.StartLineNumber
        if index = 1 then 
            // Just insert at the end and let the collapse code do the work in this case 
            list.Add lineRange 
            self.CollapseIntersecting (list.Count-1)
        else
            // Quick optimization check to avoid copying the contents of the List
            // structure down on insert
            let item = list.[index]

            if item.StartLineNumber = lineRange.StartLineNumber     ||
               lineRange.ContainsLineNumber(item.StartLineNumber) then
                    list.[index] <- LineRange.CreateOverarching item lineRange
            else
                list.Insert(index, lineRange)
                self.CollapseIntersecting(index)


    member __.OverarchingLineRange 
        with get() = 
            if   list.Count = 0 then None
            elif list.Count = 1 then Some list.[0]
            else 
                let startLine = list.[0].StartLineNumber
                let lastLine = list.[list.Count-1].LastLineNumber
                Some <| LineRange.CreateFromBounds startLine lastLine
    
    member __.Count with get() = list.Count
    
    member __.Item with get(i:int) = list.[i]

    member __.Contains (lineRange:LineRange) =
        list.Any(fun current -> current.Contains lineRange )

    member __.Clear() = list.Clear()

    member __.Copy() = NormalizedLineRangeCollection(list)

    member __.GetUnvisited(lineRange:LineRange) =
        let rec loop cnt =
            if cnt > list.Count then Some lineRange else
            match list.[cnt] with
            | elm when not<|elm.Intersects lineRange -> loop (cnt+1) 
            | elm when elm.Contains lineRange        -> None
            | elm when elm.StartLineNumber 
                    <= lineRange.StartLineNumber ->
                        Some <| LineRange.CreateFromBounds (elm.LastLineNumber + 1  ) 
                                                           (lineRange.LastLineNumber)
            | elm when elm.StartLineNumber 
                    >  lineRange.StartLineNumber ->
                        Some <| LineRange.CreateFromBounds (lineRange.StartLineNumber)
                                                           (elm.StartLineNumber - 1  ) 
            | _ -> loop (cnt+1 )
        loop 0

    interface IEnumerable<LineRange> with

        member x.GetEnumerator(): Collections.IEnumerator = 
            list.GetEnumerator() :> Collections.IEnumerator
        
        member x.GetEnumerator(): IEnumerator<LineRange> = 
            list.GetEnumerator() :> IEnumerator<LineRange>
