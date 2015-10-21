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

module FsRTK.Ui.Theme

open FsRTK
open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

open FsRTK.Ui.Base
open FsRTK.Ui.Widgets


module File =
    type FontEntry = {
        Mode       : FontRenderMode
        Size       : int
        CodePoints : (int * CharInfo) []
    }

    type IconEntry = {
        Width      : int
        Height     : int
        TCoordX    : int
        TCoordY    : int
    }

    type WidgetEntry = {
        Width      : int
        Height     : int
        TCoordX    : int
        TCoordY    : int

        V0         : int
        V1         : int
        H0         : int
        H1         : int
    }

    type Atlas = {
        ImageName    : string
        ImageWidth   : int
        ImageHeight  : int
        Fonts        : (string * FontEntry)[]
        Icons        : (string * IconEntry)[]
        Widgets      : ((string * PaintStyle) * WidgetEntry)[]
    }

//------------------------------------------------------------------------------

type FontData
with
    static member from (f: File.FontEntry) = {
        Mode       = f.Mode
        Size       = f.Size
        CodePoints = f.CodePoints |> Map.ofArray }

type IconData
with
    static member from (i: File.IconEntry) = {
        IconData.Width      = i.Width   
        IconData.Height     = i.Height  
        IconData.TCoordX    = i.TCoordX 
        IconData.TCoordY    = i.TCoordY  }

type WidgetData
with
    static member from (w: File.WidgetEntry) = {
        Width      = w.Width   
        Height     = w.Height  
        TCoordX    = w.TCoordX 
        TCoordY    = w.TCoordY 

        V0         = w.V0      
        V1         = w.V1      
        H0         = w.H0      
        H1         = w.H1 }

type Atlas
with
    static member from (a: File.Atlas) = {
        ImageName    = a.ImageName
        ImageWidth   = a.ImageWidth
        ImageHeight  = a.ImageHeight
        Fonts        = a.Fonts   |> Array.map(fun (s, f) -> s, FontData.from f)   |> Map.ofArray
        Icons        = a.Icons   |> Array.map(fun (s, i) -> s, IconData.from i)   |> Map.ofArray
        Widgets      = a.Widgets |> Array.map(fun ((wt, ws), w) -> (sprintf "%O.%O" wt ws), WidgetData.from w) |> Map.ofArray      
        }

type Theme
with
    static member contentSize (wid: Widget) : size2 =
        match wid with
        | Label     _ -> failwith "not implemented"
        | Checkbox  _ -> failwith "not implemented"
        | Radiobox  _ -> failwith "not implemented"
        | Button    _ -> failwith "not implemented"
        | HSlider   _ -> failwith "not implemented"
        | Collapse  _ -> failwith "not implemented"
        | Container _ -> failwith "not implemented"

    static member fromFile filename =
        use f = System.IO.File.OpenText filename
        let atlas = f.ReadToEnd ()
                    |> Data.JsonValue.Parse
                    |> Data.Json.deserialize<File.Atlas>
        Atlas.from atlas

type FrameManagerState = {
    ActiveFrame : int option
    HotFrame    : int option

    PrevPointerState  : PointerState option

    Frames      : Frame[]
}

type PointerEvent =
    | MoveTo    of vec2
    | ClickAt   of vec2
    | ReleaseAt of vec2
    | DragTo    of vec2
    | Scroll    of single
with
    static member extractPointerEvent (prev: PointerState, curr: PointerState) =
        match prev, curr with
        | { Position = p; Button0 = true  }, { Position = c; Button0 = true  } when p <> c -> DragTo    c
        | { Position = p; Button0 = false }, { Position = c; Button0 = true  }             -> ClickAt   c
        | { Position = p; Button0 = true  }, { Position = c; Button0 = false }             -> ReleaseAt c
        | _                                , { Position = c                  }             -> MoveTo    c

type FrameManagerState
with
    static member getContainingBoxIndex (pos: vec2, rects: rect[]) : int option =
        let rec findFirst i =
            if i < rects.Length
            then
                if rects.[i].Contains pos
                then Some i
                else findFirst (i + 1)
            else None
        findFirst 0

    static member nextHot    = FrameManagerState.getContainingBoxIndex
    static member nextActive = FrameManagerState.getContainingBoxIndex

type private RenderState =
    | Hot
    | Active
    | Normal
    | Disabled
with
    static member mapControlState (cont: Container) (wid: Widget) =
        match cont.ActiveWidget, cont.HotWidget with
        | Some aw, Some hw ->
            if aw = wid then Active
            elif hw = wid then Hot
            else Disabled
        | _ -> Normal

type Presenter = {
    DrawTile           : int * vec2 -> unit
    Flush              : unit -> unit
    TileSize           : Map<int, size2>
    NormalFontTiles    : Map<char, int>
    BoldFontTiles      : Map<char, int>
    TitleFontTiles     : Map<char, int>
    WidgetTiles        : Map<string, int>
}


