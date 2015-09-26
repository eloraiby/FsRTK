module SkyLine

open System
open System.Collections
open System.Drawing

open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

open FsRTK.Algorithms.SkyLine

let testSkyline() =
    let imWidth   = 512
    let imHeight  = 512
    use bmp = new Bitmap(imWidth, imHeight, Imaging.PixelFormat.Format32bppArgb)

    use g = Graphics.FromImage bmp
    use m2 = new Drawing2D.Matrix(1.0f, 0.0f, 0.0f, -1.0f, 0.0f, imHeight |> float32)
    let m1 = g.Transform.Clone()
    m1.Multiply m2
    g.Transform <-  m1
    
    use pen = new Pen(Color.Red)

    //g.Clear (SystemColors.Control)
    g.FillRectangle(Brushes.Black, 0, 0, imWidth, imHeight)

    let origArray =
        let rand = Random()
        Array.init 256 (fun i ->
                            let width = 1 + rand.Next(64)
                            let height = ((width |> float) * (rand.NextDouble() + rand.NextDouble()) |> int) + 1
                            irect(0, 0, width, height))
    let rects =
        let vec = Vector<irect>()
        for rect in origArray do
            vec.Add rect
        vec

    let skyline = Atlas.init(imWidth, imHeight)
    let dst = 
        origArray
        |> Array.map(fun r -> insert (skyline, r))

    for rect in dst do
        match rect with
        | Some rect ->
            let rect = Rectangle(rect.position.x, rect.position.y, rect.size.width, rect.size.height)
            g.FillRectangle(Brushes.Blue, rect)
            g.DrawRectangle(pen, rect)
        | None -> ()

    bmp.Save "skyline.png"

