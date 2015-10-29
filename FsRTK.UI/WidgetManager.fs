module FsRTK.Ui.WidgetManager

open FsRTK
open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

open FsRTK.Ui.Base
open FsRTK.Ui.Widgets

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

