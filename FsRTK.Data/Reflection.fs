(*
** F# Rendering ToolKit
** Copyright (C) 2015  Wael El Oraiby
** 
** This program is free software: you can redistribute it and/or modify
** it under the terms of the GNU Affero General Public License as
** published by the Free Software Foundation, either version 3 of the
** License, or (at your option) any later version.
** 
** This program is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU Affero General Public License for more details.
** 
** You should have received a copy of the GNU Affero General Public License
** along with this program.  If not, see <http://www.gnu.org/licenses/>.
*)

module FsRTK.Data.Json

open System
open System.Reflection
open Microsoft.FSharp.Reflection

open FsRTK.Data
open FsRTK.Data.Runtime

let serializeInt      (v: obj) = JsonValue.Number  (System.Convert.ToDecimal v)
let serializeSingle   (v: obj) = JsonValue.Float   (System.Convert.ToDouble  v)
let serializeFloat    (v: obj) = JsonValue.Float   (unbox<float>  v)
let serializeBoolean  (v: obj) = JsonValue.Boolean (unbox<bool>   v)
let serializeString   (v: obj) = JsonValue.String  (unbox<string> v)
let rec serializeArray (t: Type) (v: obj) =
        let elemType = t.GetElementType ()
        let e = unbox<System.Collections.IEnumerable> v
        let se = Seq.cast<obj> e

        JsonValue.Array (se
            |> Seq.toArray
            |> Array.map (fun o -> valueSerializer elemType o))

and serializeRecord   (t: Type) (v: obj) =
    let fields = FSharpType.GetRecordFields t
    JsonValue.Record (
        fields
        |> Array.map (fun pi -> pi.Name, valueSerializer pi.PropertyType (pi.GetValue v)))

and serializeUnion    (t: Type) (v: obj) =
    let cases = FSharpType.GetUnionCases t
    let ucase, objs = FSharpValue.GetUnionFields (v, t)
    JsonValue.Record [| "`tag", JsonValue.String ucase.Name
                        "`value", JsonValue.Array (objs |> Array.map(fun o -> valueSerializer (o.GetType()) o)) |]

and serializeTuple    (t: Type) (v: obj) =
    let elements = FSharpType.GetTupleElements t
    let objs = FSharpValue.GetTupleFields v
    JsonValue.Record [| "`tuple", JsonValue.Array (objs |> Array.map (fun o -> valueSerializer (o.GetType()) o)) |]

and valueSerializer (t: Type) (v: obj) =
    match t with
    | t when t = typeof<int>        -> serializeInt     v
    | t when t = typeof<single>     -> serializeSingle  v
    | t when t = typeof<float>      -> serializeFloat   v
    | t when t = typeof<bool>       -> serializeBoolean v
    | t when t = typeof<string>     -> serializeString  v
    | t when t.IsArray              -> serializeArray t v
    | t when FSharpType.IsRecord t  -> serializeRecord t v
    | t when FSharpType.IsUnion  t  -> serializeUnion t v
    | t when FSharpType.IsTuple  t  -> serializeTuple t v
    | _ -> failwith "unimplemented"        

let serialize<'T> (v: 'T) = valueSerializer typeof<'T> v

//------------------------------------------------------------------------------
// Deserializer
//------------------------------------------------------------------------------
let deserializeInt      = function | JsonValue.Number  v -> Convert.ToInt32 v  | JsonValue.Float v -> Convert.ToInt32  v | _ -> failwith "unable to convert JsonValue into int"
let deserializeSingle   = function | JsonValue.Number  v -> Convert.ToSingle v | JsonValue.Float v -> Convert.ToSingle v | _ -> failwith "unable to convert JsonValue into single"
let deserializeFloat    = function | JsonValue.Number  v -> Convert.ToDouble v | JsonValue.Float v -> Convert.ToDouble v | _ -> failwith "unable to convert JsonValue into float"
let deserializeBoolean  = function | JsonValue.Boolean v -> v | _ -> failwith "unable to convert JsonValue to boolean"
let deserializeString   = function | JsonValue.String  v -> v | _ -> failwith "unable to convert JsonValue to string"

let rec deserializeArray (t: Type) (v: JsonValue) =
    let elemType = t.GetElementType ()
    let se =
        match v with | JsonValue.Array arr -> arr | _ -> failwith "attempt to deserialize a non Json Array into an Array"
        |> Array.map (fun o -> valueDeserializer elemType o)
    let arr = Array.CreateInstance (elemType, se.Length)
    se.CopyTo (arr, 0)
    arr


and deserializeRecord   (t: Type) (v: JsonValue) =    
    let jsonFields = match v with | JsonValue.Record r -> r |> Map.ofArray | _ -> failwith "attempt to deserialize a non Json Record into a Record"

    let fields =
        FSharpType.GetRecordFields t
        |> Array.map (fun pi -> valueDeserializer pi.PropertyType jsonFields.[pi.Name])
    FSharpValue.MakeRecord(t, fields)

and deserializeUnion    (t: Type) (v: JsonValue) =
    let jsonFields = match v with | JsonValue.Record r -> r |> Map.ofArray | _ -> failwith "attempt to deserialize a non Json Union into a Union"
    let tag = deserializeString jsonFields.["`tag"]
    let case =
        FSharpType.GetUnionCases t
        |> Array.map (fun ci -> ci.Name, ci)
        |> Map.ofArray
        |> Map.find tag

    let jsValues =
        match jsonFields.["`value"] with
        | JsonValue.Array arr -> arr
        | _ -> failwith "value for union case should be an array"

    let values =
        case.GetFields()      
        |> Array.zip jsValues
        |> Array.map (fun (jsv, pi) ->
            valueDeserializer pi.PropertyType jsv)

    FSharpValue.MakeUnion(case, values)

and valueDeserializer (t: Type) (v: JsonValue) : obj =
    match t with
    | t when t = typeof<int>        -> deserializeInt     v  |> box
    | t when t = typeof<single>     -> deserializeSingle  v  |> box
    | t when t = typeof<float>      -> deserializeFloat   v  |> box
    | t when t = typeof<bool>       -> deserializeBoolean v  |> box
    | t when t = typeof<string>     -> deserializeString  v  |> box
    | t when t.IsArray              -> deserializeArray t v  |> box
    | t when FSharpType.IsRecord t  -> deserializeRecord t v |> box
    | t when FSharpType.IsUnion  t  -> deserializeUnion t v  |> box
    | _ -> failwith "unimplemented"   
   

let deserialize<'T> (v: JsonValue) = valueDeserializer typeof<'T> v |> unbox<'T>
