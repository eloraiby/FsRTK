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

module FsRTK.Ui.Widgets

open FsRTK
open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

type InputReception =
    | Accept
    | Discard

type Slider = {
    Min : single
    Max : single
    Val : single
}

type Font =
    | Application   // Application general font
    | Title         // frame title font
    | Icon          // icon text font
    | Mono          // mono space font
    | Custom    of string

type Layout = {
    Widgets : Widget []
    ComputeSize : Theme * Widget -> size2
    Apply   : size2 * Widget [] -> rect[]
}

and Label = {
    Font    : Font
    Caption : string
}

and CollapseState =
    | Collapsed
    | Expanded

and Widget =
    | Label     of InputReception * Label
    | Checkbox  of InputReception * Label * bool
    | Radiobox  of InputReception * Label * (InputReception * string * bool) []
    | Button    of InputReception * Label * bool
    | HSlider   of InputReception * Slider
    | Collapse  of InputReception * Label * CollapseState * Widget[]
    | Layout    of InputReception * Layout

and Theme = {
    Name        : string
    Present     : (rect * Widget)[] -> unit
    ComputeSize : Widget -> size2
}


type PointerState = {
    Position    : vec2
    Button0     : bool
    Button1     : bool
    Button2     : bool
}

type Frame = {
    Title       : string
    X           : single
    Y           : single
    Width       : single
    Height      : single

    HScroll     : single * single
    VScroll     : single * single

    Active      : int option
    Hot         : int option

    Layout      : Layout
}

type Widget
with
    member x.InputReception =
        match x with
        | Label     (ir, _)       -> ir
        | Checkbox  (ir, _, _)    -> ir
        | Radiobox  (ir, _, _)    -> ir
        | Button    (ir, _, _)    -> ir
        | HSlider   (ir, _)       -> ir
        | Collapse  (ir, _, _, _) -> ir
        | Layout    (ir, _)       -> ir