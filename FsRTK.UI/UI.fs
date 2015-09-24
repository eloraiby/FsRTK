namespace FsRTK.UI

open FsRTK
open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

type ControlAcceptInput =
    | Enable
    | Disable

type Slider = {
    Min : float
    Max : float
    Val : float
}

type Layout = {
    Controls : (ControlAcceptInput * Control)[]
    Apply    : size2 * Control [] -> rect[]
}

and Control =
    | Label     of string
    | Checkbox  of string * bool
    | Radiobox  of string * (ControlAcceptInput * string * bool) []
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
    Present     : (rect * ControlAcceptInput * Control)[] -> unit
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
        | ControlAcceptInput.Enable ->
            match isHot, isActive with
            | _, true -> Active
            | true, false -> Hot
            | false, false -> Normal
        | ControlAcceptInput.Disable -> Disabled