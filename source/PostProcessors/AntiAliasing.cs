using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Graphics.OpenGl.PostProcessors
{
    public class AntiAliasing
    {
        public float normalFactor = 1.5f;
        public float positionFactor = 0.1f;

        public void Apply(DeferredShader shader)
        {
            Shader s = shader.graphics.shaders.fxaa;
            s.SetValue("pixelOffset", new Vector4f(1f / shader.width, 1f / shader.height, positionFactor, normalFactor));
            s.SetValue("renderResultSampler", shader.renderResult);
            s.SetValue("normalSampler", shader.layers[(int)DeferredShader.Layers.Normal]);
            s.SetValue("positionSampler", shader.layers[(int)DeferredShader.Layers.Position]);
            s.BeginPass("FXAA");
            Sprite.DrawFullscreen(shader.graphics);
            s.EndPass();
        }

        public void ApplyRemap(DeferredShader shader, Vector4f remapRect)
        {
            Shader s = shader.graphics.shaders.fxaa;
            s.SetValue("pixelOffset", new Vector4f(1f / shader.width, 1f / shader.height, positionFactor, normalFactor));
            s.SetValue("renderResultSampler", shader.renderResult);
            s.SetValue("normalSampler", shader.layers[(int)DeferredShader.Layers.Normal]);
            s.SetValue("positionSampler", shader.layers[(int)DeferredShader.Layers.Position]);
            s.SetValue("remapRect", remapRect);
            s.BeginPass("FXAA_Remap");
            Sprite.DrawFullscreen(shader.graphics);
            s.EndPass();
        }
    }
}
