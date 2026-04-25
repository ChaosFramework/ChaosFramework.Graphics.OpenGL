using ChaosFramework.Math.Vectors;
using ChaosFramework.Math;
using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    using AssetContainers;

    public class SegmentLightInstancer : LightInstancer<SegmentLight>
    {
        static readonly string[] INSTANCE_SEMANTICS = new[] {
            "POSITION_RANGE_1",
            "LIGHT_COLOR_1",
            "POSITION_RANGE_2",
            "LIGHT_COLOR_2"
        };

        readonly Instancing.CustomInstancer informer;
        readonly ShaderContainer.Entry shader;

        public SegmentLightInstancer(Graphics graphics, int expectedInstances)
        {
            informer = new Instancing.CustomInstancer(graphics, INSTANCE_SEMANTICS, expectedInstances, true);
            shader = graphics.shaders.Load($"ChaosGraphics.{nameof(SegmentLight)}", this);
        }

        public override void Render(DeferredShader target)
        {
            if (informer.numInstances == 0)
                return;

            informer.UpdateBuffer();
            target.SetValues(shader.content);
            target.view.SetValues(shader.content, Matrix.IDENTITY);
            informer.Bind(shader.content.BeginPass("SegmentLight"));
            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, informer.numInstances);
            shader.content.EndPass();
        }

        public override void Reset()
        {
            informer.Reset();
        }

        protected override void Add(DeferredShader target, SegmentLight l)
        {
            informer.AddInstance(
                new Vector4f(l.position, l.range),
                l.color.ToVec(),
                new Vector4f(l.position2, l.range2),
                l.color2.ToVec()
                );
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            informer.Dispose();
        }
    }
}
