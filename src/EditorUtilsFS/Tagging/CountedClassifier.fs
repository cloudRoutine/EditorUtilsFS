namespace EditorUtilsFS

open System
open System.Collections.Generic
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Utilities
open Microsoft.VisualStudio.Text.Classification


/// <summary>
/// This solves the same problem as CountedTagger but for IClassifier
/// </summary>
type CountedClassifier( propertyCollection:PropertyCollection , key:obj,createFunc:unit-> IClassifier) as self =
//    private readonly CountedValue<IClassifier> _countedValue;
    let _countedValue = CountedValue.GetOrCreate( propertyCollection, key, createFunc)

    member __.Classifier with get() = _countedValue.Value

    member __.Dispose() =   _countedValue.Release()

    interface  IClassifier with
        [<CLIEvent>]
        member x.ClassificationChanged: IEvent<EventHandler<ClassificationChangedEventArgs>,ClassificationChangedEventArgs> = 
            self.Classifier.ClassificationChanged
        
        member x.GetClassificationSpans(span: SnapshotSpan): IList<ClassificationSpan> = 
            self.Classifier.GetClassificationSpans(span)
         

    interface IDisposable with
        member __.Dispose() = self.Dispose()



        

