// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open FsRTK
open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

open FsRTK.Gles2

type Mesh32<'V when 'V : struct> = {
    Vertices    : 'V[]
    Triangles   : tri32[]
}

type Vertex =
    struct
        val     Position    : vec3
        val     Normal      : vec3
        val     Tangent     : vec3
        val     BiTangent   : vec3
        val     TexCoords   : vec2
        val     Weights     : vec4
        val     Bones       : ivec4

        new(p, n, tn, btn, tc, w, b)    = { Position = p; Normal = n; Tangent = tn; BiTangent = btn; TexCoords = tc; Weights = w; Bones = b }
    end

type RenderMesh = Mesh32<Vertex>

[<EntryPoint>]
let main argv = 
    printfn "%A" argv

    use ctx = new Assimp.AssimpContext()
    let scene = ctx.ImportFile ("Seymour.dae", Assimp.PostProcessPreset.TargetRealTimeFast)

    printfn "has animation  : %b" scene.HasAnimations
    printfn "has cameras    : %b" scene.HasCameras
    printfn "has lights     : %b" scene.HasLights
    printfn "has materials  : %b" scene.HasMaterials
    printfn "has meshes     : %b" scene.HasMeshes
    printfn "has textures   : %b" scene.HasTextures

    let rec foldNodes (n: Assimp.Node) (l: int) =
        let str = System.String (Array.init (4 * l) (fun c -> ' '))

        for i in n.Children do
            printfn "%s- node.name           : %s" str n.Name

        for i in n.Children do
            foldNodes i (l + 1)

    foldNodes scene.RootNode 0

    for mtl in scene.Materials do
        printfn "- material.name       : %s" mtl.Name
        printfn "- material.diffuse    : %s" mtl.TextureDiffuse.FilePath

    let meshes =
        scene.Meshes
        |> Seq.map
            (fun mesh ->       
                printfn "- mesh.name           : %s" mesh.Name
                printfn "- mesh.bone count     : %d" mesh.BoneCount
                printfn "- mesh.face count     : %d" mesh.FaceCount
                printfn "- mesh.vertex count   : %d" mesh.VertexCount

                // start by building the vertices
                let positions   = mesh.Vertices                      |> Seq.map (fun v  -> vec3(v.X, v.Y, v.Z))    |> Seq.toArray
                let normals     = mesh.Normals                       |> Seq.map (fun n  -> vec3(n.X, n.Y, n.Z))    |> Seq.toArray
                let tangents    = mesh.Tangents                      |> Seq.map (fun t  -> vec3(t.X, t.Y, t.Z))    |> Seq.toArray
                let biTangents  = mesh.BiTangents                    |> Seq.map (fun bt -> vec3(bt.X, bt.Y, bt.Z)) |> Seq.toArray
                let biNormals   = mesh.BiTangents                    |> Seq.map (fun bn -> vec3(bn.X, bn.Y, bn.Z)) |> Seq.toArray
                let tcoords     = mesh.TextureCoordinateChannels.[0] |> Seq.map (fun tc -> vec2(tc.X, tc.Y))       |> Seq.toArray

                // build the triangles
                let tris        = mesh.Faces                         |> Seq.map (fun f -> tri32(f.Indices.[0] |> uint32, f.Indices.[1] |> uint32, f.Indices.[2] |> uint32)) |> Seq.toArray

                let bones =
                    mesh.Bones
                    |> Seq.toArray
                    |> Array.mapi
                        (fun i bone ->
                            printfn "-- bone.Name           : %s" bone.Name
                            printfn "-- bone.weight count   : %d" bone.VertexWeightCount
                            (i, bone.VertexWeights |> Seq.map(fun vw -> vw.VertexID, vw.Weight)))

                let m = { RenderMesh.Vertices =
                            mesh.Vertices
                            |> Seq.mapi
                                (fun i v ->
                                    Vertex( positions.[i]
                                          , normals.[i]
                                          , tangents.[i]
                                          , biTangents.[i]
                                          , tcoords.[i]
                                          , vec4()
                                          , ivec4()))
                            |> Seq.toArray
                          Triangles = [||] }

                m)
        |> Seq.toArray

    0 // return an integer exit code
