using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public class PointLightInstancer : LightInstancer<PointLight>
    {
        static readonly string[] registers = new[] { "INSTANCE_POSITION_RANGE", "INSTANCE_COLOR", "INSTANCE_AMBIENT" };

        readonly Graphics graphics;
        readonly MatrixInstancer informer, backFacingInformer;
        readonly MeshContainer.Entry mesh;
        readonly ShaderContainer.Entry shader;

        public PointLightInstancer(Graphics graphics, int expectedInstances)
        {
            this.graphics = graphics;

            informer = new MatrixInstancer(graphics, registers, expectedInstances, false);
            backFacingInformer = new MatrixInstancer(graphics, registers, expectedInstances, false);
            shader = graphics.shaders.Load($"ChaosGraphics.{nameof(PointLight)}", informer);
            mesh = graphics.meshes.Load($"${nameof(PointLight)}", informer);
        }

        public override void Reset()
        {
            informer.Reset();
            backFacingInformer.Reset();
        }

        public bool Frontfacing(PointLight l, Camera camera)
        {
            float dRange = l.range * mesh.content.data.hullRadius + camera.nearClip;
            return (camera.Position - l.position).LengthSq() > (dRange * dRange);
        }

        protected override void Add(DeferredShader target, PointLight l)
        {
            (Frontfacing(l, target.view) ? informer : backFacingInformer)
                .AddInstance(
                    l.transform,
                    new Vector4f(l.position, l.range),
                    l.premultipliedColor.ToVec(),
                    new Vector4f(
                        l.ambientRange == 0f ? 1f : l.ambientRange,
                        l.ambientRange == 0f ? 0f : l.ambientFactor,
                        0,
                        0)
                    );
        }

        public override void Render(DeferredShader target)
        {
            if (informer.numInstances + backFacingInformer.numInstances == 0)
                return;

            target.SetValues(shader);
            target.view.SetValues(shader, Matrix.IDENTITY, Matrix.IDENTITY);

            if (informer.numInstances > 0)
            {
                informer.UpdateBuffer();
                mesh.content.DrawInstanced(shader, "Instanced", informer);
            }

            if (backFacingInformer.numInstances > 0)
            {
                backFacingInformer.UpdateBuffer();
                mesh.content.DrawInstanced(shader, "InstancedBackface", backFacingInformer);
            }
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            informer?.Dispose();
            backFacingInformer?.Dispose();
        }
    }
}
