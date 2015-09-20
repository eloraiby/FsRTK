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

    for mtl in scene.Materials do
        printfn "- material.name       : %s" mtl.Name
        printfn "- material.diffuse    : %s" mtl.TextureDiffuse.FilePath

    for mesh in scene.Meshes do
        printfn "- mesh.name           : %s" mesh.Name
        printfn "- mesh.bone count     : %d" mesh.BoneCount
        printfn "- mesh.face count     : %d" mesh.FaceCount
        printfn "- mesh.vertex count   : %d" mesh.VertexCount

        let positions   = mesh.Vertices |> Seq.map (fun v -> vec3(v.X, v.Y, v.Z)) |> Seq.toArray
        let normals     = mesh.Normals  |> Seq.map (fun n -> vec3(n.X, n.Y, n.Z)) |> Seq.toArray

//        let m = { RenderMesh.Vertices =
//                    mesh.Vertices
//                    |> Seq.map
//                        (fun v ->
//                            v.X)
//                    |> Seq.toArray
//                  Triangles = [||] }

        for b in mesh.Bones do
            printfn "-- bone.Name           : %s" b.Name
            printfn "-- bone.weight count   : %d" b.VertexWeightCount

    0 // return an integer exit code
