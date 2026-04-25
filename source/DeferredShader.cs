using ChaosFramework.Core;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using OpenTK.Graphics.OpenGL;
using System;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl
{
    using ChaosShader;
    using Lights;
    using Lights.Intrinsic;

    public sealed class DeferredShader : Disposable
    {
        public enum Layers : byte
        {
            Position,
            Normal,
            Surface,
            Emissive,
            Diffuse,
            Specular
        }

        public static readonly int NUM_LAYERS = Enum.GetNames(typeof(Layers)).Length;

        public event Action onSetBounds;

        readonly OrderedLights lights;

        public readonly Graphics graphics;
        public readonly Camera view;

        readonly SysCol.Dictionary<Type, LightInstancerBase> lightInstancers;

        readonly DeferredShader parent;
        readonly DeferredShaderIntrinsicLights[] intrinsics;
        readonly Shader shader;

        public Light currentShadowCaster { get; private set; }

        public Framebuffer worldMaterialFramebuffer { get; private set; }
        public Framebuffer worldFramebuffer { get; private set; }
        public Framebuffer materialFramebuffer { get; private set; }
        public Framebuffer resultFramebuffer { get; private set; }

        public Texture renderResult { get; private set; }
        public Texture[] layers { get; private set; }
        public Texture depthBuffer { get; private set; }

        public Vector2i size { get; private set; }

        public int width => size.x;
        public int height => size.y;

        DeferredShader(
            Graphics graphics,
            Camera view,
            Vector2i size,
            DeferredShaderIntrinsicLights[] intrinsics
            )
        {
            this.graphics = graphics;
            this.view = view;
            this.size = size;
            this.intrinsics = intrinsics;

            layers = new Texture[NUM_LAYERS];
            lightInstancers = new SysCol.Dictionary<Type, LightInstancerBase>();
        }

        public DeferredShader(
            Graphics graphics,
            Camera view,
            Vector2i size,
            LightSet lights,
            DeferredShaderIntrinsicLights[] intrinsics,
            LightInstancerBase[] instancers
            ) : this(graphics, view, size, intrinsics)
        {
            this.lights = new OrderedLights(lights, view);

            CreateInstancers(instancers);

            if (!(intrinsics?.Length > 0))
                shader = graphics.shaders.Load("ChaosGraphics.DeferredShader", this);
            else
            {
                // TODO: use the same shader for equivalent combinations of intrinsics
                using (Disposable m = new Disposable())
                {
                    CodeBlock code = (CodeBlock)Shaders.code.Load("ChaosGraphics.DeferredShaderWithIntrinsics", m).content.Clone();
                    foreach (DeferredShaderIntrinsicLights i in intrinsics)
                        i.AddIntrinsics(Shaders.code, code);

                    shader = new Shader(graphics, code, graphics.coreProfile);
                    shader.Compile();
                }
            }
            BuildResources();
        }

        DeferredShader(DeferredShader parent)
             : this(parent.graphics, parent.view, parent.size, parent.intrinsics)
        {
            this.parent = parent;

            shader = parent.shader;
            depthBuffer = parent.depthBuffer;
            lights = parent.lights;
            lightInstancers = parent.lightInstancers;
            parent.onSetBounds += BuildResources;
            parent.AddOnDispose(Dispose);

            BuildResources();
        }

        public DeferredShader CreateChild()
            => new DeferredShader(this);

        public void SetBounds(Vector2i size)
        {
            if (this.size == size)
                return;

            this.size = size;
            BuildResources();

            onSetBounds?.Invoke();
        }

        void CreateInstancers(LightInstancerBase[] instancers)
        {
            foreach (LightInstancerBase instancer in instancers)
                lightInstancers[instancer.LightType()] = instancer;
        }

        void BuildResources()
        {
            if (renderResult != null)
                DisposeSurfaces();

            if (parent != null)
                size = parent.size;

            for (int i = 0; i < 3; i++)
                (layers[i] = new Texture(
                    graphics.dispatcher,
                    new Texture.Parameters(
                        width,
                        height,
                        pixelType: PixelType.Float,
                        minFilter: TextureMinFilter.Linear,
                        pixelFormat: PixelFormat.Rgba,
                        internalFormat: PixelInternalFormat.Rgba32f
                        )
                    )
                ).SetTextureWrap(TextureWrapMode.ClampToEdge);

            for (int i = 3; i < NUM_LAYERS; i++)
                (layers[i] = new Texture(
                    graphics.dispatcher,
                    new Texture.Parameters(
                    width,
                    height,
                    pixelType: PixelType.UnsignedByte,
                    minFilter: TextureMinFilter.Linear,
                    pixelFormat: PixelFormat.Rgba,
                    internalFormat: PixelInternalFormat.Rgba8
                    ))
                ).SetTextureWrap(TextureWrapMode.ClampToEdge);

            (renderResult = new Texture(
                graphics.dispatcher,
                new Texture.Parameters(
                    width, height,
                    pixelType: PixelType.HalfFloat,
                    internalFormat: PixelInternalFormat.Rgba16f,
                    pixelFormat: PixelFormat.Rgba
                    )
                )
            ).SetTextureWrap(TextureWrapMode.ClampToEdge);

            if (parent == null)
                (depthBuffer = new Texture(
                    graphics.dispatcher,
                    new Texture.Parameters(
                        width,
                        height,
                        pixelType: PixelType.Float,
                        pixelFormat: PixelFormat.DepthComponent,
                        internalFormat: PixelInternalFormat.DepthComponent24
                        )
                    )
                ).SetTextureWrap(TextureWrapMode.ClampToEdge);
            else
                depthBuffer = parent.depthBuffer;

            int[] fbos = new int[4];
            GL.GenFramebuffers(4, fbos);
            Graphics.ThrowErrors();
            worldMaterialFramebuffer = new Framebuffer(width, height, null);
            worldFramebuffer = new Framebuffer(width, height, null);
            materialFramebuffer = new Framebuffer(width, height, null);
            resultFramebuffer = new Framebuffer(width, height, null);

            Framebuffer previousFramebuffer = graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, worldMaterialFramebuffer);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthBuffer.textureIndex, 0);
            Graphics.ThrowErrors();
            DrawBuffersEnum[] attachments = new DrawBuffersEnum[NUM_LAYERS];
            for (int i = 0; i < NUM_LAYERS; i++)
            {
                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, layers[i].textureIndex, 0);
                Graphics.ThrowErrors();
                attachments[i] = DrawBuffersEnum.ColorAttachment0 + i;
            }
            GL.DrawBuffers(NUM_LAYERS, attachments);
            Graphics.ThrowErrors();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, worldFramebuffer.frameBufferName);
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthBuffer.textureIndex, 0);
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, layers[0].textureIndex, 0);
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, layers[1].textureIndex, 0);
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, layers[2].textureIndex, 0);
            Graphics.ThrowErrors();
            GL.DrawBuffers(
                3,
                new[] {
                    DrawBuffersEnum.ColorAttachment0,
                    DrawBuffersEnum.ColorAttachment1,
                    DrawBuffersEnum.ColorAttachment2,
                });
            Graphics.ThrowErrors();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, materialFramebuffer.frameBufferName);
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthBuffer.textureIndex, 0);
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment3, layers[3].textureIndex, 0);
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment4, layers[4].textureIndex, 0);
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment5, layers[5].textureIndex, 0);
            Graphics.ThrowErrors();
            GL.DrawBuffers(
                3,
                new[] {
                    DrawBuffersEnum.ColorAttachment3,
                    DrawBuffersEnum.ColorAttachment4,
                    DrawBuffersEnum.ColorAttachment5,
                });
            Graphics.ThrowErrors();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, resultFramebuffer.frameBufferName);
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthBuffer.textureIndex, 0);
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, renderResult.textureIndex, 0);
            Graphics.ThrowErrors();
            GL.DrawBuffers(1, new[] { DrawBuffersEnum.ColorAttachment0 });
            Graphics.ThrowErrors();

            graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, previousFramebuffer);
        }

        internal void SetValues(Shader e)
        {
            e.SetValue("positionSampler", layers[(int)Layers.Position]);
            e.SetValue("normalSampler", layers[(int)Layers.Normal]);
            e.SetValue("surfaceSampler", layers[(int)Layers.Surface]);
            e.SetValue("emissiveSampler", layers[(int)Layers.Emissive]);
            e.SetValue("diffuseSampler", layers[(int)Layers.Diffuse]);
            e.SetValue("specularSampler", layers[(int)Layers.Specular]);
        }

        public void BeginWorld()
            => BeginWorld(true);

        public void BeginWorld(bool clearDepth)
        {
            graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, worldFramebuffer);
            GL.ClearColor(0, 0, 0, 0);
            Graphics.ThrowErrors();
            GL.Viewport(0, 0, renderResult.args.width, renderResult.args.height);
            Graphics.ThrowErrors();
            graphics.stateTracker.SetRenderState<GlStateTracker.Enable, Tuple<EnableCap, bool>>(
                new Tuple<EnableCap, bool>(EnableCap.DepthTest, true)
                );
            GL.Clear((clearDepth ? ClearBufferMask.DepthBufferBit : ClearBufferMask.None) | ClearBufferMask.ColorBufferBit);
            Graphics.ThrowErrors();
        }

        public void BeginMaterial()
        {
            graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, materialFramebuffer);
            GL.ClearColor(0, 0, 0, 0);
            Graphics.ThrowErrors();
            GL.Enable(EnableCap.DepthTest);
            Graphics.ThrowErrors();
            GL.DepthFunc(DepthFunction.Lequal);
            Graphics.ThrowErrors();
            GL.Viewport(0, 0, renderResult.args.width, renderResult.args.height);
            Graphics.ThrowErrors();
            GL.Clear(ClearBufferMask.ColorBufferBit);
            Graphics.ThrowErrors();
        }

        public void Render()
            => Render(false);

        public void Render(Framebuffer renderTarget)
            => Render(renderTarget, false);

        public void Render(bool alphaTested)
            => Render(resultFramebuffer, alphaTested);

        public void Render(Framebuffer renderTarget, bool alphaTest)
        {
            Framebuffer previousBinding = graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, renderTarget);
            RenderInternal(alphaTest);
            graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, previousBinding);
        }

        public void RenderTested(float alphaThreshold = 0.5f)
        {
            shader.SetValue("alphaThreshold", alphaThreshold);
            Render(resultFramebuffer, true);
        }

        void RenderInternal(bool alphaTest = false)
        {
            using (graphics.stateTracker.Start())
            {
                graphics.stateTracker.SetRenderState<GlStateTracker.DepthMask, bool>(false);
                graphics.stateTracker.SetRenderState<GlStateTracker.Enable, Tuple<EnableCap, bool>>(
                    new Tuple<EnableCap, bool>(EnableCap.Blend, false)
                    );

                int[] prevViewport = new int[4];
                GL.GetInteger(GetPName.Viewport, prevViewport);
                Graphics.ThrowErrors();

                SetValues(shader);
                view.SetValues(
                    shader,
                    Matrix.IDENTITY,
                    Matrix.IDENTITY,
                    new Vector4i(prevViewport[0], prevViewport[1], prevViewport[2], prevViewport[3])
                    );

                lights.Update();
                Graphics.ThrowErrors();

                foreach (DeferredShaderIntrinsicLights intrinsic in intrinsics)
                    intrinsic.Clear();

                foreach (LightInstancerBase instancer in lightInstancers.Values)
                    instancer.Reset();

                foreach (Type t in lights.EnumerateTypes())
                {
                    LightInstancerBase instancer;
                    bool hasInstancer = lightInstancers.TryGetValue(t, out instancer);
                    foreach (Light light in lights.EnumerateLights(t))
                    {
                        foreach (DeferredShaderIntrinsicLights i in intrinsics)
                            if (i.AddLight(this, light))
                                goto next;

                        if (hasInstancer)
                            instancer.Add(this, light);
                        next:;
                    }
                }

                foreach (DeferredShaderIntrinsicLights intrinsic in intrinsics)
                    intrinsic.SetValues(shader);

                shader.BeginPass(alphaTest ? "renderTested" : "render");
                Sprite.DrawFullscreen(graphics);
                shader.EndPass();

                graphics.stateTracker.SetRenderState<GlStateTracker.Enable, Tuple<EnableCap, bool>>(
                    new Tuple<EnableCap, bool>(EnableCap.Blend, true)
                    );
                graphics.stateTracker.SetRenderState<
                    GlStateTracker.BlendFuncSeperate,
                    Tuple<BlendingFactorSrc, BlendingFactorDest, BlendingFactorSrc, BlendingFactorDest>
                    >(
                    new Tuple<BlendingFactorSrc, BlendingFactorDest, BlendingFactorSrc, BlendingFactorDest>(
                        BlendingFactorSrc.One,
                        BlendingFactorDest.One,
                        BlendingFactorSrc.Zero,
                        BlendingFactorDest.One
                        ));

                foreach (LightInstancerBase instancer in lightInstancers.Values)
                    instancer.Render(this);
            }
        }

        protected override void DoDispose()
        {
            base.DoDispose();

            if (parent != null)
            {
                parent.RemoveOnDispose(Dispose);
                parent.onSetBounds -= BuildResources;
            }

            DisposeSurfaces();
            foreach (Disposable instancer in lightInstancers.Values)
                instancer.Dispose();
        }

        void DisposeSurfaces()
        {
            renderResult.Dispose();
            foreach (Texture t in layers)
                t.Dispose();

            depthBuffer.Dispose();
            resultFramebuffer.Dispose();
            worldFramebuffer.Dispose();
            materialFramebuffer.Dispose();
            worldMaterialFramebuffer.Dispose();
            lights.Dispose();
        }
    }
}
