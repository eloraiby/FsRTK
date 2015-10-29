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

module FsRTK.Ui.Theme

open FsRTK
open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

open FsRTK.Ui.Base
open FsRTK.Ui.Widgets


module File =
    type FontEntry = {
        Mode       : FontRenderMode
        Size       : int
        CodePoints : (int * CharInfo) []
    }

    type IconEntry = {
        Width      : int
        Height     : int
        TCoordX    : int
        TCoordY    : int
    }

    type WidgetEntry = {
        Width      : int
        Height     : int
        TCoordX    : int
        TCoordY    : int

        V0         : int
        V1         : int
        H0         : int
        H1         : int
    }

    type Atlas = {
        ImageName    : string
        ImageWidth   : int
        ImageHeight  : int
        Fonts        : (string * FontEntry)[]
        Icons        : (string * IconEntry)[]
        Widgets      : ((string * PaintStyle) * WidgetEntry)[]
    }

type ICompositor =
    abstract member TryGetFont      : string -> FontData option
    abstract member TryGetWidget    : string -> WidgetData option
    abstract member PresentAndReset : unit -> int
    abstract member Post            : Command -> unit
    abstract member Theme           : Theme
    abstract member ContentFont     : FontData
    abstract member TitleFont       : FontData
    abstract member IconFont        : FontData
    abstract member MonoFont        : FontData

//------------------------------------------------------------------------------

type FontData
with
    member this.CharSize (ch : char) =
        let chI = ch |> int
        match this.CodePoints.TryFind chI with
        | Some info -> size2 (info.Width |> single, info.Height |> single)
        | _ -> size2()

    member this.CharBox (ch : char) =
        let chI = ch |> int
        match this.CodePoints.TryFind chI with
        | Some info -> size2 (info.AdvanceX |> single, info.AdvanceY |> single)
        | _ -> size2()

    /// compute string size, this is independent of the string alignment: alignment is a function of the max line width/height as such it doesn't matter
    member this.StringSize (s: string) =
        let c, w, h =
            s
            |> Seq.toArray
            |> Array.fold (fun (cursor, width, height) ch ->
                if ch = '\n'
                then (0.0f, width, height + (this.Size + 2 |> single))  // TODO: 2 pixels ? this should be absolute value of the box
                else
                    let si = this.CharBox ch
                    let cursor = cursor + si.width
                    (cursor, max width cursor, height)) (0.0f, 0.0f, this.Size + 2 |> single)  // TODO: 2 pixels ? this should be absolute value of the box
        size2(w, h)
        
    static member from (f: File.FontEntry) = {
        Mode       = f.Mode
        Size       = f.Size
        CodePoints = f.CodePoints |> Map.ofArray }

type IconData
with
    static member from (i: File.IconEntry) = {
        IconData.Width      = i.Width   
        IconData.Height     = i.Height  
        IconData.TCoordX    = i.TCoordX 
        IconData.TCoordY    = i.TCoordY  }

type WidgetData
with
    static member from (w: File.WidgetEntry) = {
        Width      = w.Width   
        Height     = w.Height  
        TCoordX    = w.TCoordX 
        TCoordY    = w.TCoordY 

        V0         = w.V0      
        V1         = w.V1      
        H0         = w.H0      
        H1         = w.H1 }

type Atlas
with
    static member from (a: File.Atlas) = {
        ImageName    = a.ImageName
        ImageWidth   = a.ImageWidth
        ImageHeight  = a.ImageHeight
        Fonts        = a.Fonts   |> Array.map(fun (s, f) -> s, FontData.from f)   |> Map.ofArray
        Icons        = a.Icons   |> Array.map(fun (s, i) -> s, IconData.from i)   |> Map.ofArray
        Widgets      = a.Widgets |> Array.map(fun ((wt, ws), w) -> (sprintf "%O.%O" wt ws), WidgetData.from w) |> Map.ofArray      
        }

type Theme
with
    member this.ContentSize (comp: ICompositor) (wid: Widget, ps: PaintStyle) : size2 =
        match wid with
        | Label     l -> comp.ContentFont.StringSize l.Caption

        | Checkbox  _ -> failwith "not implemented"
        | RadioGroup  _ -> failwith "not implemented"
        | Button    b ->
            let labelSize = comp.ContentFont.StringSize b.Caption

            let v0, v1, h0, h1 =
                match this.Widgets.TryFind (WtButton, ps) with
                | Some wid -> wid.V0, wid.V1, wid.H0, wid.H1
                | _ -> failwith (sprintf "widget style %O for button not found" ps)
            size2(labelSize.width + (single (v0 + v1)) , labelSize.height + single(h0 + h1))

        | HSlider   _ -> failwith "not implemented"
        | VSlider   _ -> failwith "not implemented"
        | Collapsible  _ -> failwith "not implemented"
        | Container _ -> failwith "not implemented"
        | Frame     _ -> failwith "not implemented"

    member this.Draw (comp: ICompositor) (isDisabled: bool) (isHot: bool) (wid: Widget) (view: rect) =
        match wid with
        | Label     l ->
            comp.Post (Command.PushRegion view)
            comp.Post (Command.DrawString (comp.ContentFont, vec2(), l.Caption, color4(0.0f, 0.0f, 0.0f, 1.0f)))
            comp.Post Command.PopRegion

        | Button    b -> 
            let ps =
                if isDisabled
                then PaintStyle.Disabled
                elif isHot
                then PaintStyle.Hot
                else match b.State with
                     | Pressed -> PaintStyle.Active
                     | Released -> PaintStyle.Normal

            let labelSize = comp.ContentFont.StringSize b.Caption
            let v0, v1, h0, h1 =
                match this.Widgets.TryFind (WtButton, ps) with
                | Some wid -> wid.V0, wid.V1, wid.H0, wid.H1
                | _ -> failwith (sprintf "widget style %O for button not found" ps)
            
            let s = size2(labelSize.width + (single (v0 + v1)) , labelSize.height + single(h0 + h1))
            
            comp.Post (Command.PushRegion view)
            match comp.Theme.Widgets.TryFind (WidgetType.from wid, ps) with
            | Some wd ->
                comp.Post (Command.DrawWidget (wd, vec2(), s))
                comp.Post (Command.DrawString (comp.ContentFont, vec2(wd.H0 |> single, wd.V0 |> single), b.Caption, color4(0.0f, 0.0f, 0.0f, 1.0f)))
            | _ -> failwith (sprintf "button style %O not found" ps)
            comp.Post Command.PopRegion

        | Checkbox  _ -> failwith "not implemented"
        | RadioGroup  _ -> failwith "not implemented"
        | HSlider   _ -> failwith "not implemented"
        | VSlider   _ -> failwith "not implemented"
        | Collapsible  _ -> failwith "not implemented"
        | Container _ -> failwith "not implemented"
        | Frame     _ -> failwith "not implemented"

       

    static member fromFile filename =
        use f = System.IO.File.OpenText filename
        let atlas = f.ReadToEnd ()
                    |> Data.JsonValue.Parse
                    |> Data.Json.deserialize<File.Atlas>
        let atlas = Atlas.from atlas
        let widSt =
            atlas.Widgets
            |> Map.toArray
            |> Array.map(fun (s, widData) ->
                let sArr = s.Split '.'
                (sArr.[0] |> WidgetType.parse, sArr.[1] |> PaintStyle.parse), widData)
            |> Map.ofArray

        {   Name        = filename
            Atlas       = atlas
            Widgets     = widSt } : Theme

type FrameManagerState = {
    ActiveFrame : int option
    HotFrame    : int option

    PrevPointerState  : PointerState option

    Frames      : Frame[]
}


type FrameManagerState
with
    static member getContainingBoxIndex (pos: vec2, rects: rect[]) : int option =
        let rec findFirst i =
            if i < rects.Length
            then
                if rects.[i].Contains pos
                then Some i
                else findFirst (i + 1)
            else None
        findFirst 0

    static member nextHot    = FrameManagerState.getContainingBoxIndex
    static member nextActive = FrameManagerState.getContainingBoxIndex

type Presenter = {
    DrawTile           : int * vec2 -> unit
    Flush              : unit -> unit
    TileSize           : Map<int, size2>
    NormalFontTiles    : Map<char, int>
    BoldFontTiles      : Map<char, int>
    TitleFontTiles     : Map<char, int>
    WidgetTiles        : Map<string, int>
}


