namespace EditorUtilsFS

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text.Tagging
open System.Threading



[<AbstractClass>]
type AsyncTaggerSource<'Data, 'Tag when 'Tag :> ITag>
            (textBuffer:ITextBuffer, ?textView:ITextView)  =
    
    let changed = Event<EventArgs>()
    
    do 
        if textView.Value = null then
            failwithf "Cannot construct an AsyncTaggerSource from a null textView"
        if textBuffer = null then
            failwithf "Cannot construct an AsyncTaggerSource from a null textBuffer"
            

    member __.TextBuffer with get() = textBuffer
    member __.TextViewOptional with get() = textView

    abstract GetDataForSnapshot : snapshot:ITextSnapshot -> 'Data
    abstract GetTagsInBackground : data:'Data -> span:SnapshotSpan ->
                                        cancellationToken:CancellationToken ->
                                            ReadOnlyCollection<ITagSpan<'Tag>>

    abstract TryGetTagsPrompt : span:SnapshotSpan -> 
                                    tags:IEnumerable<ITagSpan<'Tag>> ref -> 
                                            bool *tags:IEnumerable<ITagSpan<'Tag>> ref
                                    
    default  __.TryGetTagsPrompt (_:SnapshotSpan) (tags:IEnumerable<ITagSpan<'Tag>> ref) =
        tags:=  null
        false, tags
    


    member x.RaiseChanged() =
        changed.Trigger(EventArgs.Empty)


    interface IAsyncTaggerSource<'Data,'Tag> with

        [<CLIEvent>]
        member x.Changed: IEvent<EventArgs> = 
           changed.Publish
    
        member x.Delay with get(): int option = Some Constants.DefaultAsyncDelay
    
        member x.GetDataForSnapshot(snapshot: ITextSnapshot): 'Data = 
            x.GetDataForSnapshot snapshot
    
        member x.GetTagsInBackground(data: 'Data) (span: SnapshotSpan) (cancellationToken: CancellationToken): IReadOnlyCollection<ITagSpan<'Tag>> = 
            x.GetTagsInBackground data span cancellationToken :> IReadOnlyCollection<ITagSpan<'Tag>> 
    
        member x.TextSnapshot: ITextSnapshot = 
            textBuffer.CurrentSnapshot
    
        member x.TextViewOptional: ITextView option = 
            textView
    
        member x.TryGetTagsPrompt(span: SnapshotSpan) (tags: seq<ITagSpan<'Tag>> ref) = 
            x.TryGetTagsPrompt span tags
     
        

