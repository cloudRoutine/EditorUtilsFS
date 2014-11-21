namespace EditorUtilsFS


open Microsoft.VisualStudio.Utilities
open Extensions



/// <summary>
/// Counts Values for Counted Classifier
/// </summary>
type CountedValue<'T> ( propertyCollection:PropertyCollection, key:obj, value:'T ) =
    let _value = value
    let _key = key
    let _propertyCollection = propertyCollection
    let mutable _count = 1

    member __.Value with get() = _value

    member __.Count with get() = _count and set v = _count <- v

    member __.Release() =
        _count <- _count - 1
    //    if _count = 0 then
      //      (_value :?> IDisposable).Dispose()
        _propertyCollection.RemoveProperty(_key) |> ignore


    static member GetOrCreate( propertyCollection:PropertyCollection , key:obj, createFunc:unit-> 'T) =
        let countedValueRef = ref Unchecked.defaultof<CountedValue<'T>>

        if propertyCollection.TryGetPropertySafe(key, countedValueRef) then
            let countedValue = !countedValueRef
            countedValue.Count <- countedValue.Count + 1
            countedValue
        else
            let countedValue = new CountedValue<'T>( propertyCollection, key, createFunc())
            propertyCollection.[key] <- countedValue
            countedValue
            