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

type WindowEvent =
    | CursorMove    of vec2
    | CursorPress   of int * vec2
    | CursorRelease of int * vec2

type PaintStyle =
    | Hot
    | Active
    | Normal
    | Disabled
with
    override x.ToString () =
        match x with
        | Hot       -> "hot"       
        | Active    -> "active"    
        | Normal    -> "normal"    
        | Disabled  -> "disabled"

type Command =
    | PushRegion    of rect
    | PopRegion
    | DrawString    of FontData * vec2 * string * color4   
    | FillRect      of vec2 * size2 * color4
    | DrawLine      of single * vec2 * vec2 * color4
    | DrawRect      of single * vec2 * size2 * color4
    | DrawIcon      of IconData * vec2
    | DrawWidget    of WidgetData * vec2 * size2

type ICompositor =
    abstract member TryGetFont      : string -> FontData option
    abstract member TryGetWidget    : string -> WidgetData option
    abstract member PresentAndReset : unit -> int
    abstract member Post            : Command -> unit
    abstract member Theme           : Theme
    abstract member ContentFont     : FontData
    abstract member TitleFont       : FontData
    abstract member IconFont        : FontData
    abstract member MonoFont        : FontData

and WidgetType =
    | WtLabel      
    | WtCheckbox  
    | WtRadioGroup 
    | WtButton    
    | WtHSlider   
    | WtVSlider   
    | WtCollapsible
    | WtContainer
    | WtFrame
with
    override x.ToString () =
        match x with
        | WtLabel    -> "label"    
        | WtCheckbox -> "checkbox" 
        | WtRadioGroup -> "radiogroup" 
        | WtButton   -> "button"   
        | WtHSlider  -> "hslider"  
        | WtVSlider  -> "vslider"  
        | WtCollapsible -> "collapsible" 
        | WtContainer-> "container" 
        | WtFrame    -> "frame"

and Theme = {
    Name        : string
    Atlas       : Atlas
    Widgets     : Map<WidgetType * PaintStyle, WidgetData>
}

//type WidgetBase = {
//    State   : obj
//    Region  : rect
//    Paint   : ICompositor -> Theme -> size2 -> obj -> unit
//    Handle  : WindowEvent -> obj -> obj
//}
//
//type IWidget =
//    abstract State  : obj
//    abstract member Paint  : ICompositor -> Theme -> size2 -> obj -> unit
//    abstract member HandlePointerEvent : WindowEvent -> obj -> obj
//
//type Widget<'S> = {
//     State   : 'S
//     Paint   : ICompositor -> Theme -> size2 -> 'S -> unit
//     HandlePointerEvent : WindowEvent -> 'S -> 'S
//} with
//    interface IWidget with
//        member x.State  = box x.State
//        member x.Paint c t s o = x.Paint c t s (unbox<'S> o)
//        member x.HandlePointerEvent w o = x.HandlePointerEvent w (unbox<'S> o) |> box

