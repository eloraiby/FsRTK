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

type InputReception =
    | Accept
    | Discard

type Slider = {
    Min : single
    Max : single
    Val : single
}

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
    Font    : Font
    Caption : string
}

type CollapseState =
    | Collapsed
    | Expanded

type Layout = size2 * (Widget * size2) [] -> rect[]

and Container = {
    Widgets     : Widget []

    ActiveWidget: Widget option
    HotWidget   : Widget option
    Deactivated : Widget []

    Layout      : Layout

    Positions   : rect []
}


and [<CustomComparison; CustomEquality>] Widget =
    | Label     of Guid * Label
    | Checkbox  of Guid * Label * bool
    | Radiobox  of Guid * Label * (string * bool) []
    | Button    of Guid * Label * bool
    | HSlider   of Guid * Slider
    | Collapse  of Guid * Label * CollapseState * Container
    | Container of Guid * Container

    with
        member x.Guid =
            match x with
            | Label     (guid, _)       -> guid
            | Checkbox  (guid, _, _)    -> guid
            | Radiobox  (guid, _, _)    -> guid
            | Button    (guid, _, _)    -> guid
            | HSlider   (guid, _)       -> guid
            | Collapse  (guid, _, _, _) -> guid
            | Container (guid, _)       -> guid

        interface IComparable<Widget> with
            member this.CompareTo other = this.Guid.CompareTo other.Guid
          
        interface IComparable with
            member this.CompareTo other =
                match other with
                | :? Widget as other -> (this :> IComparable<Widget>).CompareTo other
                | _ -> failwith "other is not a Widget"

        interface IEquatable<Widget> with
            member this.Equals other = this.Guid.Equals other.Guid

        override this.Equals obj =
            match obj with
            | :? Widget as other -> (this :> IEquatable<Widget>).Equals other
            | _ -> failwith "obj is not a Widget"

        override this.GetHashCode () = this.Guid.GetHashCode ()

and WidgetType =
    | WtLabel      
    | WtCheckbox  
    | WtRadiobox  
    | WtButton    
    | WtHSlider   
    | WtCollapse  
    | WtContainer
with
    override x.ToString () =
        match x with
        | WtLabel    -> "label"    
        | WtCheckbox -> "checkbox" 
        | WtRadiobox -> "radiobox" 
        | WtButton   -> "button"   
        | WtHSlider  -> "hslider"  
        | WtCollapse -> "collapse" 
        | WtContainer-> "container" 

and Theme = {
    Name        : string
    Widgets     : Map<WidgetType * PaintStyle, Base.WidgetData>
    Present     : (rect * Widget)[] -> unit
    ComputeSize : Widget -> size2
}


type PointerState = {
    Position    : vec2
    Button0     : bool
    Button1     : bool
    Button2     : bool
} with
    member x.Left   = x.Button0
    member x.Middle = x.Button1
    member x.Right  = x.Button2

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
        | "radiobox"  -> WtRadiobox  
        | "button"    -> WtButton    
        | "hslider"   -> WtHSlider   
        | "collapse"  -> WtCollapse  
        | "container" -> WtContainer
        | _           -> failwith "unrecognized widget type"

  

let widgetTypeAndStyle (str: string) =
    match str.Split('.') with
    | [| wt; ws |] -> wt, ws |> PaintStyle.parse
    | _            -> failwith "invalid widget type and/or state"


