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

open System

open FsRTK
open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

open FsRTK.Ui.Base




type Label = {
    Caption : string
}

type ButtonState =
    | Pressed
    | Released

type Button<'S> = {
    Caption : string
    State   : ButtonState
    OnStateChange : ButtonState -> 'S
}

type CheckboxState =
    | Checked
    | Unchecked

type Checkbox<'S> = {
    Caption : string
    State   : CheckboxState
    OnStateChange : CheckboxState -> 'S
}

type RadioGroup<'S> = {
    Caption : string
    Buttons : Checkbox<'S> []
}

type CollapsibleState =
    | Collapsed
    | Expanded



type Slider<'S> = {
    Min : single
    Max : single
    Val : single
    OnStateChange : single -> 'S
}

[<MeasureAttribute>] type wid

type Layout<'S> = size2 * (Widget<'S> * size2) [] -> rect[]


and Container<'S> = {
    Widgets     : Widget<'S> []

    Active      : int<wid> option
    Hot         : int<wid> option
    Disabled    : Set<int<wid>>

    Layout      : Layout<'S>

    Positions   : rect []
}

and Collapsible<'S> = {
    Caption     : string
    State       : CollapsibleState
    Container   : Container<'S>
    OnStateChange : CollapsibleState -> 'S
}

and Frame<'S> = {
    Title       : string
    Position    : vec2
    Size        : size2
    Container   : Container<'S>
    OnStateChange : vec2 -> 'S
}

and Widget<'S> =
    | Label         of Label
    | Checkbox      of Checkbox<'S>
    | RadioGroup    of RadioGroup<'S>
    | Button        of Button<'S>
    | HSlider       of Slider<'S>
    | VSlider       of Slider<'S>
    | Collapsible   of Collapsible<'S>
    | Container     of Container<'S>
    | Frame         of Frame<'S>


type WidgetType
with
        
    static member from (wd: Widget<_>) =
        match wd with
        | Label     _ -> WtLabel    
        | Checkbox  _ -> WtCheckbox 
        | RadioGroup  _ -> WtRadioGroup 
        | Button    _ -> WtButton   
        | HSlider   _ -> WtHSlider  
        | VSlider   _ -> WtVSlider  
        | Collapsible  _ -> WtCollapsible 
        | Container _ -> WtContainer
        | Frame     _ -> WtFrame    

type PointerState = {
    Position    : vec2
    Button0     : ButtonState
    Button1     : ButtonState
    Button2     : ButtonState
} with
    member x.Left   = x.Button0
    member x.Middle = x.Button1
    member x.Right  = x.Button2

type PaintStyle
with
    static member parse str =
        match str with
        | "hot"        -> Hot
        | "active"     -> Active
        | "normal"     -> Normal
        | "disabled"   -> Disabled
        | _ -> failwith "invalid Activation case to parse"

type WidgetType
with
    static member parse str =
        match str with
        | "label"     -> WtLabel      
        | "checkbox"  -> WtCheckbox  
        | "radiogroup"-> WtRadioGroup
        | "button"    -> WtButton    
        | "hslider"   -> WtHSlider   
        | "vslider"   -> WtVSlider   
        | "collapsible" -> WtCollapsible
        | "container" -> WtContainer
        | "frame"     -> WtContainer
        | _           -> failwith "unrecognized widget type"

  

let widgetTypeAndStyle (str: string) =
    match str.Split('.') with
    | [| wt; ws |] -> wt, ws |> PaintStyle.parse
    | _            -> failwith "invalid widget type and/or state"


type PointerEvent =
    | MoveTo    of vec2
    | ClickAt   of vec2
    | ReleaseAt of vec2
    | DragTo    of vec2
    | Scroll    of single
with
    static member extractPointerEvent (prev: PointerState, curr: PointerState) =
        match prev, curr with
        | { Position = p; Button0 = Pressed  }, { Position = c; Button0 = Pressed  } when p <> c -> DragTo    c
        | { Position = p; Button0 = Released }, { Position = c; Button0 = Pressed  }             -> ClickAt   c
        | { Position = p; Button0 = Pressed  }, { Position = c; Button0 = Released }             -> ReleaseAt c
        | _                                   , { Position = c                     }             -> MoveTo    c

type Widget<'S>
with
    static member label  (s: string) =
        Label { Label.Caption = s }

    static member button (s: string, bs: ButtonState) (f: ButtonState -> 'S) =
        Button { Button.Caption     = s
                 State              = bs
                 OnStateChange      = f }


    static member handlePointerEvent (e: PointerEvent) (w: Widget<'S>) =

        failwith "unimplemented"





