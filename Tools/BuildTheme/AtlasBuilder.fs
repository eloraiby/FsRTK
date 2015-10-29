module AtlasBuilder

open System
open SharpFont
open System.Drawing
open System.IO

open FsRTK
open FsRTK.Data

open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

open FsRTK.Algorithms

open FsRTK.Ui.Base
open FsRTK.Ui.Widgets
open FsRTK.Ui.Theme


type RectangleOption =
    | DrawBounds
    | NoBounds

module Source =
    type FontEntry  = {
        FileName    : string
        Size        : int
        Mode        : FontRenderMode
        Alias       : string
    }

    type IconEntry  = {
        FileName    : string
    }

    type WidgetEntry = {
        FileName    : string
        V0          : int
        V1          : int
        H0          : int
        H1          : int
    }

    type Theme  = {
        Fonts       : FontEntry[]
        Icons       : IconEntry[]
        Widgets     : WidgetEntry[]
    } with
        static member empty() = { Fonts = [||]; Icons = [||]; Widgets = [||] }

type irect
with
    member x.Rectangle = Rectangle(x.X, x.Y, x.Width - 1, x.Height - 1)

let getExtension str = Path.GetExtension str
let stripDirectoryAndExtension str = Path.GetFileNameWithoutExtension str
let toString o = o.ToString ()

let compileFont (lib: SharpFont.Library)
                (f: Face)
                (rm: FontRenderMode)
                (size: int)
                (g: Graphics)
                (ropt: RectangleOption)
                (atlas: SkyLine.Atlas)
                (cps: int[]) =
    f.SetCharSize(Fixed26Dot6(0), Fixed26Dot6(size), 0u, 96u)
    use pen = new Pen(Color.Red)
    let entries =
        cps
        |> Array.map
            (fun cp ->
                let glyphIndex  = f.GetCharIndex (uint32 <| cp)
                f.LoadGlyph (glyphIndex, LoadFlags.Default, LoadTarget.Normal)
                
                let rm =
                    match rm with
                    | FontRenderMode.AntiAlias -> SharpFont.RenderMode.Normal
                    | FontRenderMode.Mono      -> SharpFont.RenderMode.Mono

                f.Glyph.RenderGlyph(rm)

                let ftbmp   = f.Glyph.Bitmap
            
                let width   = f.Glyph.Metrics.Width              |> single |> float |> Math.Round |> int
                let height  = f.Glyph.Metrics.Height             |> single |> float |> Math.Round |> int
                let advX    = f.Glyph.Advance.X                  |> single |> float |> Math.Round |> int
                let advY    = f.Glyph.Advance.Y                  |> single |> float |> Math.Round |> int
                let left    = f.Glyph.BitmapLeft
                let top     = f.Glyph.BitmapTop
                let newRect = SkyLine.insert (atlas, irect(0, 0, width + 2, height + 2))

                match newRect with
                | Some rect ->
                    if cp <> (' ' |> int) && cp <> ('\n' |> int)
                    then
                        use tmpBmp    = ftbmp.ToGdipBitmap Color.White
                        // convert to grayscale
                        use dstBmp    = new Bitmap(tmpBmp.Width, tmpBmp.Height, Imaging.PixelFormat.Format32bppArgb)
                        for y in 0..tmpBmp.Height - 1 do
                            for x in 0..tmpBmp.Width - 1 do
                                let sCol = tmpBmp.GetPixel(x, y)
                                let r    = sCol.R |> int
                                let g    = sCol.G |> int
                                let b    = sCol.B |> int
                                let a    = sCol.A |> int// (r + g + b) / 3
                                let dCol  = Color.FromArgb(a, 0xFF, 0xFF, 0xFF)
                                dstBmp.SetPixel(x, y, dCol)

                        g.DrawImage(dstBmp, rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2)
                    
                    match ropt with
                    | DrawBounds -> g.DrawRectangle(pen, rect.Rectangle)
                    | NoBounds   -> ()

                    cp, { CharInfo.AdvanceX  = advX
                          CharInfo.AdvanceY  = advY
                          CharInfo.Width     = width
                          CharInfo.Height    = height
                          CharInfo.Left      = left
                          CharInfo.Top       = top
                          CharInfo.TCoordX   = rect.X + 1
                          CharInfo.TCoordY   = rect.Y + 1
                        }
                | None -> failwith "Not enough space"

            )
    entries

let compileIcon (g: Graphics) (atlas: SkyLine.Atlas) (ropt: RectangleOption) (pt: string -> string) (icon: Source.IconEntry) =
    try
        use bmp         = new Bitmap(pt (icon.FileName))
        let rect        = irect(0, 0, bmp.Width + 2, bmp.Height + 2)
        let placement   = SkyLine.insert (atlas, rect)
        use pen         = new Pen(Color.Red)

        match placement with
        | Some rect ->
            g.DrawImage(bmp, rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2)

            match ropt with
            | DrawBounds -> g.DrawRectangle(pen, rect.Rectangle)
            | NoBounds   -> ()

            { File.IconEntry.Width      = bmp.Width
              File.IconEntry.Height     = bmp.Height
              File.IconEntry.TCoordX    = rect.X + 1
              File.IconEntry.TCoordY    = rect.Y + 1 }
        | None -> failwith "not enough space"
    with e ->
        failwith (sprintf "%s.png not found" icon.FileName)

let compileWidget (g: Graphics) (atlas: SkyLine.Atlas) (ropt: RectangleOption) (pt: string -> string) (widget: Source.WidgetEntry) =
    try
        use bmp         = new Bitmap(pt (widget.FileName))
        let rect        = irect(0, 0, bmp.Width + 2, bmp.Height + 2)
        let placement   = SkyLine.insert (atlas, rect)
        use pen         = new Pen(Color.Red, -1.0f)

        match placement with
        | Some rect ->
            g.DrawImage(bmp, rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2)

            match ropt with
            | DrawBounds -> g.DrawRectangle(pen, rect.Rectangle)
            | NoBounds   -> ()

            { File.WidgetEntry.Width      = bmp.Width
              File.WidgetEntry.Height     = bmp.Height
              File.WidgetEntry.TCoordX    = rect.X + 1
              File.WidgetEntry.TCoordY    = rect.Y + 1
              File.WidgetEntry.V0         = widget.V0
              File.WidgetEntry.V1         = widget.V1
              File.WidgetEntry.H0         = widget.H0
              File.WidgetEntry.H1         = widget.H1 }
        | None -> failwith "not enough space"
    with e ->
        failwith (sprintf "%s.png not found" widget.FileName)

let buildAtlas (ftLib: SharpFont.Library) (themeName: string) (size: isize2) (ropt: RectangleOption) =
    let themeFile = if getExtension themeName = ".theme" then themeName else themeName + ".theme"

    let getPath (p: string) =
        if Path.IsPathRooted p
        then p
        else Path.Combine (Path.GetDirectoryName themeFile, p)

    let srcAtlas =
        let readAtlas () =
            use file = File.OpenText themeFile
            let json = JsonValue.Parse (file.ReadToEnd ())
            Json.deserialize<Source.Theme> json
        readAtlas()

    use tmpBmp = new Bitmap(size.width, size.height, Imaging.PixelFormat.Format32bppArgb)
    let bmp = tmpBmp :> Image
    use g = Graphics.FromImage bmp

    g.FillRectangle(Brushes.Transparent, 0, 0, size.width, size.height)
    
    let slAtlas = SkyLine.Atlas.init(size.width, size.height)
    let cps =
        "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM,./;'[]\\1234567890`-=~!@#$%^&*()_+{}|:<>?\" "
        |> Seq.map(fun x -> x |> int)
        |> Array.ofSeq

    let fontMap =
        srcAtlas.Fonts
        |> Array.map
            (fun f ->
                let fontName    = getPath f.FileName
                use face = ftLib.NewFace (fontName, 0)
                let cpMap   = compileFont ftLib face f.Mode f.Size g ropt slAtlas cps
                (sprintf "%s" f.Alias)
                , { File.FontEntry.Mode       = f.Mode
                    File.FontEntry.Size       = f.Size
                    File.FontEntry.CodePoints = cpMap })

    // horizontal separator
    SkyLine.insert (slAtlas, irect(0, 0, size.width - 1, 1)) |> ignore

    let iconMap =
        srcAtlas.Icons
        |> Array.map (fun ic -> (stripDirectoryAndExtension ic.FileName), compileIcon g slAtlas ropt getPath ic)

    // horizontal separator
    SkyLine.insert (slAtlas, irect(0, 0, size.width - 1, 1)) |> ignore

    let widgetMap =
        srcAtlas.Widgets
        |> Array.map (fun w ->
            let name = stripDirectoryAndExtension w.FileName
            let state = (name |> Path.GetExtension).[1..] |> PaintStyle.parse
            let name = stripDirectoryAndExtension name

            (name, state), compileWidget g slAtlas ropt getPath w)

    let cmplAtlas   = {
        File.Atlas.ImageName    = Path.GetFileName ((stripDirectoryAndExtension themeFile) + ".png")
        File.Atlas.ImageWidth   = bmp.Width
        File.Atlas.ImageHeight  = bmp.Height
        File.Atlas.Fonts        = fontMap   
        File.Atlas.Icons        = iconMap   
        File.Atlas.Widgets      = widgetMap 
    }
                      
    bmp.Save ((stripDirectoryAndExtension themeFile) + ".png", Imaging.ImageFormat.Png)
    
    let json = Json.serialize cmplAtlas
    use file = File.CreateText ((stripDirectoryAndExtension themeFile) + ".atlas")
    file.Write (json.ToString ())
