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

type RectangleOption =
    | DrawBounds
    | NoBounds



module Source =
    type FontEntry  = {
        FileName    : string
        SymName     : string
        Size        : int
        Mode        : RenderMode
        CodePoints  : uint32[]
    }

    type IconEntry  = {
        FileName    : string
        SymName     : string
    }

    type Atlas  = {
        Width       : int
        Height      : int
        Fonts       : FontEntry list
        Icons       : IconEntry list
    }

type irect
with
    member x.Rectangle = Rectangle(x.X, x.Y, x.Width, x.Height)

//let sourceExample () =
//    let cps =
//        "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM,./;'[]\\1234567890`-=~!@#$%^&*()_+{}|:<>?\" "
//        |> Seq.map(fun x -> x |> uint32)
//        |> Array.ofSeq
//
//    let atlas = {
//        Source.Atlas.Width  = 512
//        Source.Atlas.Height = 512
//        Source.Atlas.Fonts  =
//            [ { Source.FontEntry.FileName = "DroidSans.ttf"
//                Source.FontEntry.SymName  = "droid-sans.12.aa"
//                Source.FontEntry.Size     = 12
//                Source.FontEntry.Mode     = RenderMode.AntiAlias
//                Source.FontEntry.CodePoints   = cps }
//
//              { Source.FontEntry.FileName = "DroidSans.ttf"
//                Source.FontEntry.SymName  = "droid-sans.14.m"
//                Source.FontEntry.Size     = 14
//                Source.FontEntry.Mode     = RenderMode.Mono
//                Source.FontEntry.CodePoints   = cps.Clone() :?> _ }   ]
//
//        Source.Atlas.Icons  =
//             [
//                 { Source.IconEntry.FileName = "white.png"         ; Source.IconEntry.SymName = "white" }
//                 { Source.IconEntry.FileName = "folder-open.png"   ; Source.IconEntry.SymName = "folder-open" }
//                 { Source.IconEntry.FileName = "folder-music.png"  ; Source.IconEntry.SymName = "folder-music" }
//                 { Source.IconEntry.FileName = "folder-videos.png" ; Source.IconEntry.SymName = "folder-videos" }
//             ]
//    }
//    
//    use file    = File.CreateText ("example.source.atlas")
//    let json    = FsPickler.CreateJson(indent = true, omitHeader = true)
//    json.Serialize(file, atlas)
//
//
let compileFont (lib: SharpFont.Library)
                (f: Face)
                (rm: Ui.RenderMode)
                (size: int)
                (g: Graphics)
                (ropt: RectangleOption)
                (atlas: SkyLine.Atlas)
                (cps: uint32[]) =
    f.SetCharSize(Fixed26Dot6(0), Fixed26Dot6(size), 0u, 96u)
    use pen = new Pen(Color.Red)
    let entries =
        cps
        |> Array.map
            (fun cp ->
                let glyphIndex  = f.GetCharIndex cp
                f.LoadGlyph (glyphIndex, LoadFlags.Default, LoadTarget.Normal)
                
                let rm =
                    match rm with
                    | Ui.RenderMode.AntiAlias -> SharpFont.RenderMode.Normal
                    | Ui.RenderMode.Mono      -> SharpFont.RenderMode.Mono

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
                    if cp <> (' ' |> uint32) && cp <> ('\n' |> uint32)
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

                        g.DrawImageUnscaled(dstBmp, rect.X + 1, rect.Y + 1)
                    
                    match ropt with
                    | DrawBounds -> g.DrawRectangle(pen, rect.Rectangle)
                    | NoBounds   -> ()

                    cp, { Ui.CharInfo.AdvanceX  = advX
                          Ui.CharInfo.AdvanceY  = advY
                          Ui.CharInfo.Width     = width
                          Ui.CharInfo.Height    = height
                          Ui.CharInfo.Left      = left
                          Ui.CharInfo.Top       = top
                          Ui.CharInfo.TCoordX   = rect.X + 1
                          Ui.CharInfo.TCoordY   = rect.Y + 1
                        }
                | None -> failwith "Not enough space"

            )
    entries

let compileIcon (g: Graphics) (atlas: SkyLine.Atlas) (ropt: RectangleOption) (pt: string -> string) (icon: Source.IconEntry) =
    let bmp         = new Bitmap(pt icon.FileName)
    let rect        = irect(0, 0, bmp.Width + 2, bmp.Height + 2)
    let placement   = SkyLine.insert (atlas, rect)
    use pen         = new Pen(Color.Red)

    match placement with
    | Some rect ->
        g.DrawImageUnscaled(bmp, rect.X + 1, rect.Y + 1)

        match ropt with
        | DrawBounds -> g.DrawRectangle(pen, rect.Rectangle)
        | NoBounds   -> ()

        { Ui.IconEntry.FileName   = icon.FileName
          Ui.IconEntry.Width      = bmp.Width
          Ui.IconEntry.Height     = bmp.Height
          Ui.IconEntry.TCoordX    = rect.X + 1
          Ui.IconEntry.TCoordY    = rect.Y + 1 }
    | None -> failwith "not enough space"

//let buildAtlas (ftLib: SharpFont.Library) (atlasName: string) (ropt: RectangleOption) =
//
//    let getPath (p: string) =
//        if Path.IsPathRooted p
//        then p
//        else Path.Combine (Path.GetDirectoryName atlasName, p)
//
//    let srcAtlas =
//        let readAtlas () =
//            use file = File.OpenText (atlasName + ".source.atlas")
//            let json = FsPickler.CreateJson(omitHeader = true)
//            json.Deserialize<Source.Atlas>(file)
//        readAtlas()
//
//    use tmpBmp = new Bitmap(srcAtlas.Width, srcAtlas.Height, Imaging.PixelFormat.Format32bppArgb)
//    let bmp = tmpBmp :> Image
//    use g = Graphics.FromImage bmp
//
//    g.FillRectangle(Brushes.Transparent, 0, 0, srcAtlas.Width, srcAtlas.Height)
//    
//    let slAtlas = SkyLine.Atlas.init(srcAtlas.Width, srcAtlas.Height)
//
//    let fontMap =
//        srcAtlas.Fonts
//        |> List.map
//            (fun f ->
//                let fontName    = getPath f.FileName
//                use face = ftLib.NewFace (fontName, 0)
//                let cpMap   = compileFont ftLib face f.Mode f.Size g ropt slAtlas f.CodePoints
//                f.SymName
//                , { Ui.FontEntry.FileName   = f.FileName
//                    Ui.FontEntry.Mode       = f.Mode
//                    Ui.FontEntry.Size       = f.Size
//                    Ui.FontEntry.CodePoints = Map.ofArray cpMap })
//        |> Map.ofList
//
//    // horizontal separator
//    SkyLine.insert (slAtlas, Rectangle(0, 0, srcAtlas.Width - 1, 1)) |> ignore
//
//    let iconMap =
//        srcAtlas.Icons
//        |> List.map (fun ic -> ic.SymName, compileIcon g slAtlas ropt getPath ic)
//        |> Map.ofList
//
//    let cmplAtlas   = {
//        Ui.Atlas.ImageName    = Path.GetFileName(atlasName + ".png")
//        Ui.Atlas.ImageWidth   = bmp.Width
//        Ui.Atlas.ImageHeight  = bmp.Height
//        Ui.Atlas.Fonts        = fontMap
//        Ui.Atlas.Icons        = iconMap
//    }
//                      
//    bmp.Save (atlasName + ".png", Imaging.ImageFormat.Png)
//    
//    Ui.Atlas.toFile (atlasName + ".compiled.atlas") cmplAtlas
