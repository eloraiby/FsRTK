(*
** Math3D for F#
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

namespace FsRTK.UI

open FsRTK
open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

type InputReception =
    | Accept
    | Discard

type Slider = {
    Min : float
    Max : float
    Val : float
}

type Layout = {
    Controls : (InputReception * Control)[]
    Apply    : size2 * Control [] -> rect[]
}

and Control =
    | Label     of string
    | Checkbox  of string * bool
    | Radiobox  of string * (InputReception * string * bool) []
    | Button    of string * bool
    | HSlider   of Slider
    | Collapse  of string * Control[]
    | Layout    of Layout

type PointerState = {
    Position    : vec2
    Button0     : bool
    Button1     : bool
    Button2     : bool
}

type Frame = {
    Title       : string
    X           : float
    Y           : float
    Width       : float
    Height      : float

    HScroll     : float * float
    VScroll     : float * float

    Active      : int option
    Hot         : int option

    Layout      : Layout
}

type Theme = {
    Name        : string
    Present     : (rect * InputReception * Control)[] -> unit
}

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

type Control
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

    static member nextHot    = Control.getContainingBoxIndex
    static member nextActive = Control.getContainingBoxIndex

type private RenderState =
    | Hot
    | Active
    | Normal
    | Disabled

with
    static member mapControlState e (isHot, isActive) =
        match e with
        | InputReception.Accept ->
            match isHot, isActive with
            | _, true -> Active
            | true, false -> Hot
            | false, false -> Normal
        | InputReception.Discard -> Disabled