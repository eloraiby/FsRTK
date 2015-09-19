// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open FsRTK
open Gles2

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

    glShaderSource (vsId, [| simple.Vertex   |])
    glShaderSource (fsId, [| simple.Fragment |])

    let windowRefresh win =
        let r, g, b, a = rnd.NextDouble() |> single, rnd.NextDouble() |> single, rnd.NextDouble() |> single, rnd.NextDouble() |> single
        glClearColor(r, g, b, a)
        glClear ((GLenum.GL_COLOR_BUFFER_BIT ||| GLenum.GL_DEPTH_BUFFER_BIT) |> int32)
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
    Glfw3.setMouseButtonCallback(win, fun (w, b, a, m) -> printfn "mouse %A, %A, %A" b a m; (*printfn "clipboard: %s" (Glfw3.getClipboardString w)*))
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
    printfn "Pos : %A" (Glfw3.getWindowPos win)
    printfn "Size: %A" (Glfw3.getWindowSize win)
    0 // return an integer exit code
