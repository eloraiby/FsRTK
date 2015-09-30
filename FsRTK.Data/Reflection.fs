module FsRTK.Data.Reflection

open System
open System.Reflection
open Microsoft.FSharp.Reflection

open FsRTK.Data
open FsRTK.Data.Runtime

//let rec valueSerializer (t: Type) (v: obj) =
//    match t with
//    | t when t = typeof<int>        -> JsonValue.Number  (System.Convert.ToDecimal v)
//    | t when t = typeof<single>     -> JsonValue.Float   (System.Convert.ToDouble  v)
//    | t when t = typeof<float>      -> JsonValue.Float   (unbox<float>  v)
//    | t when t = typeof<bool>       -> JsonValue.Boolean (unbox<bool>   v)
//    | t when t = typeof<string>     -> JsonValue.String  (unbox<string> v)
//    | t when t.IsArray              ->
//        JsonValue.Array
//        <| 
                                        
let rec createRecordInstance t =
    let fields = FSharpType.GetRecordFields t
                 |> Array.map(fun f ->
                    match f.PropertyType with
                    | t when t = typeof<int> -> printfn "hello"
                    | t when t = typeof<single> -> pr)
    
    fun (o: obj) ->
        JsonValue.Record

let createUnionInstance t = failwith "unimplemented"
let createTupleInstance t = failwith "unimplemented"

let createInstance<'A>() =
    let t = typeof<'A>
    if FSharpType.IsRecord t
    then createRecordInstance t
    elif FSharpType.IsUnion t
    then createUnionInstance t
    elif FSharpType.IsTuple t
    then createTupleInstance t
    else failwith "unsupported type"

