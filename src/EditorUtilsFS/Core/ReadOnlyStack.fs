namespace EditorUtilsFS


open System
open System.Collections.Generic
open Microsoft.VisualStudio.Text
open Extensions


[<UsedInBackgroundThread>]
type ReadOnlyStack<'T>(lineRange:'T, next: ReadOnlyStack<'T> option) =
     
    new() = ReadOnlyStack<'T>(Unchecked.defaultof<'T>, None)

    static member Empty = ReadOnlyStack<'T>()

    member x.Value with get() = x.ThrowIfEmpty(); lineRange

    member x.Count with get() = if next <> None then next.Value.Count + 1 else 0

    member x.IsEmpty with get() = next = None
    
    member x.ThrowIfEmpty() =
        if x.IsEmpty then raise (Exception(sprintf "ReadOnlyStack< %A > is empty" typeof<'T> ))
    
    member x.Pop() = x.ThrowIfEmpty(); next.Value

    member x.Push (lineRange:'T) =  ReadOnlyStack<'T>(lineRange, Some x)

    member x.GetEnumerator<'T>() =
        // TODO - This probably doesn't work, remember to check here for issues 
        let rec gen (top:ReadOnlyStack<'T>) = 
            seq{   if top.IsEmpty = true then () else
                   yield top.Value 
                   yield! gen (top.Pop())
                }
        (gen x).GetEnumerator() 


    interface IEnumerable<'T> with

        member x.GetEnumerator(): IEnumerator<'T> = 
            x.GetEnumerator()    

        member x.GetEnumerator(): Collections.IEnumerator = 
            x.GetEnumerator()  :> Collections.IEnumerator
