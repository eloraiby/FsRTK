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
    let atlas = Theme.fromFile "test.atlas"
    let uiDriver = new Ui.Gles2Driver.OpenGLDriver (65535, 8096) :> Ui.Compositor.IDriver
    let uiCompositor = Ui.Compositor.create ("test.atlas", uiDriver)
    let droid12 = uiCompositor.TryGetFont "DroidSans-antialias-12"

    while Glfw3.windowShouldClose window |> not do
        glClearColor (0.0f, 0.0f, 0.0f, 0.0f)
        glClear ((GLenum.GL_COLOR_BUFFER_BIT ||| GLenum.GL_DEPTH_BUFFER_BIT) |> int)

        let width, height = Glfw3.getWindowSize window
        glViewport (0, 0, width, height)
        uiCompositor.Post (Ui.Compositor.Command.FillRect (vec2(0.0f, 0.0f), size2(width |> single, height |> single), color4(0.0f, 1.0f, 0.0f, 1.0f)))
        uiCompositor.Post (Ui.Compositor.PushRegion (Ui.Compositor.Box (0.0f, 0.0f, width |> single, height |> single)))
        uiCompositor.Post (Ui.Compositor.Command.DrawString (droid12.Value, vec2(100.0f, 100.0f), "Hello World", color4(1.0f, 1.0f, 1.0f, 1.0f)))
        uiCompositor.Post (Ui.Compositor.Command.DrawLine (0.25f, vec2(50.0f, 50.0f), vec2(750.0f, 600.0f), color4(1.0f, 0.0f, 0.0f, 1.0f)))
        uiCompositor.PresentAndReset () |> ignore

        Glfw3.swapBuffers window
        Glfw3.pollEvents ()
        
    Glfw3.destroyWindow window

    0