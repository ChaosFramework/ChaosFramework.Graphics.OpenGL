using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Core;
using ChaosFramework.Math;
using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl.PostProcessors
{
    public class TemporalAntiAliasing : Disposable
    {
        public float blendFactor = 0.9f;
        public float historyClampFactor = 0.75f;

        readonly DeferredShader shader;
        readonly Framebuffer[] fbos = new Framebuffer[2];
        readonly Texture[] tex = new Texture[2];
        readonly float[] weights = new float[9];

        int newTarget, oldTarget;
        Matrix oldViewProj;

        public Texture resultTexture => tex[newTarget];

        public TemporalAntiAliasing(DeferredShader shader)
        {
            this.shader = shader;
            shader.onSetBounds += BuildBuffers;
            BuildBuffers();

            float totalWeight = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                int x = i % 3 - 1;
                int y = i / 3 - 1;
                float v = (float)System.Math.Exp(-2.29 * ((x * x) + (y * y)));
                totalWeight += v;
                weights[i] = v;
            }
            for (int i = 0; i < weights.Length; i++)
                weights[i] /= totalWeight;
        }

        void BuildBuffers()
        {
            DisposeBuffers();
            for (int i = 0; i < 2; i++)
            {
                tex[i] = new Texture(
                    shader.graphics.dispatcher,
                    new Texture.Parameters(
                        shader.width,
                        shader.height,
                        pixelType: PixelType.Float,
                        pixelFormat: PixelFormat.Rgba,
                        internalFormat: PixelInternalFormat.Rgb16f
                        )
                    );
                fbos[i] = new Framebuffer(shader.resultFramebuffer.viewport, new[] { tex[i] });
            }
        }

        public void Apply()
        {
            oldTarget = newTarget;
            newTarget = (newTarget + 1) % 2;
            shader.graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, fbos[newTarget]);
            Shader s = shader.graphics.shaders.taa;
            s.SetValue("weights", weights);
            s.SetValue("blendFactor", blendFactor);
            s.SetValue("historyClampFactor", historyClampFactor);
            s.SetValue("historySampler", tex[oldTarget]);
            s.SetValue("renderResultSampler", shader.renderResult);
            s.SetValue("reProjection", oldViewProj);
            s.SetValue("positionSampler", shader.layers[(int)DeferredShader.Layers.Position]);
            s.BeginPass("TAA");
            Sprite.DrawFullscreen(shader.graphics);
            s.EndPass();
            oldViewProj = shader.view.ViewProjection;
        }

        void DisposeBuffers()
        {
            for (int i = 0; i < 2; i++)
            {
                tex[i]?.Dispose();
                fbos[i]?.Dispose();
            }
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            shader.onSetBounds -= BuildBuffers;
            DisposeBuffers();
        }
    }
}
