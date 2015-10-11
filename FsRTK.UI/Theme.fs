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

open FsRTK.Ui.Widgets


type CharInfo = {
    AdvanceX  : int
    AdvanceY  : int
    Width     : int
    Height    : int
    Left      : int
    Top       : int
    TCoordX   : int
    TCoordY   : int
}

type FontRenderMode =
    | AntiAlias
    | Mono
with
    override x.ToString() =
        match x with
        | AntiAlias -> "antialias"
        | Mono      -> "mono"
    static member parse str =
        match str with
        | "antialias" -> AntiAlias
        | "mono"      -> Mono 
        | _ -> failwith "invalid FontRenderMode case to parse"

type IconEntry = {
    Width      : int
    Height     : int
    TCoordX    : int
    TCoordY    : int
}

type FontEntry = {
    Mode       : FontRenderMode
    Size       : int
    CodePoints : (int * CharInfo) []
}

type WidgetState =
    | Hot
    | Active
    | Normal
    | Disabled
with
    static member parse str =
        match str with
        | ".hot"        -> Hot
        | ".active"     -> Active
        | ".normal"     -> Normal
        | ".disabled"   -> Disabled
        | _ -> failwith "invalid WidgetState case to parse"

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
    Widgets      : ((string * WidgetState) * WidgetEntry)[]
}

//------------------------------------------------------------------------------



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
        | Layout    _ -> failwith "not implemented"

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
    | Scroll    of float
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
    static member mapControlState (wid: Widget) (isHot, isActive) =
        match wid.InputReception with
        | InputReception.Accept ->
            match isHot, isActive with
            | _, true -> Active
            | true, false -> Hot
            | false, false -> Normal
        | InputReception.Discard -> Disabled

type Presenter = {
    DrawTile           : int * vec2 -> unit
    Flush              : unit -> unit
    TileSize           : Map<int, size2>
    NormalFontTiles    : Map<char, int>
    BoldFontTiles      : Map<char, int>
    TitleFontTiles     : Map<char, int>
    WidgetTiles        : Map<string, int>
}


