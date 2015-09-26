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

open System.Runtime
open System.Runtime.InteropServices

open FsRTK
open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Gles2

#nowarn "9"

type Program =
    { Vertex      : string
      Fragment    : string }

let simple =
    { Vertex        =
        "attribute vec4 attrPosition;\n" +
        "attribute vec4 attrColor;\n" +
        "varying vec4 varColor;\n" +
        "void main() {\n" +
        "   varColor = attrColor;\n" +
        "   gl_Position = attrPosition;\n" +
        "}"

      Fragment      =
        "precision mediump float;\n" +
        "varying vec4 varColor;\n" +
        "void main() {\n" +
        "    gl_FragColor = varColor;\n" +
        "}" }

[<StructAttribute; StructLayoutAttribute(LayoutKind.Sequential)>]
type Vertex =
    val     position    : vec4
    val     color       : vec4

    new(p, c)   = { position = p; color = c }

let verts =
    [| Vertex(vec4(0.0f, -1.0f, 0.0f, 1.0f), vec4(1.0f, 0.0f, 0.0f, 1.0f))
       Vertex(vec4(-0.6f, 1.0f, 0.0f, 1.0f), vec4(0.0f, 1.0f, 0.0f, 1.0f))
       Vertex(vec4(0.6f, 1.0f, 0.0f, 1.0f), vec4(0.0f, 0.0f, 1.0f, 1.0f)) |]

let indices = [| 0; 1; 2 |]

[<EntryPoint>]
let main argv = 

    printfn "init: %d" (Glfw3.init())
    printfn "version: %A" (Glfw3.getVersion())
    printfn "str version: %s" (Glfw3.getVersionString())

    let monitors = Glfw3.getMonitors()
    printfn "monitors: %d" (monitors.Length)

    let primaryMonitor = Glfw3.getPrimaryMonitor()

    printfn "primary monitor position: %A" (Glfw3.getMonitorPos(primaryMonitor))

    monitors
    |> Array.iter (fun m -> printfn "Name: %s, Pos: %A, Size: %A" (Glfw3.getMonitorName m) (Glfw3.getMonitorPos m) (Glfw3.getMonitorPhysicalSize m))

    monitors
    |> Array.iteri
        (fun im m ->
            m
            |> Glfw3.getVideoModes
            |> Array.iteri
                (fun iv vm ->
                    printfn "%dx%d - width:  %d - height: %d - RGB:    %d%d%d - rate:   %d" im iv vm.width vm.height vm.redBits vm.greenBits vm.blueBits vm.refreshRate))

    let vm = Glfw3.getVideoMode primaryMonitor
    printfn "Primary: width:  %d - height: %d - RGB:    %d%d%d - rate:   %d" vm.width vm.height vm.redBits vm.greenBits vm.blueBits vm.refreshRate    

    Glfw3.setGamma(primaryMonitor, 1.0f)

    let gammaRamp0 = Glfw3.getGammaramp primaryMonitor
    Glfw3.setGammaramp(primaryMonitor, gammaRamp0)
    let gammaRamp1 = Glfw3.getGammaramp primaryMonitor

    gammaRamp0.Red
    |> Array.zip gammaRamp1.Red
    |> Array.iter(fun (r0, r1) -> printfn "r0: %d - r1: %d" r0 r1)

    let win = Glfw3.createWindow(640, 480, "Hello World", None, None)
    Glfw3.makeContextCurrent(win)

    Glfw3.setWindowTitle(win, "Hahaha")
    Glfw3.setWindowPos(win, 100, 120)
    Glfw3.setWindowSize(win, 512, 512)

    printfn "Framebuffer %A" (Glfw3.getFrameBufferSize win)
    printfn "Window Frame size %A" (Glfw3.getWindowFrameSize win)

    Glfw3.iconifyWindow win
    Glfw3.restoreWindow win
    Glfw3.hideWindow win
    Glfw3.showWindow win

    let rnd = System.Random ()

    let vsId = glCreateShader GLenum.GL_VERTEX_SHADER
    let fsId = glCreateShader GLenum.GL_FRAGMENT_SHADER
    let progId = glCreateProgram ()

    glShaderSource (vsId, [| simple.Vertex   |])
    glShaderSource (fsId, [| simple.Fragment |])

    glCompileShader vsId
    glCompileShader fsId

    glAttachShader (progId, vsId)
    glAttachShader (progId, fsId)

    glLinkProgram progId

    let posId = glGetAttribLocation(progId, "attrPosition")
    let colId = glGetAttribLocation(progId, "attrColor")

    let buffers = glGenBuffers 2

    glBindBuffer (GLenum.GL_ARRAY_BUFFER, buffers.[0])
    glBufferData (GLenum.GL_ARRAY_BUFFER, verts, GLenum.GL_DYNAMIC_DRAW)

    glBindBuffer (GLenum.GL_ELEMENT_ARRAY_BUFFER, buffers.[1])
    glBufferData (GLenum.GL_ELEMENT_ARRAY_BUFFER, indices, GLenum.GL_DYNAMIC_DRAW)

    let windowRefresh win =
        let viewWidth, viewHeight = Glfw3.getWindowSize win
        glViewport (0, 0, viewWidth, viewHeight)

        let r, g, b, a = rnd.NextDouble() |> single, rnd.NextDouble() |> single, rnd.NextDouble() |> single, rnd.NextDouble() |> single

        glClearColor(r, g, b, a)
        glClear ((GLenum.GL_COLOR_BUFFER_BIT ||| GLenum.GL_DEPTH_BUFFER_BIT) |> int32)
        glUseProgram progId

        glEnableVertexAttribArray(posId)
        glEnableVertexAttribArray(colId)
        
        glBindBuffer(GLenum.GL_ARRAY_BUFFER, buffers.[0])
        glVertexAttribPointer(posId, 4, GLenum.GL_FLOAT, false, glSizeOf<Vertex>, 0)
        glVertexAttribPointer(colId, 4, GLenum.GL_FLOAT, false, glSizeOf<Vertex>, 4 * 4)

        glBindBuffer (GLenum.GL_ELEMENT_ARRAY_BUFFER, buffers.[1])
        glDrawElements(GLenum.GL_TRIANGLES, 3, GLenum.GL_UNSIGNED_INT, 0)
        Glfw3.swapBuffers win

    glClearColor(1.f, 0.5f, 0.5f, 0.f)

    Glfw3.setWindowRefreshCallback(win, windowRefresh)
    Glfw3.setWindowSizeCallback (win, fun (win, w, h) -> printfn "w: %d, h: %d" w h)
    Glfw3.setWindowPosCallback  (win, fun (win, x, y) -> printfn "x: %d, y: %d" x y)
    Glfw3.setWindowFocusCallback(win, fun (win, b) -> if b then printfn "focused" else printfn "unfocused")
    Glfw3.setWindowIconifyCallback (win, fun (win, b) -> if b then printfn "iconified" else printfn "uniconified")
    Glfw3.setFramebufferSizeCallback (win, fun (win, w, h) -> printfn "FB: w: %d, h: %d" w h)
    Glfw3.setKeyCallback(win, fun (w, k, i, a, m) -> printfn "%A - %d - %A - %A" k i a m; printfn "Key State: %A" (Glfw3.getKey (w, k)))
    Glfw3.setCharCallback(win, fun (w, c) -> printfn "%c" c)
    Glfw3.setCharModsCallback(win, fun (w, c, m) -> printfn "%c - %A" c m)
    Glfw3.setMouseButtonCallback(win, fun (w, b, a, m) -> printfn "mouse %A, %A, %A" b a m; printfn "clipboard: %s" (Glfw3.getClipboardString w))
    Glfw3.setCursorPosCallback(win, fun (w, x, y) -> printfn "pos: %f, %f" x y)
    Glfw3.setCursorEnterCallback(win, fun (w, b) -> printfn "Enter: %b" b)
    Glfw3.setScrollCallback(win, fun (w, x, y) -> printfn "Scroll: %f %f" x y)
    Glfw3.setDropCallback(win, fun (w, s) -> printfn "%A" s)

    let rec loop () =
        if Glfw3.windowShouldClose win
        then ()
        else
            Glfw3.pollEvents ()
            loop ()

    loop ()

    glDeleteBuffers buffers

    printfn "Pos : %A" (Glfw3.getWindowPos win)
    printfn "Size: %A" (Glfw3.getWindowSize win)
    0 // return an integer exit code
