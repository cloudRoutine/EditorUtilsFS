namespace EditorUtilsFS





[<RequireQualifiedAccess>]
module Seq =
    let tryHead s =
        if Seq.isEmpty s then None else Some (Seq.head s)

    let toReadOnlyCollection (xs: _ seq) = ResizeArray(xs).AsReadOnly()



