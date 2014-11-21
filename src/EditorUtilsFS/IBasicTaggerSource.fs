namespace EditorUtilsFS


open System
open System.Collections.ObjectModel
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Tagging


type IBasicTaggerSource<'Tag when 'Tag :> ITag> =
    abstract GetTags : span:SnapshotSpan -> ReadOnlyCollection<ITagSpan<'Tag>>
    abstract Changed : IEvent<EventHandler<SnapshotSpanEventArgs>,SnapshotSpanEventArgs>
