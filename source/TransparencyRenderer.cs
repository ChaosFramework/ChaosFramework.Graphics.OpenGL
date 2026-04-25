using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Math.Vectors;
using OpenTK.Graphics.OpenGL;
using System;

namespace ChaosFramework.Graphics.OpenGl
{
    public class TransparencyRenderer : Disposable
    {
        public readonly int maxOverdraw;

        public LinkedList<Transparent> transparents = new LinkedList<Transparent>();
        public float solidDepthBias = 0.01f;

        public Graphics graphics { get; private set; }
        public int width { get; private set; }
        public int height { get; private set; }
        public Camera view { get; private set; }

        public bool hasSolidWorld => currentDeferred != null;

        public ShaderContainer.Entry maskEffect;

        int resultTextureBaseIndex;
        public Texture renderTexture;
        Texture tmpRender;
        Texture[] maskTexture;
        Texture worldDepthBuffer;

        int maskFramebuffer;
        int renderFramebuffer;
        int blitFramebuffer;
        bool canStencil;

        int stencilBuffer;
        int[] finishedQuery;

        DeferredShader currentDeferred;
        int currentlyRenderedLayer = 0;

        public TransparencyRenderer(Graphics g, Vector2i size, Texture worldDepthBuffer, int maxOverdraw = 25)
            : this(g, size.x, size.y, worldDepthBuffer, maxOverdraw)
        { }

        public TransparencyRenderer(Graphics g, int width, int height, Texture worldDepthBuffer, int maxOverdraw = 25)
        {
            graphics = g;
            maskEffect = g.shaders.Load("ChaosGraphics.TransparencyMask", this);
            this.worldDepthBuffer = worldDepthBuffer;
            this.maxOverdraw = maxOverdraw;
            SetBounds(width, height);
        }

        public void SetBounds(Vector2i size)
            => SetBounds(size.x, size.y);

        public void SetBounds(int width, int height)
        {
            if (this.width == width && this.height == height)
                return;

            this.width = width;
            this.height = height;
            BuildResources();
        }

        void BuildResources()
        {
            DisposeSurfaces();

            tmpRender = new Texture(
                graphics.dispatcher,
                new Texture.Parameters(
                    width,
                    height,
                    pixelType: PixelType.Float,
                    internalFormat: PixelInternalFormat.Rgba16f,
                    pixelFormat: PixelFormat.Rgba
                    )
                );
            tmpRender.SetTextureWrap(TextureWrapMode.ClampToEdge);

            maskTexture = new Texture[maxOverdraw];
            for (int i = 0; i < maskTexture.Length; i++)
                (maskTexture[i] = new Texture(
                    graphics.dispatcher,
                    new Texture.Parameters(
                        width,
                        height,
                        pixelType: PixelType.Float,
                        internalFormat: PixelInternalFormat.DepthComponent24,
                        pixelFormat: PixelFormat.DepthComponent,
                        minFilter: TextureMinFilter.Nearest
                        )
                    )
                ).SetTextureWrap(TextureWrapMode.ClampToEdge);

            renderTexture = new Texture(
                graphics.dispatcher,
                new Texture.Parameters(
                    width,
                    height,
                    pixelType: PixelType.Float,
                    pixelFormat: PixelFormat.Rgba,
                    internalFormat: PixelInternalFormat.Rgba16f,
                    minFilter: TextureMinFilter.Nearest
                    )
                );

            renderTexture.SetTextureWrap(TextureWrapMode.ClampToEdge);

            stencilBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, stencilBuffer);
            Graphics.ThrowErrors();
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.StencilIndex1, width, height);
            Graphics.ThrowErrors();

            canStencil = true;

        // Generate the Framebuffer for creating the masks front to back
        try_make_mask_fbo:
            maskFramebuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, maskFramebuffer);
            Graphics.ThrowErrors();
            if (canStencil)
                GL.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.StencilAttachment,
                    RenderbufferTarget.Renderbuffer,
                    stencilBuffer
                    );
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, maskTexture[0].textureIndex, 0);
            Graphics.ThrowErrors();
            GL.DrawBuffer(DrawBufferMode.None);
            Graphics.ThrowErrors();
            GL.ReadBuffer(ReadBufferMode.None);
            Graphics.ThrowErrors();
            FramebufferErrorCode error;

            if ((error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer)) != FramebufferErrorCode.FramebufferComplete)
            {
                // GPU does not support separate depth and stencil buffers.
                // Try again without a stencil buffer, since that's just supposed to be an optimization.
                GL.DeleteFramebuffer(maskFramebuffer);
                GL.DeleteRenderbuffer(stencilBuffer);
                Graphics.ThrowErrors();
                if (!canStencil)
                    throw new NotSupportedException("Transparency rendering doesn't work on this system.");

                canStencil = false;
                goto try_make_mask_fbo;
            }


            // Generate the Framebuffer for rendering the images back to front
            renderFramebuffer = GL.GenFramebuffer();
            Framebuffer previousFramebuffer = graphics.stateTracker.GetFramebuffer(FramebufferTarget.Framebuffer);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderFramebuffer);
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, tmpRender.textureIndex, 0);
            Graphics.ThrowErrors();
            GL.DrawBuffers(1, new[] { DrawBuffersEnum.ColorAttachment0 });
            Graphics.ThrowErrors();
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                throw new NotSupportedException("Transparency rendering doesn't work on this system.");

            // Generate a temporary Framebuffer used for blitting
            blitFramebuffer = GL.GenFramebuffer();

            finishedQuery = new int[maxOverdraw];
            GL.GenQueries(maxOverdraw, finishedQuery);
            Graphics.ThrowErrors();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, previousFramebuffer?.frameBufferName ?? 0);
            Graphics.ThrowErrors();
        }

        public void Render(DeferredShader shader)
            => Render(shader, shader.resultFramebuffer);

        public void Render(DeferredShader shader, Framebuffer target)
        {
            currentDeferred = shader;
            view = shader.view;
            Vector4i oldViewport = view.viewPort;
            view.viewPort = new Vector4i(0, 0, width, height);

            RenderInternal();
            FinalizeRender(target);
            view.viewPort = oldViewport;
            currentDeferred = null;
        }

        public void Render(Camera view, Framebuffer target)
        {
            currentDeferred = null;
            this.view = view;
            Vector4i oldViewport = view.viewPort;
            view.viewPort = new Vector4i(0, 0, width, height);

            RenderInternal();
            FinalizeRender(target);
            view.viewPort = oldViewport;
        }

        void FinalizeRender(Framebuffer target)
        {
            graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, target);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, target == null ? 0 : target.frameBufferName);
            Graphics.ThrowErrors();

            graphics.shaders.spriteEffect.SetValue("tex", tmpRender);
            GL.Enable(EnableCap.Blend);
            Graphics.ThrowErrors();
            GL.BlendFuncSeparate(
                BlendingFactorSrc.One,
                BlendingFactorDest.OneMinusSrcAlpha,
                BlendingFactorSrc.OneMinusDstAlpha,
                BlendingFactorDest.One
                );
            Graphics.ThrowErrors();
            graphics.shaders.spriteEffect.BeginPass("ScreenCustomBlend");
            Sprite.DrawFullscreen(graphics);
            graphics.shaders.spriteEffect.EndPass();
            GL.Disable(EnableCap.Blend);
            Graphics.ThrowErrors();
        }

        public void SetMaskRenderingValues(Shader shader)
        {
            shader.SetValue("sampleLayer", currentlyRenderedLayer - 1);
            shader.SetValue("compareSampler", currentlyRenderedLayer > 0 ? maskTexture[currentlyRenderedLayer - 1] : null);
            shader.SetValue("solidSampler", worldDepthBuffer);
            shader.SetValue("solidDepthBias", solidDepthBias);
            shader.SetValue("zNear", view.nearClip);
            shader.SetValue("zFar", view.farClip);
        }

        public void SetScreenSamplingValues(Shader shader)
        {
            shader.SetValue("screenTransparent", renderTexture);
            shader.SetValue("screenSolid", currentDeferred?.renderResult);
        }

        void RenderInternal()
        {
            int[] previousViewport = new int[4];
            GL.GetInteger(GetPName.Viewport, previousViewport);
            Graphics.ThrowErrors();
            GL.Viewport(0, 0, width, height);
            Graphics.ThrowErrors();

            foreach (Transparent t in transparents)
                t.PrepareVertices();

            using (graphics.stateTracker.Start())
            {
                if (canStencil)
                {
                    graphics.stateTracker.SetStencilFunc(StencilFunction.Equal, 1, 0xFF);
                    graphics.stateTracker.SetStencilOp(StencilOp.Keep, StencilOp.Decr, StencilOp.Keep);
                    graphics.stateTracker.SetEnable(EnableCap.StencilTest, true);
                    GL.ClearStencil(1);
                    Graphics.ThrowErrors();
                    GL.Clear(ClearBufferMask.StencilBufferBit);
                    Graphics.ThrowErrors();
                }

                graphics.stateTracker.SetRenderState<GlStateTracker.DepthFunc, DepthFunction>(DepthFunction.Lequal);
                graphics.stateTracker.SetEnable(EnableCap.DepthTest, true);

                GL.ClearDepth(1);
                Graphics.ThrowErrors();

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, maskFramebuffer);
                Graphics.ThrowErrors();

                // Render masks front to back
                for (currentlyRenderedLayer = 0; currentlyRenderedLayer < maxOverdraw; currentlyRenderedLayer++)
                {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, maskFramebuffer);
                    Graphics.ThrowErrors();

                    // TODO: investigate if changing framebuffer attachments like is really a good idea;
                    //       this may be the cause for our "driver issues"
                    GL.FramebufferTexture(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        maskTexture[currentlyRenderedLayer].textureIndex,
                        0
                        );
                    Graphics.ThrowErrors();
                    GL.Clear(ClearBufferMask.DepthBufferBit);
                    Graphics.ThrowErrors();
                    SetMaskRenderingValues(maskEffect);

                    foreach (Transparent t in transparents)
                        t.DrawMask(this);

                }
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderFramebuffer);
                Graphics.ThrowErrors();
                GL.ClearColor(0, 0, 0, 0);
                Graphics.ThrowErrors();
                GL.Clear(ClearBufferMask.ColorBufferBit);
                Graphics.ThrowErrors();
                graphics.stateTracker.SetRenderState<GlStateTracker.DepthFunc, DepthFunction>(DepthFunction.Equal);
                graphics.stateTracker.SetEnable(EnableCap.StencilTest, false);
                graphics.stateTracker.SetStencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Incr);
                graphics.stateTracker.SetStencilFunc(StencilFunction.Greater, 0, int.MaxValue);

                // Render transparents back to front
                int stencilValue = 0;
                for (resultTextureBaseIndex = currentlyRenderedLayer - 1; resultTextureBaseIndex >= 0; resultTextureBaseIndex--)
                {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderFramebuffer);
                    Graphics.ThrowErrors();
                    GL.FramebufferTexture(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        maskTexture[resultTextureBaseIndex].textureIndex,
                        0
                        );
                    Graphics.ThrowErrors();

                    if (canStencil)
                    {
                        GL.FramebufferRenderbuffer(
                            FramebufferTarget.Framebuffer,
                            FramebufferAttachment.StencilAttachment,
                            RenderbufferTarget.Renderbuffer,
                            stencilBuffer
                            );
                        Graphics.ThrowErrors();
                        GL.StencilFunc(StencilFunction.Always, stencilValue, int.MaxValue);
                        Graphics.ThrowErrors();
                    }

                    foreach (Transparent t in transparents)
                        t.DrawTransparent(this);

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, blitFramebuffer);
                    Graphics.ThrowErrors();
                    GL.FramebufferTexture(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.ColorAttachment0,
                        renderTexture.textureIndex,
                        0
                        );
                    Graphics.ThrowErrors();
                    if (canStencil)
                    {
                        GL.FramebufferRenderbuffer(
                            FramebufferTarget.Framebuffer,
                            FramebufferAttachment.StencilAttachment,
                            RenderbufferTarget.Renderbuffer,
                            stencilBuffer
                            );
                        Graphics.ThrowErrors();

                        GL.StencilFunc(StencilFunction.Always, stencilValue++, int.MaxValue);
                        Graphics.ThrowErrors();
                    }

                    GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
                    Graphics.ThrowErrors();
                    maskEffect.SetValue("solidSampler", tmpRender);
                    maskEffect.BeginPass("CopyScreen");
                    Sprite.DrawFullscreen(graphics);
                    maskEffect.EndPass();

                }
            }

            GL.Viewport(previousViewport[0], previousViewport[1], previousViewport[2], previousViewport[3]);
            Graphics.ThrowErrors();
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            graphics.dispatcher.RunAndAwait(DisposeSurfaces);
        }

        void DisposeSurfaces()
        {
            if (tmpRender == null) // Never initialized before
                return;
            tmpRender.Dispose();
            if (canStencil)
                GL.DeleteRenderbuffer(stencilBuffer);

            Graphics.ThrowErrors();
            for (int i = 0; i < maskTexture.Length; i++)
                maskTexture[i].Dispose();
            renderTexture.Dispose();
            GL.DeleteFramebuffer(maskFramebuffer);
            Graphics.ThrowErrors();
            GL.DeleteFramebuffer(renderFramebuffer);
            Graphics.ThrowErrors();
        }
    }
}
