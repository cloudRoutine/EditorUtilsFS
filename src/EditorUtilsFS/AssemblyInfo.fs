﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("EditorUtilsFS")>]
[<assembly: AssemblyProductAttribute("EditorUtilsFS")>]
[<assembly: AssemblyDescriptionAttribute("A utility library to aid building VSIX Extensions with F#")>]
[<assembly: AssemblyVersionAttribute("0.01")>]
[<assembly: AssemblyFileVersionAttribute("0.01")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.01"
