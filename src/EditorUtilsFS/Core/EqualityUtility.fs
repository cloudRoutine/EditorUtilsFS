namespace EditorUtilsFS


open System.Collections.Generic


type EqualityUtility<'T> ( equalsFunc      : 'T -> 'T -> bool  ,
                            getHashCodeFunc : 'T -> int         ) as self =

    member __.Equals ( x, y )   = equalsFunc x y
    member __.GetHashCode x     = getHashCodeFunc x

    static member Create<'T> ( equalsFunc      : 'T -> 'T -> bool )
                             ( getHashCodeFunc : 'T -> int        ) =
        EqualityUtility<'T>  ( equalsFunc, getHashCodeFunc )

    interface IEqualityComparer<'T> with

        member __.Equals(x:'T, y:'T): bool  = self.Equals(x, y)
        member __.GetHashCode(x: 'T): int   = self.GetHashCode x     




