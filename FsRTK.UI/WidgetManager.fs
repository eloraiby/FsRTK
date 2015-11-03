module FsRTK.Ui.WidgetManager

open FsRTK
open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

open FsRTK.Data

open FsRTK.Ui.Base
open FsRTK.Ui.Widgets

//    Widgets     : Widget<'S> []
//
//    Active      : int<wid> option
//    Hot         : int<wid> option
//    Disabled    : Set<int<wid>>
//
//    Layout      : Layout<'S>
//
//    Positions   : rect []
type Container<'S>
with
    member x.ProcessEvent (we: WindowEvent) : 'S option =
        failwith "not implemented"

type Manager<'S, 'M> = {
    EventQueue  : Queue<WindowEvent>
    Widgets     : Widget<'S> list
    Model       : 'M
    ApplySignal : 'M -> 'S -> 'M           // apply signal to old model and generate new model
    GenerateUi  : 'M -> Widget<'S> list    // regenerate the widgets
} with
    member x.EnqueueEvent e = { x with EventQueue = x.EventQueue.Enqueue e }

    static member applyWindowEvent (m: Manager<'Ev, 'Md>) =
        let e, newEvQueue = m.EventQueue.Dequeue ()

        { m with EventQueue = newEvQueue }
    
    