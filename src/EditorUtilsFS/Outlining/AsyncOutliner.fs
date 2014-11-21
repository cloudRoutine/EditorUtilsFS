namespace EditorUtilsFS

open System
open System.Collections.Generic
open System.Linq
open System.Text
open System.ComponentModel.Composition
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text.Outlining
open Microsoft.VisualStudio.Text.Tagging
open Microsoft.VisualStudio.Utilities
open System.Windows.Threading
open System.Threading
open System.Threading.Tasks
open System.Collections.ObjectModel
open Microsoft.VisualStudio.Text.Classification

type AsyncOutliner ()=
    let  x=6