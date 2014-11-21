namespace EditorUtilsFS


open System


[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Method ||| AttributeTargets.Interface)>]
type UsedInBackgroundThreadAttribute () = 
    inherit Attribute()


