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

type Label = {
    Caption : string
}

type ButtonState =
    | Pressed
    | Released

type Button = {
    Caption : string
    State   : ButtonState
}

type CheckboxState =
    | Checked
    | Unchecked

type Checkbox = {
    Caption : string
    State   : CheckboxState
}

type RadioGroup = {
    Caption : string
    Buttons : Checkbox []
}

type CollapsibleState =
    | Collapsed
    | Expanded



type HSlider = {
    Min : single
    Max : single
    Val : single
}

type VSlider = {
    Min : single
    Max : single
    Val : single
}

[<MeasureAttribute>] type wid

type Layout = size2 * (Widget * size2) [] -> rect[]


and Container = {
    Widgets     : Widget []

    Active      : int<wid> option
    Hot         : int<wid> option
    Disabled    : Set<int<wid>>

    Layout      : Layout

    Positions   : rect []
}

and Collapsible = {
    Caption : string
    State   : CollapsibleState
    Container   : Container
}

and Frame = {
    Title       : string
    Position    : vec2
    Size        : size2
    Container   : Container
}

and Widget =
    | Label         of Label
    | Checkbox      of Checkbox
    | RadioGroup    of RadioGroup
    | Button        of Button
    | HSlider       of HSlider
    | VSlider       of VSlider
    | Collapsible   of Collapsible
    | Container     of Container
    | Frame         of Frame


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
        
    static member from (wd: Widget) =
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

and Theme = {
    Name        : string
    Atlas       : Atlas
    Widgets     : Map<WidgetType * PaintStyle, Base.WidgetData>
}

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


//let private gWid = ref 0<WidM>
//let private newWid () =
//    let g = !gWid
//    gWid := !gWid + 1<WidM>
//    g
//
//type Widget
//with
//    static member label (fd: FontData) (s: string) = Label (newWid(), { Label.Caption = s; Font = fd })
//    static member button (fd: FontData) (s: string, bs: ButtonState) = Button (newWid(), { Label.Caption = s; Font = fd }, bs)
//
//


