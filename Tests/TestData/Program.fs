﻿(*
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

open System

open FsRTK.Data

type RecordA = {
    A : int
    B : string }

type RecordB = {
    A : RecordA
    B : int[] }

type RecordC = {
    A : RecordB
    B : RecordA[]
}

type UnionA = 
    | A
    | B of int
    | C of int * string

[<EntryPoint>]
let main argv = 
    let ra = { RecordA.A = 10; B = "Hello World" }
    let jsRa = Json.serialize ra
    let rra = Json.deserialize<RecordA> jsRa

    assert(ra = rra)

    printfn "%O\n------------------" jsRa

    let rb = { RecordB.A = ra; B = [| 10; 7; 3 |] }
    let jsRb = Json.serialize rb
    let rrb = Json.deserialize<RecordB> jsRb

    assert(rb = rrb)

    printfn "%O\n------------------" jsRb

    let rc = { RecordC.A = rb; B = [| ra; { RecordA.A = 12; B = "haha" } |] }
    let jsRc = Json.serialize rc
    let rrc = Json.deserialize<RecordC> jsRc

    assert(rc = rrc)

    printfn "%O\n------------------" jsRc

    let a = UnionA.A
    let jsA = Json.serialize a
    let rra = Json.deserialize<UnionA> jsA

    assert(a = rra)

    printfn "%O\n------------------" jsA

    let b = UnionA.B 10
    let jsB = Json.serialize b
    let rrb = Json.deserialize<UnionA> jsB

    assert(b = rrb)

    printfn "%O\n------------------" jsB

    let c = UnionA.C (11, "hello")
    let jsC = Json.serialize c
    let rrc = Json.deserialize<UnionA> jsC

    assert(c = rrc)

    printfn "%O\n------------------" jsC

    let jst = Json.serialize (111, "hhhaaa", 32.0f)
    let rrc = Json.deserialize<int * string * single> jst

    assert((111, "hhhaaa", 32.0f) = rrc)

    printfn "%O\n------------------" jsC

    0 // return an integer exit code
