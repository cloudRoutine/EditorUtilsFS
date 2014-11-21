namespace EditorUtilsFS

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text.Tagging
open System.Threading

/// <summary>
/// A tagger source for asynchronous taggers.  This interface is consumed from multiple threads
/// and each method which is called on the background thread is labelled as such
/// be called on any thread
/// </summary>
type IAsyncTaggerSource<'Data,'Tag when 'Tag :> ITag> =
    
    /// <summary>
    /// Delay in milliseconds which should occur between the call to GetTags and the kicking off
    /// of a background task
    /// </summary>
    abstract Delay              : int option        with get

    /// <summary>
    /// The current Snapshot.  
    /// Called from the main thread only
    /// </summary>
    abstract TextSnapshot       : ITextSnapshot     with get

    /// <summary>
    /// The current ITextView if this tagger is attached to a ITextView.  This is an optional
    /// value
    ///
    /// Called from the main thread only
    /// </summary>
    abstract TextViewOptional   : ITextView option
    /// <summary>
    /// This method is called to gather data on the UI thread which will then be passed
    /// down to the background thread for processing
    ///
    /// Called from the main thread only
    /// </summary>
    abstract GetDataForSnapshot : snapshot:ITextSnapshot -> 'Data


    [<UsedInBackgroundThread>]
    /// <summary>
    /// Return the applicable tags for the given SnapshotSpan instance.  This will be
    /// called on a background thread and should respect the provided CancellationToken
    ///
    /// Called from the background thread only
    /// </summary>
    abstract GetTagsInBackground: data:'Data -> span:SnapshotSpan -> cancellationToken:CancellationToken -> IReadOnlyCollection<ITagSpan<'Tag>>

    /// <summary>
    /// To prevent needless spawning of Task<T> values the async tagger has the option
    /// of providing prompt data.  This method should only be used when determination
    /// of the tokens requires no calculation.
    ///
    /// Called from the main thread only
    /// <summary>
    abstract TryGetTagsPrompt   : span:SnapshotSpan -> seq<ITagSpan<'Tag>> ref -> bool * seq<ITagSpan<'Tag>> ref
    
    [<CLIEvent>]
    /// <summary>
    /// Raised by the source when the underlying source has changed.  All previously
    /// provided data should be considered incorrect after this event
    /// </summary>
    abstract Changed            : IEvent<EventArgs>
