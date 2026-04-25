using ChaosFramework.Core;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl.PostProcessors
{
    using AssetContainers;

    public class Blur : Disposable
    {
        protected static float[] GetWeights(int numSamples, float radius)
        {
            float c = 0.5f;
            float[] values = new float[numSamples];
            float sum = 0;
            for (int i = 0; i < numSamples; i++)
            {
                float f = -1 + 2 * ((float)i / (numSamples - 1));
                sum += (values[i] = (float)System.Math.Pow(System.Math.E, -(f * f / (2 * c * c))));
            }
            for (int i = 0; i < numSamples; i++)
                values[i] /= sum;

            return values;
        }

        public readonly int numSamples;

        protected readonly Graphics graphics;
        protected readonly Shader shader;

        public Blur(Graphics graphics, int numSamples)
            : this(graphics, numSamples, Shaders.code, "ChaosGraphics.Blur")
        { }

        protected Blur(Graphics graphics, int numSamples, ShaderCodeContainer baseCodeContainer, string baseCodeKey)
        {
            this.graphics = graphics;
            this.numSamples = numSamples;

            ShaderCodeContainer.Entry baseCode = baseCodeContainer.Load(baseCodeKey, this);
            baseCode.content.AddDefine(new Define("NUM_SAMPLES", numSamples.ToString()));
            shader = new Shader(graphics, baseCode, graphics.shaders.shaders.defaultParameter);
            shader.Compile();
        }

        public virtual void Render(Texture source, Framebuffer tmpBuffer, float radius)
        {
            GL.BindTexture(TextureTarget.Texture2D, source.textureIndex);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Graphics.ThrowErrors();

            GL.BindTexture(TextureTarget.Texture2D, tmpBuffer.textures[0].textureIndex);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Graphics.ThrowErrors();

            GL.BindTexture(TextureTarget.Texture2D, 0);
            Graphics.ThrowErrors();

            float[] weights = GetWeights(numSamples, radius);

            Framebuffer previousBuffer = graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, tmpBuffer);
            int[] prevViewport = new int[4];
            GL.GetInteger(GetPName.Viewport, prevViewport);
            Graphics.ThrowErrors();
            GL.Viewport(0, 0, tmpBuffer.textures[0].args.width, tmpBuffer.textures[0].args.height);
            Graphics.ThrowErrors();

            shader.SetValue("blurRange", radius);
            shader.SetValue("sampleWeight", weights);
            shader.SetValue("srcSampler", source);

            shader.BeginPass("Horizontal");
            Sprite.DrawFullscreen(graphics);
            shader.EndPass();
            shader.SetValue("srcSampler", tmpBuffer.textures[0]);
            graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, previousBuffer);
            GL.Viewport(prevViewport[0], prevViewport[1], prevViewport[2], prevViewport[3]);
            Graphics.ThrowErrors();
            shader.CommitChanges();

            shader.BeginPass("Vertical");
            Sprite.DrawFullscreen(graphics);
            shader.EndPass();
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            shader.Dispose();
        }
    }
}
