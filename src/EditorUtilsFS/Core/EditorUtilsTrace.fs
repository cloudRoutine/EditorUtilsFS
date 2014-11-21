namespace EditorUtilsFS
open System
open System.Diagnostics

type EditorUtilsTrace() =
    static let traceSwitch = TraceSwitch("EditorUtilsFS", "EditorUtilsFS Trace")
    
    static do 
        traceSwitch.Level = TraceLevel.Off |> ignore

    static member TraceSwitch with get() = traceSwitch

    [<Conditional("TRACE")>]
    static member TraceInfo (message:string) =
        Trace.WriteLineIf(traceSwitch.TraceInfo, "EditorUtilsFs: " + message)


    [<Conditional("TRACE")>]
    static member TraceInfo (message:string, [<ParamArray>] args: obj[]) =
        Trace.WriteLineIf( traceSwitch.TraceInfo, "EditorUtilsFs: " + String.Format(message,args))
