using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosFramework.Math.Vectors;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using static ChaosFramework.Math.Clamping;
using SysCol = System.Collections.Generic;
using ChaosFramework.Graphics.Text;

namespace ChaosFramework.Graphics.OpenGl.Text
{
    public abstract partial class TextRenderer : Disposable
    {
        public static TextRenderer Create(
            Graphics graphics,
            int maxTexts,
            int glyphCapacity,
            int maxCharsPerGeometry,
            bool tryMultidDawIndirect,
            params Font[] fonts
            )
        {
            bool multiDrawIndirect = tryMultidDawIndirect && graphics.SupportsExtensions("ARB_multi_draw_indirect");
            return multiDrawIndirect
                ? (TextRenderer)new MultiDrawIndirectTextRenderer(graphics, maxTexts, glyphCapacity, maxCharsPerGeometry, fonts)
                : new MultiDrawElementsTextRenderer(graphics, maxTexts, glyphCapacity, maxCharsPerGeometry, fonts);
        }

        public readonly Graphics graphics;
        public readonly int textCapacity;
        public readonly int glyphCapacity;
        public readonly int maxCharsPerGeometry;

        readonly LinkedList<TextNode> needsBuffering;
        readonly SysCol.Dictionary<ChaosShader.Shader.SemanticMapping, int> vaos;
        readonly TextureArray sdf, col;
        readonly Font[] fonts;
        readonly Vector4f[] fontBounds;
        readonly Vector4f maxBounds;

        int vertexBuffer, indexBuffer;

        protected TextRenderer(
            Graphics graphics,
            int textCapacity,
            int glyphCapacity,
            int maxCharsPerGeometry,
            params Font[] fonts
            )
        {
            this.graphics = graphics;
            this.textCapacity = textCapacity;
            this.glyphCapacity = glyphCapacity;
            this.maxCharsPerGeometry = maxCharsPerGeometry;
            this.fonts = fonts;

            needsBuffering = new LinkedList<TextNode>();
            vaos = new SysCol.Dictionary<ChaosShader.Shader.SemanticMapping, int>();

            Vector2i maxBoundsSDF = Vector2i.EMPTY;
            Vector2i maxBoundsCOL = Vector2i.EMPTY;
            fontBounds = new Vector4f[fonts.Length];
            for (int i = 0; i < fonts.Length; i++)
            {
                maxBoundsSDF = Max(maxBoundsSDF, fonts[i].sdf.args.size);
                maxBoundsCOL = Max(maxBoundsCOL, fonts[i].col.args.size);
                fontBounds[i] = new Vector4i(fonts[i].sdf.args.size, fonts[i].col.args.size);
            }
            maxBounds = new Vector4f(maxBoundsSDF, maxBoundsCOL);

            sdf = new TextureArray(
                graphics.dispatcher,
                new Texture.Parameters(
                    maxBoundsSDF.x,
                    maxBoundsSDF.y,
                    pixelType: PixelType.UnsignedByte,
                    pixelFormat: PixelFormat.Red,
                    internalFormat: PixelInternalFormat.R8,
                    minFilter: TextureMinFilter.Linear,
                    magFilter: TextureMagFilter.Linear
                    ),
                fonts.Length
                );
            col = new TextureArray(
                graphics.dispatcher,
                new Texture.Parameters(
                    maxBoundsCOL.x,
                    maxBoundsCOL.y,
                    pixelType: PixelType.UnsignedByte,
                    pixelFormat: PixelFormat.Rgba,
                    internalFormat: PixelInternalFormat.Rgba8,
                    minFilter: TextureMinFilter.Nearest,
                    magFilter: TextureMagFilter.Nearest
                    ),
                fonts.Length
                );

            graphics.dispatcher.RunAndAwait(Build);
        }
        public abstract TextBuffer CreateRenderContext();

        internal abstract void UnuseGeometry(Text text);

        internal abstract bool GetOrCreateTextNode(Text user, out TextNode result);

        void Build()
        {
            int[] tmpFBO = new int[2];
            GL.GenFramebuffers(2, tmpFBO);
            Graphics.ThrowErrors();

            int oldReadFBO = GL.GetInteger(GetPName.ReadFramebufferBinding);
            Graphics.ThrowErrors();
            int oldDrawFBO = GL.GetInteger(GetPName.DrawFramebufferBinding);
            Graphics.ThrowErrors();

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, tmpFBO[0]);
            Graphics.ThrowErrors();
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, tmpFBO[1]);
            Graphics.ThrowErrors();
            for (int i = 0; i < fonts.Length; i++)
                WriteFontMapsToTextureArrays(fonts[i], i);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, oldReadFBO);
            Graphics.ThrowErrors();
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, oldDrawFBO);
            Graphics.ThrowErrors();

            GL.DeleteFramebuffers(2, tmpFBO);
            Graphics.ThrowErrors();

            BuildMeshBuffers(glyphCapacity, maxCharsPerGeometry);
        }

        void WriteFontMapsToTextureArrays(Font f, int destinationLayer)
        {
            GL.FramebufferTexture(
                FramebufferTarget.ReadFramebuffer,
                FramebufferAttachment.ColorAttachment0,
                f.sdf.textureIndex,
                0
                );
            Graphics.ThrowErrors();
            GL.FramebufferTextureLayer(
                FramebufferTarget.DrawFramebuffer,
                FramebufferAttachment.ColorAttachment0,
                sdf.textureIndex,
                0,
                destinationLayer
                );
            Graphics.ThrowErrors();
            GL.BlitFramebuffer(
                0, 0,
                f.sdf.args.width, f.sdf.args.height,
                0, 0,
                f.sdf.args.width, f.sdf.args.height,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Nearest
                );
            Graphics.ThrowErrors();

            GL.FramebufferTexture(
                FramebufferTarget.ReadFramebuffer,
                FramebufferAttachment.ColorAttachment0,
                f.col.textureIndex,
                0
                );
            Graphics.ThrowErrors();
            GL.FramebufferTextureLayer(
                FramebufferTarget.DrawFramebuffer,
                FramebufferAttachment.ColorAttachment0,
                col.textureIndex,
                0,
                destinationLayer
                );
            Graphics.ThrowErrors();
            GL.BlitFramebuffer(
                0, 0,
                f.col.args.width, f.col.args.height,
                0, 0,
                f.col.args.width, f.col.args.height,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Nearest
                );
            Graphics.ThrowErrors();
        }

        void BuildMeshBuffers(int glyphCapacity, int maxCharsPerGeometry)
        {
            int[] buffers = new int[2];
            GL.GenBuffers(2, buffers);
            Graphics.ThrowErrors();
            vertexBuffer = buffers[0];
            indexBuffer = buffers[1];
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            Graphics.ThrowErrors();
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                TextVertex.SIZE_IN_BYTES * 4 * glyphCapacity,
                System.IntPtr.Zero,
                BufferUsageHint.StreamDraw
                );
            Graphics.ThrowErrors();
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            Graphics.ThrowErrors();

            ushort[] ind = new ushort[maxCharsPerGeometry * 6];
            ushort currentIndex = 0;
            for (int i = 0; i < ind.Length; i += 6)
            {
                ind[i] = currentIndex;
                ind[i + 1] = ind[i + 4] = (ushort)(currentIndex + 1);
                ind[i + 2] = ind[i + 3] = (ushort)(currentIndex + 2);
                ind[i + 5] = (ushort)(currentIndex + 3);
                currentIndex += 4;
            }

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            Graphics.ThrowErrors();
            GL.BufferData(BufferTarget.ElementArrayBuffer, sizeof(ushort) * ind.Length, ind, BufferUsageHint.StaticDraw);
            Graphics.ThrowErrors();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            Graphics.ThrowErrors();
        }

        public void SetValues(ChaosShader.Shader fx)
        {
            fx.SetValue("sdfParams", new Vector4f(0.5f, 2.117f, 1, 0));
            fx.SetValue(fx.GetParameterBySemantic("TEX"), col);
            fx.SetValue(fx.GetParameterBySemantic("SDF"), sdf);
            fx.SetValue("textureBounds", fontBounds);
            fx.SetValue("maxBounds", maxBounds);
        }

        public void DrawTexts(
            TextBuffer renderContext,
            ChaosShader.Shader shader,
            string pass,
            SysCol.IEnumerable<Text> texts
            )
        {
            renderContext.Clear();
            renderContext.Add(texts);
            renderContext.Flush();

            DrawTexts(shader, pass, renderContext);
        }

        public void DrawTexts(ChaosShader.Shader shader, string pass, TextBuffer texts)
        {
            if (texts.SkipDraw())
                return;

            UpdateTextBuffers();
            shader.SetValue("numMetaPerRow", texts.numMetaPerRow);
            shader.SetValue("metaSampler", texts.metaDataTex);
            SetValues(shader);
            BindVAO(shader.BeginPass(pass));

            texts.ExecuteCommands();
            GL.BindVertexArray(0);
            Graphics.ThrowErrors();
            shader.EndPass();
        }

        internal TextGeometry GetGeometry(TextGeometryDescription args, Text user)
        {
            bool newMetaIndex = false;
            if (user.geometry != null)
                if (user.geometry.args != args)
                    UnuseGeometry(user);
                else
                    return user.geometry;

            user._geometry = new TextGeometry(args);

            TextNode result;
            if (!GetOrCreateTextNode(user, out result))
            {
                result.geometry = user._geometry;
                result.usageData = GetMetaNode(1);
                newMetaIndex = true;
            }

            result.users.Add(user);
            if (result.users.Count > result.usageData.meta.Length)
            {
                result.usageData.baseMeta = TextUsageNode.FREED;
                result.usageData = GetMetaNode(result.users.Count * 2);
                newMetaIndex = true;
            }

            if (newMetaIndex)
                for (int i = 0; i < result.geometry.vertices.Length; i++)
                    result.geometry.vertices[i].position.w = result.usageData.baseMeta;

            needsBuffering.Add(result);
            return result.geometry;
        }

        internal int GetFontIndex(Font font)
            => System.Array.IndexOf(fonts, font);

        int BindVAO(ChaosShader.Shader.SemanticMapping mapping)
        {
            Graphics.ThrowErrors();
            int vao;
            if (!vaos.TryGetValue(mapping, out vao))
            {
                vaos[mapping] = vao = GL.GenVertexArray();
                Graphics.ThrowErrors();
                GL.BindVertexArray(vao);
                Graphics.ThrowErrors();
                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
                Graphics.ThrowErrors();

                int pos;
                int stride = Marshal.SizeOf<TextVertex>();
                if (mapping.mapping.TryGetValue("POSITION0", out pos)
                 || mapping.mapping.TryGetValue("POSITION", out pos))
                {
                    GL.VertexAttribPointer(pos, 4, VertexAttribPointerType.Float, false, stride, 0);
                    Graphics.ThrowErrors();
                    GL.EnableVertexAttribArray(pos);
                    Graphics.ThrowErrors();
                }

                if (mapping.mapping.TryGetValue("TEXCOORD0", out pos)
                 || mapping.mapping.TryGetValue("TEXCOORD", out pos))
                {
                    GL.VertexAttribPointer(pos, 4, VertexAttribPointerType.Float, false, stride, sizeof(float) * 4);
                    Graphics.ThrowErrors();
                    GL.EnableVertexAttribArray(pos);
                    Graphics.ThrowErrors();
                }

                if (mapping.mapping.TryGetValue("COLOR0", out pos)
                 || mapping.mapping.TryGetValue("COLOR", out pos))
                {
                    GL.VertexAttribPointer(pos, 4, VertexAttribPointerType.Float, false, stride, sizeof(float) * 8);
                    Graphics.ThrowErrors();
                    GL.EnableVertexAttribArray(pos);
                    Graphics.ThrowErrors();
                }

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
                Graphics.ThrowErrors();
            }
            else
            {
                GL.BindVertexArray(vao);
                Graphics.ThrowErrors();
            }
            return vao;
        }

        void UpdateTextBuffers()
        {
            if (needsBuffering.empty)
                return;

            Graphics.ThrowErrors();
            int charSize = TextVertex.SIZE_IN_BYTES * 4;
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            Graphics.ThrowErrors();
            foreach (TextNode node in needsBuffering)
            {
                if (node.users.Count <= 0)
                    continue;

#if DEBUG
                if (node.baseVertex < 0)
                    throw new System.Exception("Illegal Textnode");

                int bufferSize;
                GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out bufferSize);
                Graphics.ThrowErrors();
                if (bufferSize <= 0)
                    throw new System.Exception("Broken Buffer");

                Graphics.ThrowErrors();
#endif

                GL.BufferSubData(BufferTarget.ArrayBuffer,
                    new System.IntPtr(node.baseVertex * charSize),
                    charSize * node.geometry.numPrintedChars,
                    node.geometry.vertices
                    );
                Graphics.ThrowErrors();
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            Graphics.ThrowErrors();
            needsBuffering.Clear();
        }

        internal void FreeNode(TextNode result)
        {
            result.baseVertex = TextNode.FREED;
            result.usageData.baseMeta = TextUsageNode.FREED;
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            sdf.Dispose();
            col.Dispose();
        }
    }
}
