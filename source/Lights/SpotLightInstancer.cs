using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public class SpotLightInstancer : LightInstancer<SpotLight>
    {
        static readonly string[] registers = new[] { "POSITION_RANGE", "LIGHT_COLOR", "DIRECTION_FALLOFF", "ANGLE" };

        readonly Graphics graphics;
        readonly MatrixInstancer informer, backFacingInformer;
        readonly MeshContainer.Entry mesh;
        readonly ShaderContainer.Entry shader;

        public SpotLightInstancer(Graphics graphics, int expectedInstances)
        {
            this.graphics = graphics;

            informer = new MatrixInstancer(graphics, registers, expectedInstances, false);
            backFacingInformer = new MatrixInstancer(graphics, registers, expectedInstances, false);
            shader = graphics.shaders.Load($"ChaosGraphics.{nameof(SpotLight)}", informer);
            mesh = graphics.meshes.Load($"${nameof(SpotLight)}", informer);
        }

        public override void Reset()
        {
            informer.Reset();
            backFacingInformer.Reset();
        }

        public bool Frontfacing(SpotLight l, Camera camera)
            => Shapes.Intersection.ConvexHull.CheckIntersection(
                new Shapes.Convex.SphereShape(camera.Position, camera.nearClip),
                l.shape
                ) == null;

        protected override void Add(DeferredShader target, SpotLight l)
        {
            (Frontfacing(l, target.view) ? informer : backFacingInformer)
                .AddInstance(
                    l.transform,
                    new Vector4f(l.position, l.range),
                    l.premultipliedColor.ToVec(),
                    new Vector4f(l.direction, l.falloff),
                    l.angle
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
