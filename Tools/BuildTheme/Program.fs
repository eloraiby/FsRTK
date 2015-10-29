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

open FsRTK.Math3D.Vector
open FsRTK.Math3D.Geometry
open FsRTK.Math3D.Matrix

open FsRTK.Ui.Base
open FsRTK.Ui.Theme
open FsRTK.Ui.Widgets

open AtlasBuilder

type Arguments =
    | [<PrintLabels>]
      [<Mandatory>]   Theme         of theme : string
    | [<PrintLabels>] Add_Icon      of icon  : string
    | [<PrintLabels>] Remove_Icon   of icon  : string
    | [<PrintLabels>] Add_Widget    of widget: string * v0: int * v1: int * h0: int * h1: int
    | [<PrintLabels>] Remove_Widget of widget: string
    | [<PrintLabels>] Add_Font      of font  : string * size: int * mode: string * alias: string
    | [<PrintLabels>] Remove_Font   of font  : string * size: int * mode: string
    | [<PrintLabels>] Build_To      of atlas : string * width: int * height: int
    | Draw_Bounds

with
    interface IArgParserTemplate with
        member x.Usage =
            match x with
            | Theme         _ -> "the theme source file (file is going to be updated depending on the commands given)"
            | Add_Icon      _ -> "add the specified icon to the theme configuration file"
            | Remove_Icon   _ -> "remove the specified icon from the theme configuration file"
            | Add_Widget    _ -> "add the specified widget to the theme configuration file"
            | Remove_Widget _ -> "remove the specified widget from the theme configuration file (v0: vertical line 0, v1: vertical line 1, h0: horizontal line 0, h1: horizontal line 1)"
            | Add_Font      _ -> "add the specified font to the theme configuration file (mode: either mono or antialias)"
            | Remove_Font   _ -> "remove the specified font from the theme configuration file"
            | Build_To      _ -> "build to the atlas file"
            | Draw_Bounds     -> "draw a bounding box around each tile in the atlas"

let argParser = ArgumentParser.Create<Arguments> ()
       
[<EntryPoint>]
let main argv =
    try
        let usage           = argParser.Usage ()
        let values          = argParser.Parse argv
        
        let theme           = values.GetResults <@ Theme         @> |> Set.ofList |> Set.toArray
        let addIcon         = values.GetResults <@ Add_Icon      @> |> Set.ofList
        let iconsToRemove   = values.GetResults <@ Remove_Icon   @> |> Set.ofList
        let addWidget       = values.GetResults <@ Add_Widget    @> |> List.map (fun (str, v0, v1, h0, h1) -> str, (v0, v1, h0, h1)) |> Map.ofList
        let widgetsToRemove = values.GetResults <@ Remove_Widget @> |> Set.ofList
        let addFont         = values.GetResults <@ Add_Font      @> |> Set.ofList
        let fontsToRemove   = values.GetResults <@ Remove_Font   @> |> Set.ofList
        let buildTo         = values.GetResults <@ Build_To      @> |> Set.ofList |> Set.toArray
        let drawBounds      = values.GetResults <@ Draw_Bounds   @> |> function | [] -> false | h :: [] -> true | _ -> failwith "only one draw-bounds is allowed"

        if theme.Length > 1
        then failwith (sprintf "you can have only 1 theme file, got: %d\nUsage:\n%s" theme.Length usage)
 
        if buildTo.Length > 1
        then failwith (sprintf "you can have only 1 output atlas file, got: %d\nUsage:\n%s" buildTo.Length usage)
           
        let iconsToAdd      = Set.difference addIcon iconsToRemove

        let widgetsToAdd    = addWidget                            |> Map.filter (fun k v -> not (widgetsToRemove.Contains k))

        let fontsToAdd      =
            addFont
            |> Set.fold (fun s (f, si, m, a) ->
                if fontsToRemove.Contains (a, si, m)
                then s
                else Set.add (f, si, m, a) s) Set.empty

        let themeFile = if (getExtension theme.[0]) = ".theme" then theme.[0] else theme.[0] + ".theme"

        printfn "Theme file: %s" themeFile

        let jsonStr =
            try
                use file = File.OpenText themeFile
                file.ReadToEnd ()

            with e ->
                printfn "%s does not exist, creating a new theme file" themeFile
                use file = File.CreateText themeFile

                let theme = AtlasBuilder.Source.Theme.empty () |> Json.serialize |> toString
                file.Write theme
                theme
            |> JsonValue.Parse
            
        let theme = Json.deserialize<AtlasBuilder.Source.Theme> jsonStr

        // remove entries
        let theme = { theme with
                        Icons   = theme.Icons   |> Array.filter (fun i -> not (iconsToRemove.Contains i.FileName))
                        Fonts   = theme.Fonts   |> Array.filter (fun f -> not (fontsToRemove.Contains (f.FileName, f.Size, toString f.Mode)))
                        Widgets = theme.Widgets |> Array.filter (fun w -> not (widgetsToRemove.Contains w.FileName)) }

        // add entries
        let theme = {
            theme with
                Icons =
                    theme.Icons
                    |> Array.map (fun i -> i.FileName)
                    |> Set.ofArray
                    |> Set.union iconsToAdd
                    |> Set.toArray
                    |> Array.map (fun i -> {Source.IconEntry.FileName = i})

                Fonts =
                    theme.Fonts
                    |> Array.map (fun f -> f.FileName, f.Size, toString f.Mode, f.Alias)
                    |> Set.ofArray
                    |> Set.union fontsToAdd
                    |> Set.toArray
                    |> Array.map (fun (f, s, m, a) -> { Source.FontEntry.FileName = f; Size = s; Mode = FontRenderMode.parse m; Alias = a })

                Widgets =
                    theme.Widgets
                    |> Array.map (fun w -> w.FileName, (w.V0, w.V1, w.H0, w.H1))
                    |> Map.ofArray
                    |> Map.fold (fun (m: Map<_, _>) k v -> m.Add (k, v)) widgetsToAdd   // combine the two maps
                    |> Map.toArray
                    |> Array.map(fun (s, (v0, v1, h0, h1)) -> { Source.WidgetEntry.FileName = s; V0 = v0; V1 = v1; H0 = h0; H1 = h1 }) }

        do
            // write final theme
            use file = File.CreateText themeFile
            theme
            |> Json.serialize
            |> toString
            |> file.Write

        if buildTo.Length > 0
        then
            let drawBounds = if drawBounds then RectangleOption.DrawBounds else RectangleOption.NoBounds 
            let fileName, width, height = buildTo.[0]
            use ftLib = new SharpFont.Library()
            buildAtlas ftLib fileName (isize2(width, height)) drawBounds

    with e ->
        printfn "Fatal Error: %s" e.Message

    0 // return an integer exit code
