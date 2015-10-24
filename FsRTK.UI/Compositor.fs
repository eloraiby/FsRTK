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

module FsRTK.Ui.Compositor

open System
open System.Runtime.InteropServices

open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

open FsRTK.Ui.Base
open FsRTK.Ui.Widgets
open FsRTK.Ui.Theme

[<StructAttribute>]
type Vertex =
    val Position    : vec2
    val TexCoord    : vec2
    val Color       : color4

    new(pos: vec2, tcoord: vec2, col: color4)    = { Position = pos; TexCoord = tcoord; Color = col }

[<StructAttribute>]
type Triangle =
    val v0          : uint16
    val v1          : uint16
    val v2          : uint16

    new(v0, v1, v2) = { v0 = v0; v1 = v1; v2 = v2 }
    
    member x.Item with get i =
                        match i with
                        | 0 -> x.v0
                        | 1 -> x.v1
                        | 2 -> x.v2
                        | _ -> failwith "index out of range"



type UiBox(vMin: Vertex, vMax: Vertex) =
    member x.Min    = vMin
    member x.Max    = vMax
    member x.Width  = vMax.Position.x - vMin.Position.x
    member x.Height = vMax.Position.y - vMin.Position.y
    member x.Size   = size2.ofVec2 (vMax.Position - vMin.Position)
    member x.ToBox  =
        let vmin = vMin.Position
        let vmax = vMax.Position
        rect(vmin, vmax)

    //
    // clip a UiBox with a box (region)
    //
    static member clip (box: rect) (uiBox: UiBox) =
        // first check if the uiBox intersects the box
        let box2    = rect(uiBox.Min.Position.x, uiBox.Min.Position.y, uiBox.Width, uiBox.Height)
        if not (rect.overlap box box2)
        then None   // not intersecting, return nothing
        else
            // we only need to clip the min and the max (reposition) since all
            // the other elements (like ucs) can be induced from the min/max
            let tMin = (box.Min - uiBox.Min.Position) / uiBox.Size.AsVec2
            let tMax = (box.Max - uiBox.Min.Position) / uiBox.Size.AsVec2
 
           
            let interpolateClip (mn, mx, t) =
                let t = min (max t 0.0f) 1.0f
                mn + (mx - mn) * t

            let buildVec2Tuple (mn: vec2, mx: vec2, tmn: vec2, tmx: vec2) =
                let vMin = vec2( interpolateClip (mn.x, mx.x, tmn.x)
                               , interpolateClip (mn.y, mx.y, tmn.y) )
                let vMax = vec2( interpolateClip (mn.x, mx.x, tmx.x)
                               , interpolateClip (mn.y, mx.y, tmx.y) )
                vMin, vMax

            let uvMin, uvMax = buildVec2Tuple(uiBox.Min.TexCoord, uiBox.Max.TexCoord, tMin, tMax)
            let pMin,  pMax  = buildVec2Tuple(uiBox.Min.Position, uiBox.Max.Position, tMin, tMax)

            // color is uniform
            let col = uiBox.Max.Color
            Some (UiBox(Vertex(pMin, uvMin, col), Vertex(pMax, uvMax, col)))

type IDriver =
    inherit IDisposable

    abstract MaxVertexCount     : int
    abstract MaxTriangleCount   : int

    abstract member SetAtlasImage   : string -> unit
    abstract member BeginUi     : unit -> unit
    abstract member EndUi       : unit -> unit
    abstract member RenderBatch : viewport: vec2 * verts: Vertex[] * vertCount: int * tris: Triangle[] * triCount: int -> unit


let [<LiteralAttribute>] private MAX_REGIONS = 128

type private CompositorState = {
    VB              : Vertex[]
    TB              : Triangle[]

    UiAtlas         : Atlas

    Driver          : IDriver

    RelRegionStack  : rect[] // relative region stack
    AbsRegionStack  : rect[] // absolute region stack

    mutable StackIndex  : int

    mutable CharPos : vec2
    mutable VertexCount : int
    mutable TriCount: int
    mutable CountDC : int   // drawcall count
} with
    static member create (vCount: int, tCount: int, atlas: Atlas, driver: IDriver) =
        { VB        = Array.create vCount (Vertex())
          TB        = Array.create tCount (Triangle())
          
          UiAtlas   = atlas
          
          Driver    = driver
          
          RelRegionStack    = Array.create MAX_REGIONS (rect())
          AbsRegionStack    = Array.create MAX_REGIONS (rect())
          StackIndex    = 0

          CharPos   = vec2()
          VertexCount   = 0
          TriCount  = 0
          CountDC   = 0 }

    member x.Push b =
        x.RelRegionStack.[x.StackIndex] <- b
        match x.StackIndex with
        | 0 -> x.AbsRegionStack.[x.StackIndex]    <- b
        | _ ->
            let headRegion : rect = x.AbsTop
            x.AbsRegionStack.[x.StackIndex]    <- rect.intersect (rect.move b headRegion.Min) headRegion

        x.StackIndex <- x.StackIndex + 1
        let tip : rect = x.AbsTop
        printfn "- %d -> Tip At: %f, %f - %f, %f" x.StackIndex tip.X tip.Y tip.Width tip.Height

    member x.Pop    =
        match x.StackIndex with
        | 0 -> failwith "Region stack index below 0"
        | _ ->
            x.StackIndex <- x.StackIndex - 1
            x.RelRegionStack.[x.StackIndex], x.AbsRegionStack.[x.StackIndex]

    member x.Top    =
        x.RelRegionStack.[x.StackIndex - 1]

    member x.AbsTop    =
        x.AbsRegionStack.[x.StackIndex - 1]
    
type private CompositorImpl(theme: Theme, driver: IDriver) =
    let atlas   = theme.Atlas
    let uiImage = atlas.ImageName
    let white   = atlas.Icons.["white"]
    
    do driver.SetAtlasImage uiImage

    let state = CompositorState.create (driver.MaxVertexCount, driver.MaxTriangleCount, atlas, driver)

    let imWidth     = atlas.ImageWidth  |> single
    let imHeight    = atlas.ImageHeight |> single

    let queue   = System.Collections.Generic.Queue<Command>()

    let tryFlush (reqVerts: int, reqTris: int) =
        let width, height = state.AbsRegionStack.[0].Width, state.AbsRegionStack.[0].Height

        if state.VertexCount + reqVerts > driver.MaxVertexCount   ||
           state.TriCount    + reqTris  > driver.MaxTriangleCount
        then
            driver.RenderBatch(vec2(width, height), state.VB, state.VertexCount, state.TB, state.TriCount)
            state.VertexCount   <- 0
            state.TriCount      <- 0
            state.CountDC       <- state.CountDC + 1

    let tcoordToUV(posX: single, posY: single) : vec2 = vec2(posX / imWidth, posY  / imHeight)

    let compVec3 (v: vec2) =
        let z = (state.TriCount |> single) / (single driver.MaxTriangleCount)
        vec3(v.x, v.y, z)
        
    let addVertsTris(vCount: int, tCount: int) =
        state.VertexCount   <- state.VertexCount + vCount
        state.TriCount      <- state.TriCount    + tCount

    let drawLine (t: single, s: vec2, e: vec2, col: color4) =
        tryFlush (4, 2)

        let wX  = white.TCoordX + white.Width / 2  |> single
        let wY  = white.TCoordY + white.Height / 2 |> single
        let uv = tcoordToUV(wX, wY)

        let dir = e - s

        let ortho   = vec2.normalize(vec2.orhtogonal dir)

        let vs0 = s + ortho * t
        let vs1 = s - ortho * t
        let ve0 = e + ortho * t
        let ve1 = e - ortho * t

        let verts =
            [| Vertex(vs1, uv, col)
               Vertex(ve1, uv, col)
               Vertex(ve0, uv, col)
               Vertex(vs0, uv, col) |]

        let index = [| 0us; 1us; 2us; 2us; 3us; 0us |]

        let vidx    = state.VertexCount |> uint16

        state.VB.[vidx |> int]       <- verts.[0]
        state.VB.[(vidx |> int) + 1] <- verts.[1]
        state.VB.[(vidx |> int) + 2] <- verts.[2]
        state.VB.[(vidx |> int) + 3] <- verts.[3]


        let tidx    = state.TriCount |> uint16
        state.TB.[tidx |> int]            <- Triangle (vidx + index.[0],
                                                       vidx + index.[1],
                                                       vidx + index.[2])

        state.TB.[(tidx + 1us) |> int]    <- Triangle (vidx + index.[3],
                                                       vidx + index.[4],
                                                       vidx + index.[5])

        
        addVertsTris(4, 2)
        
    let drawTexturedBox (b: rect, tc: vec2 * vec2, col: color4) =
        tryFlush (4, 2)

        let u0v0, u1v1 = tc
        let u0 = u0v0.x
        let v0 = u0v0.y
        let u1 = u1v1.x
        let v1 = u1v1.y

        let region = state.AbsTop
        let x0, y0, x1, y1 =
            region.position.x + b.position.x,
            region.position.y + b.position.y,
            region.position.x + b.position.x + b.size.width,
            region.position.y + b.position.y + b.size.height

        let uiBox = UiBox(Vertex(vec2(x0, y0), vec2(u0, v0), col), Vertex(vec2(x1, y1), vec2(u1, v1), col))
        let cpBox = UiBox.clip state.AbsRegionStack.[state.StackIndex - 1] uiBox
        match cpBox with
        | Some uiBox ->
            let ve0 = uiBox.Min
            let ve1 = uiBox.Max

            let x0, y0 = ve0.Position.x, ve0.Position.y
            let u0, v0 = ve0.TexCoord.x, ve0.TexCoord.y
            let x1, y1 = ve1.Position.x, ve1.Position.y
            let u1, v1 = ve1.TexCoord.x, ve1.TexCoord.y

            let charRectVerts = 
                [| Vertex(vec2(x0, y0), vec2(u0, v0), col)
                   Vertex(vec2(x1, y0), vec2(u1, v0), col)
                   Vertex(vec2(x1, y1), vec2(u1, v1), col)
                   Vertex(vec2(x0, y1), vec2(u0, v1), col) |]

            let charIndex = [| 0us; 1us; 2us; 2us; 3us; 0us |]

            let vidx    = state.VertexCount |> uint16

            state.VB.[vidx |> int]       <- charRectVerts.[0]
            state.VB.[(vidx |> int) + 1] <- charRectVerts.[1]
            state.VB.[(vidx |> int) + 2] <- charRectVerts.[2]
            state.VB.[(vidx |> int) + 3] <- charRectVerts.[3]

            let tidx    = state.TriCount |> uint16
            state.TB.[tidx |> int]            <- Triangle (vidx + charIndex.[0],
                                                           vidx + charIndex.[1],
                                                           vidx + charIndex.[2])

            state.TB.[(tidx + 1us) |> int]    <- Triangle (vidx + charIndex.[3],
                                                           vidx + charIndex.[4],
                                                           vidx + charIndex.[5])

        
            addVertsTris(4, 2)
        | _ -> ()
//        let verts =
//            [| Vertex(vec2(x0, y0), vec2(u0, v0), col)
//               Vertex(vec2(x1, y0), vec2(u1, v0), col)
//               Vertex(vec2(x1, y1), vec2(u1, v1), col)
//               Vertex(vec2(x0, y1), vec2(u0, v1), col) |]
//
//        let index = [| 0us; 1us; 2us; 2us; 3us; 0us |]
//
//        let vidx    = state.VertexCount |> uint16
//
//        state.VB.[vidx |> int]       <- verts.[0]
//        state.VB.[(vidx |> int) + 1] <- verts.[1]
//        state.VB.[(vidx |> int) + 2] <- verts.[2]
//        state.VB.[(vidx |> int) + 3] <- verts.[3]
//
//        let tidx    = state.TriCount |> uint16
//        state.TB.[tidx |> int]            <- Triangle (vidx + index.[0],
//                                                       vidx + index.[1],
//                                                       vidx + index.[2])
//
//        state.TB.[(tidx + 1us) |> int]    <- Triangle (vidx + index.[3],
//                                                       vidx + index.[4],
//                                                       vidx + index.[5])
//
//        
//        addVertsTris(4, 2)


    let drawIcon (ie: IconData, pos: vec2, size: size2, uvOff: vec2, col: color4) =
        tryFlush (4, 2)

        let tx = ie.TCoordX |> single
        let ty = ie.TCoordY |> single
        let w  = ie.Width   |> single
        let h  = ie.Height  |> single

        let uv0 = tcoordToUV(tx, ty)
        let uv1 = tcoordToUV(tx + w, ty + h)

        let u0v0  = vec2(uv0.x + uvOff.x, uv0.y + uvOff.y)
        let u1v1  = vec2(uv1.x - uvOff.x, uv1.y - uvOff.y)
        drawTexturedBox (rect(pos, size), (u0v0, u1v1), col)


    let drawChar (fe: FontData, col: color4, scale: single) (ch: char) =
        tryFlush (4, 2)

        let chInfo  = fe.CodePoints.[ch |> int]
        let chTx    = chInfo.TCoordX |> single
        let chTy    = chInfo.TCoordY |> single
        let chW     = chInfo.Width   |> single
        let chH     = chInfo.Height  |> single
        let x, y    = state.CharPos.x + (chInfo.Left |> single), state.CharPos.y + scale * ((fe.Size +  - chInfo.Top) |> single)

        let region = state.AbsTop
        let v0, v1 =
            let x0, y0, x1, y1 =
                x + region.Min.x,
                y + region.Min.y,
                x + region.Min.x + scale * (chInfo.Width  |> single),
                y + region.Min.y + scale * (chInfo.Height |> single)

            let uv0, uv1 =
                let offset = vec2(0.0f, 0.0f) //vec2(0.5f / imWidth, 0.5f / imHeight)
                tcoordToUV(chTx, chTy) + offset,
                tcoordToUV(chTx + chW, chTy + chH) - offset

            Vertex(vec2(x0, y0), uv0, col),
            Vertex(vec2(x1, y1), uv1, col)

        let uiBox = UiBox(v0, v1)

        let cpBox = UiBox.clip state.AbsRegionStack.[state.StackIndex - 1] uiBox
        match cpBox with
        | Some uiBox ->
            let ve0 = uiBox.Min
            let ve1 = uiBox.Max

            let x0, y0 = ve0.Position.x, ve0.Position.y
            let u0, v0 = ve0.TexCoord.x, ve0.TexCoord.y
            let x1, y1 = ve1.Position.x, ve1.Position.y
            let u1, v1 = ve1.TexCoord.x, ve1.TexCoord.y

            let charRectVerts = 
                [| Vertex(vec2(x0, y0), vec2(u0, v0), col)
                   Vertex(vec2(x1, y0), vec2(u1, v0), col)
                   Vertex(vec2(x1, y1), vec2(u1, v1), col)
                   Vertex(vec2(x0, y1), vec2(u0, v1), col) |]

            let charIndex = [| 0us; 1us; 2us; 2us; 3us; 0us |]

            let vidx    = state.VertexCount |> uint16

            state.VB.[vidx |> int]       <- charRectVerts.[0]
            state.VB.[(vidx |> int) + 1] <- charRectVerts.[1]
            state.VB.[(vidx |> int) + 2] <- charRectVerts.[2]
            state.VB.[(vidx |> int) + 3] <- charRectVerts.[3]

            let tidx    = state.TriCount |> uint16
            state.TB.[tidx |> int]            <- Triangle (vidx + charIndex.[0],
                                                           vidx + charIndex.[1],
                                                           vidx + charIndex.[2])

            state.TB.[(tidx + 1us) |> int]    <- Triangle (vidx + charIndex.[3],
                                                           vidx + charIndex.[4],
                                                           vidx + charIndex.[5])

        
            addVertsTris(4, 2)
        | _ -> ()
        state.CharPos     <- vec2(state.CharPos.x + scale * (chInfo.AdvanceX |> single),
                                  state.CharPos.y + scale * (chInfo.AdvanceY |> single))

    let drawString (font: FontData, scale: single) (pos: vec2) (col: color4) (s: string) =
        state.CharPos <- pos
        for ch in s do
            if ch = '\n'
            then state.CharPos <- vec2(pos.x, state.CharPos.y + scale * (font.Size + 2 |> single))  // TODO: 2 pixels ? this should be absolute value of the box
            else drawChar (font, col, scale) ch
          
    let drawWidget (widget: WidgetData, pos: vec2, size: size2) =
        let s  = widget.TCoordX    |> single
        let t  = widget.TCoordY    |> single
        let w  = widget.Width      |> single
        let h  = widget.Height     |> single
        let h0 = widget.H0 |> single//t + (widget.H0 |> single)      |> single
        let h1 = widget.H1 |> single//t + h - (widget.H1 |> single)  |> single
        let v0 = widget.V0 |> single//s + (widget.V0 |> single)      |> single
        let v1 = widget.V1 |> single//s + w - (widget.V1 |> single)  |> single

        // we have 9 rectangles which are layed out as the following
        //        V0                      V1
        //        |                       |
        // p0     |p1                   p2|     p3
        //  +-----+-----------------------+-----+
        //  |     |                       |     |
        //  |  0  |           1           |  2  |
        //p4+-----+-----------------------+-----+---- H0
        //  |     |p5                   p6|     |p7
        //  |     |                       |     |
        //  |  3  |           4           |  5  |
        //  |     |                       |     |
        //  |     |p9                  p10|     |p11
        //p8+-----+-----------------------+-----+---- H1
        //  |  6  |           7           |  8  |
        //  |     |                       |     |
        //  +-----+-----------------------+-----+
        //p12      p13                  p14     p15
        //

        let p0 , st0  = pos                                 , tcoordToUV(s, t)
        let p1 , st1  = pos + vec2(v0, 0.0f)                , tcoordToUV(s + v0, t)
        let p2 , st2  = pos + vec2(size.width - v1, 0.0f)   , tcoordToUV(s + w - v1, t)
        let p3 , st3  = pos + vec2(size.width, 0.0f)        , tcoordToUV(s + w, t)

        let p4 , st4  = pos + vec2(0.0f, h0)                , tcoordToUV(s, t + h0)
        let p5 , st5  = pos + vec2(v0, h0)                  , tcoordToUV(s + v0, t + h0)
        let p6 , st6  = pos + vec2(size.width - v1, h0)     , tcoordToUV(s + w - v1, t + h0)
        let p7 , st7  = pos + vec2(size.width, h0)          , tcoordToUV(s + w, t + h0)

        let p8 , st8  = pos + vec2(0.0f, size.height - h1)  , tcoordToUV(s, h + t - h1)
        let p9 , st9  = pos + vec2(v0, size.height - h1)    , tcoordToUV(s + v0, h + t - h1)
        let p10, st10 = pos + vec2(size.width - v1, size.height - h1)   , tcoordToUV(s + w - v1, h + t - h1)
        let p11, st11 = pos + vec2(size.width, size.height - h1)        , tcoordToUV(s + w, h + t - h1)

        let p12, st12 = pos + vec2(0.0f, size.height)       , tcoordToUV(s, h + t)
        let p13, st13 = pos + vec2(v0, size.height)         , tcoordToUV(s + v0, h + t)
        let p14, st14 = pos + vec2(size.width - v1, size.height)   , tcoordToUV(s + w - v1, h + t)
        let p15, st15 = pos + vec2(size.width, size.height) , tcoordToUV(s + w, h + t)

        let r0 = rect.fromTo (p0, p5) in drawTexturedBox (r0, (st0, st5), color4(1.0f, 1.0f, 1.0f, 1.0f))
        let r1 = rect.fromTo (p1, p6) in drawTexturedBox (r1, (st1, st6), color4(1.0f, 1.0f, 1.0f, 1.0f))
        let r2 = rect.fromTo (p2, p7) in drawTexturedBox (r2, (st2, st7), color4(1.0f, 1.0f, 1.0f, 1.0f))

        let r3 = rect.fromTo (p4, p9)  in drawTexturedBox (r3, (st4, st9),  color4(1.0f, 1.0f, 1.0f, 1.0f))
        let r4 = rect.fromTo (p5, p10) in drawTexturedBox (r4, (st5, st10), color4(1.0f, 1.0f, 1.0f, 1.0f))
        let r5 = rect.fromTo (p6, p11) in drawTexturedBox (r5, (st6, st11), color4(1.0f, 1.0f, 1.0f, 1.0f))

        let r6 = rect.fromTo (p8, p13)  in drawTexturedBox (r6, (st8, st13),  color4(1.0f, 1.0f, 1.0f, 1.0f))
        let r7 = rect.fromTo (p9, p14)  in drawTexturedBox (r7, (st9, st14), color4(1.0f, 1.0f, 1.0f, 1.0f))
        let r8 = rect.fromTo (p10, p15) in drawTexturedBox (r8, (st10, st15), color4(1.0f, 1.0f, 1.0f, 1.0f))


    interface ICompositor with
        member x.TryGetFont (s: string) = state.UiAtlas.Fonts.TryFind s
        member x.TryGetWidget (s: string) = state.UiAtlas.Widgets.TryFind s

        member x.PresentAndReset() =
            driver.BeginUi ()
            while queue.Count > 0 do
                match queue.Dequeue() with
                | PushRegion b  -> state.Push b
                | PopRegion     -> state.Pop |> ignore
                | DrawString (fe, pos, str, col) ->
                    drawString (fe, 1.0f) pos col str
                    state.CharPos       <- vec2()

                | FillRect (s, size, col) ->
                    let off = vec2(0.5f / imWidth, 0.5f / imHeight)
                    drawIcon (white, s, size, off, col)
                | DrawIcon (ie, pos)    -> drawIcon (white, pos, size2(ie.Width  |> single, ie.Height |> single), vec2(), color4(1.0f, 1.0f, 1.0f, 1.0f))
                | DrawLine (t, s, e, col)  -> drawLine(t, s, e, col)
                | DrawRect (t, s, size, col) ->
                    drawLine(t, s, s + vec2(size.width, 0.0f), col)
                    drawLine(t, s + vec2(size.width, 0.0f), s + size.AsVec2, col)
                    drawLine(t, s + size.AsVec2, s + vec2(0.0f, size.height), col)
                    drawLine(t, s + vec2(0.0f, size.height), s, col)
                | DrawWidget (w, p, s) -> drawWidget (w, p, s)
        
            assert(queue.Count = 0)
            tryFlush (driver.MaxVertexCount, driver.MaxTriangleCount)

            assert(state.TriCount = 0)
            assert(state.VertexCount = 0)

            let dcCount = state.CountDC
            state.StackIndex    <- 0    // pop all regions
            state.CharPos       <- vec2()
            state.CountDC       <- 0
            driver.EndUi ()

            dcCount

        member x.Post cmd = queue.Enqueue cmd
        member x.Theme = theme

let create (theme: Theme, driver: IDriver) = CompositorImpl (theme, driver) :> ICompositor


