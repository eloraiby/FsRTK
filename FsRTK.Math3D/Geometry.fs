﻿(*
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

module FsRTK.Math3D.Geometry

open System
open System.Runtime
open System.Runtime.InteropServices

open FsRTK
open Math3D.Vector

type size2 = struct
    val         width   : single
    val         height  : single

    new(w, h) = { width = w; height = h }

    member x.Item i =
        match i with
        | 0 -> x.width
        | 1 -> x.height
        | _ -> failwith "size2: index out of range"

    member x.AsVec2   = vec2(x.width, x.height)

    static member inline asVec2 (s: size2) = s.AsVec2
    static member inline ofVec2 (v: vec2)  = size2(v.x, v.y)

    end

type isize2 = struct
    val         width   : int
    val         height  : int

    new(w, h) = { width = w; height = h }

    member x.Item i =
        match i with
        | 0 -> x.width
        | 1 -> x.height
        | _ -> failwith "size2: index out of range"

    member x.AsIVec2   = ivec2(x.width, x.height)

    static member inline asIVec2 (s: isize2) = s.AsIVec2
    static member inline ofIVec2 (v: ivec2)  = isize2(v.x, v.y)

    end

type tri<'I> = struct
    val         v0  : 'I
    val         v1  : 'I
    val         v2  : 'I

    new(v0, v1, v2) = { v0 = v0; v1 = v1; v2 = v2 }

    end

type tri16  = tri<uint16>
type tri32  = tri<uint32>

type rect = struct
    val         position    : vec2
    val         size        : size2

    new(p, s)   = { position = p; size = s }
    new(x, y, w, h) = { position = vec2(x, y); size = size2(w, h) }
    new(p0, p1: vec2) = { position = p0; size = let s = p1 - p0 in size2(s.x, s.y) }

    member x.Contains (p: vec2) =
        p.x >= x.position.x &&
        p.y >= x.position.y &&
        p.x <= x.position.x + x.size.width &&
        p.y <= x.position.y + x.size.height

    member x.X      = x.position.x
    member x.Y      = x.position.y
    member x.Width = x.size.width
    member x.Height = x.size.height

    member x.Min    = x.position
    member x.Max    = vec2(x.position.x + x.size.width, x.position.y + x.size.height)

    member x.IsOnOrInside (p: vec2) =
        if p.x >= x.position.x && p.x <= x.position.x + x.size.width
           && p.y >= x.position.y && p.y <= x.position.y + x.size.height
        then true
        else false

    static member intersect (a: rect) (b: rect) =
        let xMin = max a.Min.x b.Min.x
        let yMin = max a.Min.y b.Min.y
        let xMax = min a.Max.x b.Max.x
        let yMax = min a.Max.y b.Max.y
        rect(vec2(xMin, yMin), vec2(xMax, yMax))

    static member move (a: rect) (t: vec2) =
        rect(a.Min + t, a.size)

    static member overlap (a: rect) (b: rect) =
        let intersection = rect.intersect a b
        intersection.Width <> 0.0f && intersection.Height <> 0.0f

    static member fromTo (start: vec2, endp: vec2) = rect(start, size2(endp.x - start.x, endp.y - start.y))

    end

type irect = struct
    val         position    : ivec2
    val         size        : isize2

    new(p, s)   = { position = p; size = s }
    new(x, y, w, h) = { position = ivec2(x, y); size = isize2(w, h) }
    new(p0, p1: ivec2) = { position = p0; size = let s = p1 - p0 in isize2(s.x, s.y) }

    member x.Contains (p: ivec2) =
        p.x >= x.position.x &&
        p.y >= x.position.y &&
        p.x <= x.position.x + x.size.width &&
        p.y <= x.position.y + x.size.height

    member x.X      = x.position.x
    member x.Y      = x.position.y
    member x.Width  = x.size.width
    member x.Height = x.size.height

    member x.Min    = x.position
    member x.Max    = ivec2(x.position.x + x.size.width, x.position.y + x.size.height)

    member x.IsOnOrInside (p: ivec2) =
        if p.x >= x.position.x && p.x <= x.position.x + x.size.width
           && p.y >= x.position.y && p.y <= x.position.y + x.size.height
        then true
        else false

    static member intersect (a: rect) (b: rect) =
        let xMin = max a.Min.x b.Min.x
        let yMin = max a.Min.y b.Min.y
        let xMax = min a.Max.x b.Max.x
        let yMax = min a.Max.y b.Max.y
        rect(vec2(xMin, yMin), vec2(xMax, yMax))

    static member move (a: rect) (t: vec2) =
        rect(a.Min + t, a.size)

    static member overlap (a: rect) (b: rect) =
        let intersection = rect.intersect a b
        intersection.Width <> 0.0f && intersection.Height <> 0.0f

    end
