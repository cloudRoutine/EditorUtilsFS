namespace EditorUtilsFS

open System
open System.Collections.Generic
open System.Linq
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Tagging
open Microsoft.VisualStudio.Text.Classification


type Classifier(tagger:ITagger<IClassificationTag> ) as self =
    
    let _subscriptions = List<IDisposable>()

    let _tagger = tagger

    let _classificationChanged = Event<EventHandler<ClassificationChangedEventArgs>,ClassificationChangedEventArgs>() 
    
    do _tagger.TagsChanged.Subscribe(self.OnTagsChanged) |> _subscriptions.Add
     
    member this.ClassificationChanged = _classificationChanged.Publish
            
    member __.OnTagsChanged( args ) =
        _classificationChanged.Trigger(self, new ClassificationChangedEventArgs( args.Span))

    member __.Dispose() =
        _subscriptions.ForEach ( fun x -> x.Dispose() ) 
        _subscriptions.Clear()
        

        if _tagger <> null then (_tagger :?> IDisposable).Dispose()

    interface IClassifier with
        [<CLIEvent>]
        member x.ClassificationChanged: IEvent<EventHandler<ClassificationChangedEventArgs>,ClassificationChangedEventArgs> = 
            self.ClassificationChanged
        
        member x.GetClassificationSpans(span: SnapshotSpan): IList<ClassificationSpan> = 
            _tagger.GetTags(
                NormalizedSnapshotSpanCollection span).Select( 
                    fun x -> ClassificationSpan(x.Span, x.Tag.ClassificationType)).ToList() :> IList<_>

    interface IDisposable with 
        member __.Dispose() =
            self.Dispose();

