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
open System.IO

open Nessos.Argu

open FsRTK.Data
open FsRTK.Ui

type Arguments =
    | [<PrintLabels>]
      [<Mandatory>]   Theme         of theme : string
    | [<PrintLabels>] Add_Icon      of icon  : string
    | [<PrintLabels>] Remove_Icon   of icon  : string
    | [<PrintLabels>] Add_Widget    of widget: string * v0: int * v1: int * h0: int * h1: int
    | [<PrintLabels>] Remove_Widget of widget: string
    | [<PrintLabels>] Add_Font      of font  : string * size: int * mode: string
    | [<PrintLabels>] Remove_Font   of font  : string * size: int * mode: string
    | [<PrintLabels>] Build_To      of atlas : string

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
        let usage           = argParser.Usage ()
        let values          = argParser.Parse argv
        
        let theme           = values.GetResults <@ Theme         @> |> Set.ofList |> Set.toArray
        let addIcon         = values.GetResults <@ Add_Icon      @> |> Set.ofList
        let removeIcon      = values.GetResults <@ Remove_Icon   @> |> Set.ofList
        let addWidget       = values.GetResults <@ Add_Widget    @> |> List.map (fun (str, v0, v1, h0, h1) -> str, (v0, v1, h0, h1)) |> Map.ofList
        let removeWidget    = values.GetResults <@ Remove_Widget @> |> Set.ofList
        let addFont         = values.GetResults <@ Add_Font      @> |> Set.ofList
        let removeFont      = values.GetResults <@ Remove_Font   @> |> Set.ofList
        let buildTo         = values.GetResults <@ Build_To      @> |> Set.ofList |> Set.toArray

        if theme.Length > 1
        then failwith (sprintf "you can have only 1 theme file, got: %d\nUsage:\n%s" theme.Length usage)
 
        if buildTo.Length > 1
        then failwith (sprintf "you can have only 1 output atlas file, got: %d\nUsage:\n%s" buildTo.Length usage)
           
        let iconsToAdd      = Set.difference addIcon removeIcon |> Set.toArray
        let iconsToRemove   = removeIcon                        |> Set.toArray

        let widgetsToAdd    = addWidget                         |> Map.filter (fun k v -> not (removeWidget.Contains k)) |> Map.toArray
        let widgetsToRemove = removeWidget                      |> Set.toArray

        let fontsToAdd      = Set.difference addFont removeFont |> Set.toArray
        let fontsToRemove   = removeFont                        |> Set.toArray

        printfn "Theme file: %s" theme.[0]

        let jsonStr =
            try
                use file = File.OpenText theme.[0]
                file.ReadToEnd ()

            with e ->
                printfn "%s does not exist, creating a new theme file" theme.[0]
                use file = File.CreateText theme.[0]

                let atlas = (AtlasBuilder.Source.Atlas.empty ()).ToString ()
                file.Write atlas
                atlas
            |> JsonValue.Parse
            
        let atlas = Json.deserialize<AtlasBuilder.Source.Atlas> jsonStr
        printfn "%A" atlas


    with e ->
        printfn "Fatal Error: %s" e.Message

    0 // return an integer exit code
