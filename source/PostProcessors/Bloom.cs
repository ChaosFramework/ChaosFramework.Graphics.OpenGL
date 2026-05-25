namespace ChaosFramework.Graphics.OpenGl.PostProcessors
{
    public class Bloom : Blur
    {
        public float minimumGlowValue = 1f;

        public Bloom(Graphics graphics, int numSamples)
            : base(graphics, numSamples, Shaders.code, "ChaosGraphics.Bloom")
        { }

        public override void Render(Texture source, Framebuffer tmpBuffer, float radius)
        {
            shader.SetValue("originalSampler", source);
            shader.SetValue("minimumGlowValue", minimumGlowValue);
            base.Render(source, tmpBuffer, radius);
        }
    }
}
