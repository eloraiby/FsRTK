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

module FsRTK.Ui.Base

open FsRTK
open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

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

type IconData = {
    Width      : int
    Height     : int
    TCoordX    : int
    TCoordY    : int
}


type WidgetData = {
    Width      : int
    Height     : int
    TCoordX    : int
    TCoordY    : int

    V0         : int
    V1         : int
    H0         : int
    H1         : int
}

type FontData = {
    Mode       : FontRenderMode
    Size       : int
    CodePoints : Map<int, CharInfo>
}

type Font   = {
    Name    : string
    Data    : FontData
}

type Icon   = {
    Name    : string
    Data    : IconData
}

type Atlas = {
    ImageName    : string
    ImageWidth   : int
    ImageHeight  : int
    Fonts        : Map<string, FontData>
    Icons        : Map<string, IconData>
    Widgets      : Map<string, WidgetData>
}

type Command =
    | PushRegion    of rect
    | PopRegion
    | DrawString    of FontData * vec2 * string * color4   
    | FillRect      of vec2 * size2 * color4
    | DrawLine      of single * vec2 * vec2 * color4
    | DrawRect      of single * vec2 * size2 * color4
    | DrawIcon      of IconData * vec2
    | DrawWidget    of WidgetData * vec2 * size2
