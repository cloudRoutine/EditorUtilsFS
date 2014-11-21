namespace EditorUtilsFS

open System
open System.Collections.Generic
open System.Linq
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text.Outlining
open Microsoft.VisualStudio.Text.Tagging
open Microsoft.VisualStudio.Utilities
open System.Windows.Threading
open System.Threading
open System.Threading.Tasks
open System.Collections.ObjectModel
open Extensions
  

/// <summary>
/// Need another type here because SnapshotLineRange is a struct and we need atomic assignment
/// guarantees to use Interlocked.Exchange
/// </summary>
type TextViewLineRange (lineRange:SnapshotLineRange) =
    
    member __.LineRange with get() = lineRange

    static member Empty = TextViewLineRange(SnapshotLineRange())



/// <summary>
/// This type is used to support the one way transfer of SnapshotLineRange values between
/// the foreground thread of the tagger and the background processing thread.  It understands
/// the priority placed on the visible UI lines and will transfer those lines at a higher
/// priority than normal requests
/// </summary>
type Channel() as self =

    /// <summary>
    /// This is the normal request stack from the main thread.  More recently requested items
    /// are given higher priority than older items
    /// </summary>
    let mutable _stack = ReadOnlyStack<SnapshotLineRange>.Empty

    /// <summary>
    /// When set this is represents the visible line range of the text view.  It has the highest
    /// priority for the background thread
    /// </summary>
    let mutable _textViewLineRange = TextViewLineRange.Empty

    /// <summary>
    /// Version number tracks the number of writes to the channel
    /// </summary>
    let mutable _version = 0

    /// <summary>
    /// The current state of the request stack
    /// </summary>
    member __.CurrentStack with get() = _stack

    /// <summary>
    /// This number is incremented after every write to the channel.  It is a hueristic only and 
    /// not an absolute indicator.  It is not set atomically with every write but instead occurs
    /// some time after the write.  
    /// </summary>
    member __.CurrentVersion with get() = _version


    member __.WriteVisibleLines (lineRange:SnapshotLineRange) =
        let textViewLineRange = TextViewLineRange(lineRange)
        _textViewLineRange <- Interlocked.Exchange(ref _textViewLineRange, textViewLineRange)
        // TODO unsure if this is correct
        //Interlocked.Exchange(_textViewLineRange, textViewLineRange) |> ignore

        _version <- Interlocked.Increment(ref _version)
        // TODO unsure whether to reassign result or to ignore it
        // Interlocked.Increment(_version) |> ignore


    member __.WriteNormal (lineRange:SnapshotLineRange) =
        let compareStacks() =
            let oldStack = _stack
            let newStack = _stack.Push(lineRange)
            oldStack = Interlocked.CompareExchange(ref _stack, newStack, oldStack)

        let rec loop success =
            if   success  = true then () else
            let  success' = compareStacks()
            loop success'

        loop <| compareStacks()
        _version <- Interlocked.Increment(ref _version)

    member __.ReadVisibleLines() =
        let rec readLoop() =
            let oldTextViewLineRange = _textViewLineRange
            if  oldTextViewLineRange = TextViewLineRange.Empty then None else
            let success = 
                oldTextViewLineRange = Interlocked.CompareExchange( ref _textViewLineRange  , 
                                                                    TextViewLineRange.Empty , 
                                                                    oldTextViewLineRange    )
            if success = true then Some oldTextViewLineRange.LineRange
            else readLoop()
        readLoop()  


    member __.ReadNormal() =
        let rec readNormalLoop() =
            let oldStack = _stack
            if  oldStack =  ReadOnlyStack<SnapshotLineRange>.Empty then None else
            let newStack = _stack.Pop()
            let success  =
                oldStack = Interlocked.CompareExchange( ref _stack, newStack, oldStack )
            if success = true then Some oldStack.Value
            else readNormalLoop()
        readNormalLoop()



    member __.Read() =
        let lineRange = self.ReadVisibleLines()
        if   lineRange.IsSome then lineRange                
        else self.ReadNormal()
