module FsRTK.Ui.Gles2Driver

open System

open FsRTK

open FsRTK.Math3D.Vector
open FsRTK.Math3D.Matrix
open FsRTK.Math3D.Geometry

open FsRTK.Ui.Base
open FsRTK.Ui.Widgets
open FsRTK.Ui.Theme
open FsRTK.Ui.Compositor

open FsRTK.Gles2

open System.Collections
open System.Drawing
open System.Runtime.InteropServices


type Vertex     = Compositor.Vertex
type Triangle   = Compositor.Triangle

type private OpenGLDriverData = {
    // objects
    VsId    : int
    FsId    : int
    ProgId  : int
    TexId   : int
    VbId    : int
    IbId    : int

    // attribs
    VertexPosition  : int
    VertexTexCoord  : int
    VertexColor     : int

    // uniforms
    Viewport        : int
    UiTex           : int

} with
    member x.Dispose() =
        glDeleteBuffers  [| x.VbId |]
        glDeleteBuffers  [| x.IbId |]
        glDeleteTextures [| x.TexId |]
        glDeleteProgram x.ProgId
        glDeleteShader  x.FsId
        glDeleteShader  x.VsId

type OpenGLDriver(maxVertexCount: int, maxTriangleCount: int) =
    let uiVs =
        "#version 130\n" +
        "uniform highp vec2 Viewport;\n" +
        "attribute highp vec2 VertexPosition;\n" +
        "attribute highp vec2 VertexTexCoord;\n" +
        "attribute highp vec4 VertexColor;\n" +
        "varying highp vec2 texCoord;\n" +
        "varying highp vec4 vertexColor;\n" +
        "void main()\n" +
        "{\n" +
        "    vertexColor = VertexColor;\n" +
        "    texCoord = VertexTexCoord;\n" +
        "    highp vec2 pos = vec2((VertexPosition * 2.0) / Viewport);\n" +
        "    gl_Position = vec4(pos.x - 1.0, 1.0 - pos.y, 0.0, 1.0);\n" +
        "}\n"

    let uiFs =
        "#version 130\n" +
        "varying highp vec2 texCoord;\n" +
        "varying highp vec4 vertexColor;\n" +
        "uniform sampler2D Texture;\n" +
        "void main()\n" +
        "{\n" +
        "    gl_FragColor = vertexColor * texture2D(Texture, texCoord);\n" +
        "}\n"

    let setAtlasImage (imgName: string, id: int) =
        use atlasImage = new Bitmap(imgName)
        glBindTexture(GLenum.GL_TEXTURE_2D, id)
    
        let bmpData = atlasImage.LockBits(new Rectangle(0, 0, atlasImage.Width, atlasImage.Height), Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
        glTexParameteri(GLenum.GL_TEXTURE_2D, GLenum.GL_TEXTURE_WRAP_S, GLenum.GL_CLAMP_TO_EDGE |> int)
        glTexParameteri(GLenum.GL_TEXTURE_2D, GLenum.GL_TEXTURE_WRAP_T, GLenum.GL_CLAMP_TO_EDGE |> int)
        glTexParameteri(GLenum.GL_TEXTURE_2D, GLenum.GL_TEXTURE_MAG_FILTER, GLenum.GL_LINEAR |> int)
        glTexParameteri(GLenum.GL_TEXTURE_2D, GLenum.GL_TEXTURE_MIN_FILTER, GLenum.GL_LINEAR |> int)
        glTexImage2DPtr(GLenum.GL_TEXTURE_2D, 0, GLenum.GL_RGBA |> int, bmpData.Width, bmpData.Height, 0, GLenum.GL_RGBA, GLenum.GL_UNSIGNED_BYTE, bmpData.Scan0)

        atlasImage.UnlockBits bmpData

    let buildResources() =
        let vsId    = glCreateShader(GLenum.GL_VERTEX_SHADER)
        assert(glGetError() = GLenum.GL_NO_ERROR)

        let fsId    = glCreateShader(GLenum.GL_FRAGMENT_SHADER)
        assert(glGetError() = GLenum.GL_NO_ERROR)

        let progId  = glCreateProgram()
        assert(glGetError() = GLenum.GL_NO_ERROR)

        let texId   = (glGenTextures 1).[0]
        assert(glGetError() = GLenum.GL_NO_ERROR)

        let vbId    = (glGenBuffers 1).[0]
        assert(glGetError() = GLenum.GL_NO_ERROR)

        let ibId    = (glGenBuffers 1).[0]
        assert(glGetError() = GLenum.GL_NO_ERROR)

        glBindBuffer(GLenum.GL_ARRAY_BUFFER, vbId)
        assert(glGetError() = GLenum.GL_NO_ERROR)
        glBufferDataInit<Vertex>(GLenum.GL_ARRAY_BUFFER, maxVertexCount, GLenum.GL_DYNAMIC_DRAW)
        assert(glGetError() = GLenum.GL_NO_ERROR)

        glBindBuffer(GLenum.GL_ELEMENT_ARRAY_BUFFER, ibId)
        assert(glGetError() = GLenum.GL_NO_ERROR)
        glBufferDataInit<Triangle>(GLenum.GL_ELEMENT_ARRAY_BUFFER, maxTriangleCount, GLenum.GL_DYNAMIC_DRAW)
        assert(glGetError() = GLenum.GL_NO_ERROR)

        let deleteAll() =
            glDeleteBuffers [| vbId |]
            glDeleteBuffers [| ibId |]
            glDeleteTextures [| texId |]
            glDeleteProgram progId
            glDeleteShader fsId
            glDeleteShader vsId

        glShaderSource (vsId, [| uiVs |])
        glShaderSource (fsId, [| uiFs |])

        glCompileShader vsId
        if glGetError() <>  GLenum.GL_NO_ERROR
        then
            deleteAll()
            failwith (sprintf "Error compiling vertex shader (%A):" (glGetError()))

        printfn "Vertex shader log: %s" (glGetShaderInfoLog(vsId |> int))

        glCompileShader fsId
        if glGetError() <> GLenum.GL_NO_ERROR
        then
            deleteAll()
            failwith (sprintf "Error compiling fragment shader (%A):" (glGetError()))

        printfn "Fragment shader log: %s" (glGetShaderInfoLog(fsId |> int))

        glAttachShader(progId, vsId)
        glAttachShader(progId, fsId)
        glLinkProgram progId

        if glGetError() <> GLenum.GL_NO_ERROR
        then
            deleteAll()
            failwith (sprintf "Error linking program (%A):" (glGetError()))
        printfn "program log: %s" (glGetProgramInfoLog(progId |> int))

        let vertexPosition  = glGetAttribLocation(progId, "VertexPosition")
        assert(glGetError() = GLenum.GL_NO_ERROR)
        let vertexTexCoord  = glGetAttribLocation(progId, "VertexTexCoord")
        assert(glGetError() = GLenum.GL_NO_ERROR)
        let vertexColor     = glGetAttribLocation(progId, "VertexColor")
        assert(glGetError() = GLenum.GL_NO_ERROR)

        let viewport        = glGetUniformLocation(progId, "Viewport")
        assert(glGetError() = GLenum.GL_NO_ERROR)
        let uiTex           = glGetUniformLocation(progId, "Texture")
        assert(glGetError() = GLenum.GL_NO_ERROR)


        { OpenGLDriverData.VsId     = vsId
          OpenGLDriverData.FsId     = fsId
          OpenGLDriverData.ProgId   = progId
          OpenGLDriverData.TexId    = texId
          OpenGLDriverData.VbId     = vbId
          OpenGLDriverData.IbId     = ibId

          OpenGLDriverData.VertexPosition   = vertexPosition
          OpenGLDriverData.VertexTexCoord   = vertexTexCoord
          OpenGLDriverData.VertexColor      = vertexColor

          OpenGLDriverData.Viewport = viewport
          OpenGLDriverData.UiTex    = uiTex }

    let resources = buildResources()

    interface Ui.Compositor.IDriver with
        member x.MaxVertexCount     = maxVertexCount
        member x.MaxTriangleCount   = maxTriangleCount

        member x.SetAtlasImage imgName  =
            try  setAtlasImage (imgName, resources.TexId)
            with e -> raise e

        member x.BeginUi()  =
            glEnable GLenum.GL_BLEND
            glBlendFunc (GLenum.GL_SRC_ALPHA, GLenum.GL_ONE_MINUS_SRC_ALPHA)
            glDisable GLenum.GL_DEPTH_TEST
            glDisable GLenum.GL_CULL_FACE

            glUseProgram resources.ProgId

            glActiveTexture GLenum.GL_TEXTURE0
            glBindTexture (GLenum.GL_TEXTURE_2D, resources.TexId)
            glUniform1i (resources.UiTex, 0)

            glBindBuffer (GLenum.GL_ARRAY_BUFFER, resources.VbId)
            glBindBuffer (GLenum.GL_ELEMENT_ARRAY_BUFFER, resources.IbId)

            glVertexAttribPointer(resources.VertexPosition, 2, GLenum.GL_FLOAT, false, Marshal.SizeOf(typeof<Vertex>), 0)
            glVertexAttribPointer(resources.VertexTexCoord, 2, GLenum.GL_FLOAT, false, Marshal.SizeOf(typeof<Vertex>), 8)
            glVertexAttribPointer(resources.VertexColor,    4, GLenum.GL_FLOAT, false, Marshal.SizeOf(typeof<Vertex>), 16)
            
            glEnableVertexAttribArray resources.VertexPosition
            glEnableVertexAttribArray resources.VertexTexCoord
            glEnableVertexAttribArray resources.VertexColor

        member x.EndUi()    =
            glDisableVertexAttribArray resources.VertexColor
            glDisableVertexAttribArray resources.VertexTexCoord
            glDisableVertexAttribArray resources.VertexPosition

            glUseProgram 0
            glEnable GLenum.GL_CULL_FACE
            glEnable GLenum.GL_DEPTH_TEST
            glDisable GLenum.GL_BLEND

        member x.RenderBatch (viewport, vb, vCount, tb, tCount) =

            glBufferSubData(GLenum.GL_ARRAY_BUFFER, 0, vb)
            assert(glGetError() = GLenum.GL_NO_ERROR)
            
            glBufferSubData(GLenum.GL_ELEMENT_ARRAY_BUFFER, 0, tb)
            assert(glGetError() = GLenum.GL_NO_ERROR)

            glUniform2f(resources.Viewport, viewport.x, viewport.y)
            assert(glGetError() = GLenum.GL_NO_ERROR)

            glDrawElements(GLenum.GL_TRIANGLES, tCount * 3, GLenum.GL_UNSIGNED_SHORT, 0)
            assert(glGetError() = GLenum.GL_NO_ERROR)


    interface IDisposable with
        member x.Dispose() = resources.Dispose()
