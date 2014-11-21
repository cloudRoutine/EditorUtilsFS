namespace EditorUtilsFS

open Microsoft.VisualStudio.Text.Operations;


    type IBasicUndoHistory =
        inherit ITextUndoHistory
        /// <summary>
        /// Clear out all of the state including the undo and redo stacks
        /// </summary>
        abstract Clear : unit

    /// <summary>
    /// In certain hosted scenarios the default ITextUndoHistoryRegistry won't be 
    /// available.  This is a necessary part of editor composition though and some 
    /// implementation needs to be provided.  Importing this type will provide a 
    /// very basic implementation
    ///
    /// This type intentionally doesn't ever export ITextUndoHistoryRegistry.  Doing
    /// this would conflict with Visual Studios export and cause a MEF composition 
    /// error.  It's instead exposed via this interface 
    ///
    /// In general this type won't be used except in testing
    /// </summary>
    type IBasicUndoHistoryRegistry =
        /// <summary>
        /// Get the basic implementation of the ITextUndoHistoryRegistry
        /// </summary>
        abstract TextUndoHistoryRegistry : ITextUndoHistoryRegistry with get

        /// <summary>
        /// Try and get the IBasicUndoHistory for the given context
        /// </summary>
        // TODO - in editor utils this function takes a IBasicUndoHistory ref as an out, not sure if that
        // strategy should be kept or if returning a tuple is fine
        abstract TryGetBasicUndoHistory : context:obj -> basicUndoHistory:IBasicUndoHistory -> bool * IBasicUndoHistory 


