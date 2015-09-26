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

//
// Note: This is very imperative, the algorithm should be rewritten to be purely functional
//
module FsRTK.Algorithms.SkyLine

open System
open System.Collections

open FsRTK.Math3D.Vector
open FsRTK.Math3D.Geometry

type Vector<'T> = System.Collections.Generic.List<'T>

type Node = {
    X           : int
    Y           : int
    Width       : int
}

type Atlas  = {
    Width           : int
    Height          : int
    Nodes           : Vector<Node>
} with
    member x.Item
        with get idx        = x.Nodes.[idx]
        and set idx v       = x.Nodes.[idx] <- v

    member x.Count          = x.Nodes.Count
    member x.Insert         = x.Nodes.Insert
    member x.RemoveAt idx   = x.Nodes.RemoveAt idx

    static member init(width, height) =
        let vec = Vector()
        vec.Add { X = 0; Y = 0; Width = width }
        { Width = width; Height = height; Nodes = vec }

let rectangleFits (atlas: Atlas, index: int, rect: irect) =
    let x = atlas.[index].X
    if x + rect.size.width > atlas.Width
    then (false, 0)
    else
        let rec loop (i, y, widthLeft) =
            if widthLeft > 0
            then
                let y = max y atlas.[i].Y
                if y + rect.size.height > atlas.Height
                then (false, 0)
                else loop(i + 1, y, widthLeft - atlas.[i].Width)
            else (true, y)
        loop (index, atlas.[index].Y, rect.size.width)


let findPositionForNewNodeBottomLeft (skyline: Atlas, rect: irect) =
    let rec loop (i, bestWidth, bestHeight, newNode, bestIndex) =
        if i < skyline.Count
        then
            let ok, y = rectangleFits (skyline, i, rect)
            
            if ok && ( y + rect.size.height < bestHeight
                  || ( y + rect.size.height = bestHeight
                  && skyline.[i].Width < bestWidth))
            then
                let bestHeight  = y + rect.size.height
                let bestIndex   = i
                let bestWidth   = skyline.[i].Width
                let newNode     = irect(skyline.[i].X, y, rect.size.width, rect.size.height)
                loop (i + 1, bestWidth, bestHeight, newNode, bestIndex)
            else loop (i + 1, bestWidth, bestHeight, newNode, bestIndex)
        else newNode, bestHeight, bestWidth, bestIndex
    loop (0, Int32.MaxValue, Int32.MaxValue, irect(), -1)
        
let mergeSkylines (skyline: Atlas) =
    let rec loop i =
        if i < skyline.Count - 1
        then
            if skyline.[i].Y = skyline.[i + 1].Y
            then
                skyline.[i]    <- { skyline.[i] with Width = skyline.[i].Width + skyline.[i + 1].Width }
                skyline.RemoveAt (i + 1)
                loop i
            else loop (i + 1)
        else ()
    loop 0

let addSkylineLevel (skyline: Atlas, skylineNodeIndex: int, rect: irect)  =
    let newNode = { X = rect.position.x; Y = rect.position.y + rect.size.height; Width = rect.size.width }
    skyline.Insert (skylineNodeIndex, newNode)

    let rec loop i =
        if i < skyline.Count
        then             
            if skyline.[i].X < skyline.[i - 1].X + skyline.[i - 1].Width
            then
                let shrink = skyline.[i - 1].X + skyline.[i - 1].Width - skyline.[i].X;
                skyline.[i]    <- { skyline.[i] with X = skyline.[i].X + shrink; Width = skyline.[i].Width - shrink }
                if skyline.[i].Width <= 0
                then
                    skyline.RemoveAt i
                    loop i
                else ()
            else ()             

    loop (skylineNodeIndex + 1)
    mergeSkylines skyline


let insert(skyLine: Atlas, rect: irect) =
    let newNode, score1, score2, index = findPositionForNewNodeBottomLeft(skyLine, rect)

    if index <> -1 && newNode.size.height = 0
    then printfn "Warning: %d,%d -> %d, %d" rect.size.width rect.size.height newNode.size.width newNode.size.height

    if index <> -1
    then
        addSkylineLevel(skyLine, index, newNode)
        Some newNode
    else None


