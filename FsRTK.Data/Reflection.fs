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
    JsonValue.Record(cases
        |> Array.map(fun c -> c.Name, JsonValue.Record (c.GetFields ()
                                        |> Array.map (fun pi -> pi.Name, valueSerializer pi.PropertyType (pi.GetValue v)))))

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
    | _ -> failwith "unimplemented"        

let serialize<'T> (v: 'T) = valueSerializer typeof<'T> v


