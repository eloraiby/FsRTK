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

open System
open Nessos.Argu

type FontMode =
    | Mono
    | Anti_Alias

type Arguments =
    | [<PrintLabels>]
      [<Mandatory>]   Theme         of theme: string
    | [<PrintLabels>] Add_Icon      of icon: string
    | [<PrintLabels>] Remove_Icon   of icon: string
    | [<PrintLabels>] Add_Widget    of widget: string * v0: int * v1: int * h0: int * h1: int
    | [<PrintLabels>] Remove_Widget of widget: string
    | [<PrintLabels>] Add_Font      of font: string * size: int * mode: string
    | [<PrintLabels>] Remove_Font   of font: string
    | [<PrintLabels>] Build_To      of atlas: string

with
    interface IArgParserTemplate with
        member x.Usage =
            match x with
            | Theme         _ -> "the theme source file (file is going to be updated depending on the commands given)"
            | Add_Icon      _ -> "add the specified icon to the theme configuration file"
            | Remove_Icon   _ -> "remove the specified icon from the theme configuration file"
            | Add_Widget    _ -> "add the specified widget to the theme configuration file"
            | Remove_Widget _ -> "remove the specified widget from the theme configuration file (v0: vertical line 0, v1: vertical line 1, h0: horizontal line 0, h1: horizontal line 1)"
            | Add_Font      _ -> "add the specified font to the theme configuration file (mode: either mono or anti-alias)"
            | Remove_Font   _ -> "remove the specified font from the theme configuration file"
            | Build_To      _ -> "build to the atlas file"

let argParser = ArgumentParser.Create<Arguments> ()

[<EntryPoint>]
let main argv =
    try
        let values = argParser.Parse argv
        printfn "correct arguments"
    with e ->
        printfn "Fatal Error: %s" e.Message

    0 // return an integer exit code
