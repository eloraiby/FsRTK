(*
** .Net GLES2
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
open System

open FsRTK
open FsRTK.Gles2
open FsRTK.Glfw3

open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

open FsRTK.Ui.Base
open FsRTK.Ui.Widgets
open FsRTK.Ui.Theme

[<EntryPoint>]
let main argv =
    let init = Glfw3.init ()

    let window = Glfw3.createWindow (800, 640, "Test UI", None, None)
    Glfw3.makeContextCurrent window
    Glfw3.swapInterval 1

    let theme = Theme.fromFile "test.atlas"
    let uiDriver = new Ui.Gles2Driver.OpenGLDriver (65535, 8096) :> Ui.Compositor.IDriver
    let uiCompositor = Ui.Compositor.create (theme, uiDriver)
    let droid12 = uiCompositor.TryGetFont "DroidSans-antialias-12"

    let frame = uiCompositor.TryGetWidget "frame.active" 
    let buttonActive = uiCompositor.TryGetWidget "button.active"
    let buttonHot    = uiCompositor.TryGetWidget "button.hot"
    let buttonNormal = uiCompositor.TryGetWidget "button.normal"
    let buttonDisabled = uiCompositor.TryGetWidget "button.disabled"

    let label0 = Widget.Label fd "Hello From Label"
    let button0_released =  Widget.button fd ("Hello From Button\nHello again" , Released)
    let button0_pressed  =  Widget.button fd ("Hello From Button\nHello again" , Pressed )

    while Glfw3.windowShouldClose window |> not do

        glClearColor (0.0f, 0.0f, 0.0f, 0.0f)
        glClear ((GLenum.GL_COLOR_BUFFER_BIT ||| GLenum.GL_DEPTH_BUFFER_BIT) |> int)

        let width, height = Glfw3.getWindowSize window
        let mx, my = Glfw3.getCursorPos window
        let action = Glfw3.getMouseButton (window, MouseButton.BUTTON_LEFT)
        glViewport (0, 0, width, height)
        //uiCompositor.Post (Ui.Compositor.Command.FillRect (vec2(0.0f, 0.0f), size2(width |> single, height |> single), color4(0.0f, 1.0f, 0.0f, 1.0f)))
        uiCompositor.Post (PushRegion (rect (0.0f, 0.0f, width |> single, height |> single)))
        let strSize = droid12.Value.StringSize "Hello World"
        uiCompositor.Post (Command.FillRect (vec2(100.0f, 100.0f), strSize, color4(1.0f, 0.0f, 0.0f, 1.0f)))
        uiCompositor.Post (Command.DrawString (droid12.Value, vec2(100.0f, 100.0f), "Hello World", color4(1.0f, 1.0f, 1.0f, 1.0f)))
        //uiCompositor.Post (Ui.Compositor.Command.DrawLine (0.25f, vec2(50.0f, 50.0f), vec2(750.0f, 600.0f), color4(1.0f, 0.0f, 0.0f, 1.0f)))
        match frame with
        | Some frame ->
            uiCompositor.Post (Command.PushRegion (rect (vec2(mx |> single, my |> single), size2(250.0f, 250.0f))))
            uiCompositor.Post (Command.DrawWidget (frame, vec2(0.0f, 0.0f), size2(250.0f, 250.0f)))
            theme.Draw uiCompositor false false label0 (rect (0.0f, 0.0f, 100.0f, 100.0f))
            
            match action with
            | Action.PRESS | Action.REPEAT -> theme.Draw uiCompositor false false button0_pressed (rect (16.0f, 16.0f, 200.0f, 200.0f))
            | Action.RELEASE -> theme.Draw uiCompositor false false button0_released (rect (16.0f, 16.0f, 200.0f, 200.0f))

            uiCompositor.Post Command.PopRegion
        | _ -> ()

        match buttonActive with
        | Some buttonActive -> uiCompositor.Post (Command.DrawWidget (buttonActive, vec2((mx |> single) + 50.0f, (my |> single) + 48.0f), size2(45.0f, 16.0f)))
        | _ -> ()

        match buttonNormal with
        | Some buttonNormal -> uiCompositor.Post (Command.DrawWidget (buttonNormal, vec2((mx |> single) + 50.0f, (my |> single) + 64.0f), size2(150.0f, 16.0f)))
        | _ -> ()

        match buttonHot with
        | Some buttonHot -> uiCompositor.Post (Command.DrawWidget (buttonHot, vec2((mx |> single) + 50.0f, (my |> single) + 98.0f), size2(45.0f, 16.0f)))
        | _ -> ()

        match buttonDisabled with
        | Some buttonDisabled -> uiCompositor.Post (Command.DrawWidget (buttonDisabled, vec2((mx |> single) + 50.0f, (my |> single) + 128.0f), size2(150.0f, 16.0f)))
        | _ -> ()

        uiCompositor.Post Command.PopRegion
        uiCompositor.PresentAndReset () |> ignore

        Glfw3.swapBuffers window
        //Glfw3.pollEvents ()
        Glfw3.waitEvents ()
        
    Glfw3.destroyWindow window

    0