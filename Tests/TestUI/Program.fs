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
    
    let quit = ref false
        
    Glfw3.destroyWindow window

    0 // return an integer exit code
