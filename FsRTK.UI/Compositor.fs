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
    [<FieldOffsetAttribute(0)>]  val Position    : vec2
    [<FieldOffsetAttribute(8)>]  val TexCoord    : vec2
    [<FieldOffsetAttribute(16)>] val Color       : vec4

    new(pos: vec2, tcoord: vec2, col: vec4)    = { Position = pos; TexCoord = tcoord; Color = col }

[<StructAttribute>]
type Triangle =
    [<FieldOffsetAttribute(0)>] val v0          : uint16
    [<FieldOffsetAttribute(2)>] val v1          : uint16
    [<FieldOffsetAttribute(4)>] val v2          : uint16

    new(v0, v1, v2) = { v0 = v0; v1 = v1; v2 = v2 }
    
    member x.Item with get i =
                        match i with
                        | 0 -> x.v0
                        | 1 -> x.v1
                        | 2 -> x.v2
                        | _ -> failwith "index out of range"


[<StructAttribute>]
type Box =
    [<FieldOffsetAttribute(0)>] val Min         : vec2
    [<FieldOffsetAttribute(8)>] val Max         : vec2

    new(mn: vec2, mx: vec2)     =
        let xMin = min mn.x mx.x
        let yMin = min mn.y mx.y
        let xMax = max mn.x mx.x
        let yMax = max mn.y mx.y
        { Min = vec2(xMin, yMin); Max = vec2(xMax, yMax) }

    new(x, y, w, h) = Box(vec2(x, y), vec2(x + w, y + h))

    member x.IsOnOrInside (p: vec2) =
        if p.x >= x.Min.x && p.x <= x.Max.x
           && p.y >= x.Min.y && p.y <= x.Max.y
        then true
        else false

    member x.Width  = x.Max.x - x.Min.x
    member x.Height = x.Max.y - x.Min.y
    member x.Size   = x.Max - x.Min

    static member intersect (a: Box) (b: Box) =
        let xMin = max a.Min.x b.Min.x
        let yMin = max a.Min.y b.Min.y
        let xMax = min a.Max.x b.Max.x
        let yMax = min a.Max.y b.Max.y
        Box(vec2(xMin, yMin), vec2(xMax, yMax))

    static member move (a: Box) (t: vec2) =
        Box(a.Min + t, a.Max + t)

    static member overlap (a: Box) (b: Box) =
        let intersection = Box.intersect a b
        intersection.Width <> 0.0f && intersection.Height <> 0.0f
    
type UiBox(vMin: Vertex, vMax: Vertex) =
    member x.Min    = vMin
    member x.Max    = vMax
    member x.Width  = vMax.Position.x - vMin.Position.x
    member x.Height = vMax.Position.y - vMin.Position.y
    member x.Size   = size2.ofVec2 (vMax.Position - vMin.Position)
    member x.ToBox  =
        let vmin = vMin.Position
        let vmax = vMax.Position
        Box(vmin, vmax)

    //
    // clip a UiBox with a box (region)
    //
    static member clip (box: Box) (uiBox: UiBox) =
        // first check if the uiBox intersects the box
        let box2    = Box(uiBox.Min.Position.x, uiBox.Min.Position.y, uiBox.Width, uiBox.Height)
        if not (Box.overlap box box2)
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

type private RendererState = {
    VB              : Vertex[]
    TB              : Triangle[]

    UiAtlas         : Atlas

    Driver          : IDriver

    RelRegionStack  : Box[] // relative region stack
    AbsRegionStack  : Box[] // absolute region stack

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
          
          RelRegionStack    = Array.create MAX_REGIONS (Box())
          AbsRegionStack    = Array.create MAX_REGIONS (Box())
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
            let headRegion = x.AbsRegionStack.[x.StackIndex - 1]
            x.AbsRegionStack.[x.StackIndex]    <- Box.intersect (Box.move b headRegion.Min) headRegion
        x.StackIndex <- x.StackIndex + 1

    member x.Pop    =
        match x.StackIndex with
        | 0 -> failwith "Region stack index below 0"
        | _ ->
            x.StackIndex <- x.StackIndex - 1
            x.RelRegionStack.[x.StackIndex], x.AbsRegionStack.[x.StackIndex]

    member x.Top    =
        x.RelRegionStack.[x.StackIndex - 1]
    
type Command =
    | PushRegion    of Box
    | PopRegion
    | DrawString    of vec2 * vec4 * FontData * string   
    | FillRect      of vec2 * size2 * vec4
    | DrawLine      of single * vec2 * vec2 * vec4
    | DrawRect      of single * vec2 * size2 * vec4
    | DrawIcon      of IconData * vec2

type Renderer(maxVertexCount: int, maxTriangleCount: int, atlas: string, driver: IDriver) =
    let atlas   = Theme.fromFile atlas
    let uiImage = atlas.ImageName
    let white   = atlas.Icons.["white"]
    
    do driver.SetAtlasImage uiImage

    let state = RendererState.create (maxVertexCount, maxTriangleCount, atlas, driver)

    let imWidth     = atlas.ImageWidth  |> single
    let imHeight    = atlas.ImageHeight |> single

    let queue   = System.Collections.Generic.Queue<Command>()

    let tryFlush (reqVerts: int, reqTris: int) =
        let width, height = state.AbsRegionStack.[0].Width, state.AbsRegionStack.[0].Height

        if state.VertexCount + reqVerts > maxVertexCount   ||
           state.TriCount    + reqTris  > maxTriangleCount
        then
            driver.RenderBatch(vec2(width, height), state.VB, state.VertexCount, state.TB, state.TriCount)
            state.VertexCount   <- 0
            state.TriCount      <- 0
            state.CountDC       <- state.CountDC + 1

    let tcoordToUV(posX: int, posY: int) : vec2 = vec2((posX |> single) / imWidth, (posY |> single) / imHeight)
    let compVec3 (v: vec2) =
        let z = (state.TriCount |> single) / (single maxTriangleCount)
        vec3(v.x, v.y, z)
        
    let addVertsTris(vCount: int, tCount: int) =
        state.VertexCount   <- state.VertexCount + vCount
        state.TriCount      <- state.TriCount    + tCount

    let drawLine (t: single, s: vec2, e: vec2, col: vec4) =
        tryFlush (4, 2)

        let wX  = white.TCoordX + white.Width / 2
        let wY  = white.TCoordY + white.Height / 2
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
        
    let drawIcon (ie: IconData, pos: vec2, size: size2, col: vec4) =
        tryFlush (4, 2)

        let uv0 = tcoordToUV(ie.TCoordX, ie.TCoordY)
        let uv1 = tcoordToUV(ie.TCoordX + ie.Width, ie.TCoordY + ie.Height)

        let u0, v0  = uv0.x, uv0.y
        let u1, v1  = uv1.x, uv1.y

        let x0, y0, x1, y1 =
            pos.x,
            pos.y,
            pos.x + size.width,
            pos.y + size.height

        let verts =
            [| Vertex(vec2(x0, y0), vec2(u0, v0), col)
               Vertex(vec2(x1, y0), vec2(u1, v0), col)
               Vertex(vec2(x1, y1), vec2(u1, v1), col)
               Vertex(vec2(x0, y1), vec2(u0, v1), col) |]

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

    let drawChar (fe: FontData, col: vec4, scale: single) (ch: char) =
        tryFlush (4, 2)

        let chInfo  = fe.CodePoints.[ch |> int]
        let x, y    = state.CharPos.x + (chInfo.Left |> single), state.CharPos.y + scale * ((fe.Size +  - chInfo.Top) |> single)

        let region = state.Top
        let v0, v1 =
            let x0, y0, x1, y1 =
                x + region.Min.x,
                y + region.Min.y,
                x + region.Min.x + scale * (chInfo.Width  |> single),
                y + region.Min.y + scale * (chInfo.Height |> single)

            let uv0, uv1 =
                tcoordToUV(chInfo.TCoordX, chInfo.TCoordY),
                tcoordToUV(chInfo.TCoordX + chInfo.Width, chInfo.TCoordY + chInfo.Height)

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

    let drawString (res: RendererState, font: FontData, scale: single) (pos: vec2) (col: vec4) (s: string) =
        printfn ""
        state.CharPos <- pos
        for ch in s do
            if ch = '\n'
            then state.CharPos <- vec2(pos.x, state.CharPos.y + scale * (font.Size |> single))
            else drawChar (font, col, scale) ch
          
    member x.TryGetFont (s: string) = state.UiAtlas.Fonts.TryFind s

    member x.PresentAndReset() =
        driver.BeginUi ()
        while queue.Count > 0 do
            match queue.Dequeue() with
            | PushRegion b  -> state.Push b
            | PopRegion     -> state.Pop |> ignore
            | DrawString (pos, col, fe, str) ->
                drawString (state, fe, 1.0f) pos col str
                state.CharPos       <- vec2()

            | FillRect (s, size, col) -> drawIcon (white, s, size, col)
            | DrawIcon (ie, pos)    -> drawIcon (white, pos, size2(ie.Width  |> single, ie.Height |> single), vec4(1.0f, 1.0f, 1.0f, 1.0f))
            | DrawLine (t, s, e, col)  -> drawLine(t, s, e, col)
            | DrawRect (t, s, size, col) ->
                drawLine(t, s, s + vec2(size.width, 0.0f), col)
                drawLine(t, s + vec2(size.width, 0.0f), s + size.AsVec2, col)
                drawLine(t, s + size.AsVec2, s + vec2(0.0f, size.height), col)
                drawLine(t, s + vec2(0.0f, size.height), s, col)
        
        assert(queue.Count = 0)
        tryFlush (maxVertexCount, maxTriangleCount)

        assert(state.TriCount = 0)
        assert(state.VertexCount = 0)

        let dcCount = state.CountDC
        state.StackIndex    <- 0    // pop all regions
        state.CharPos       <- vec2()
        state.CountDC       <- 0
        driver.EndUi ()

        dcCount

    member x.Post cmd = queue.Enqueue cmd


