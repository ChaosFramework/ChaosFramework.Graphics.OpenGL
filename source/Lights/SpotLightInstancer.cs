using ChaosFramework.Core;
using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public abstract class SpotLightInstancer<SpecializedSpotLight> : LightInstancer<SpecializedSpotLight>
        where SpecializedSpotLight : SpotLight
    {
        protected static readonly string[] SPOTLIGHT_REGISTERS = new[] { "POSITION_RANGE", "LIGHT_COLOR", "DIRECTION_ANGLE" };

        protected readonly Graphics graphics;
        readonly MatrixInstancer informer, backFacingInformer;
        readonly MeshContainer.Entry mesh;
        protected readonly ShaderContainer.Entry shader;

        public SpotLightInstancer(Graphics graphics, int expectedInstances, string[] registers)
        {
            this.graphics = graphics;

            informer = new MatrixInstancer(graphics, registers, expectedInstances, false);
            backFacingInformer = new MatrixInstancer(graphics, registers, expectedInstances, false);
            shader = GetShader(informer);
            mesh = GetMesh(informer);
        }

        protected abstract MeshContainer.Entry GetMesh(Disposable monitor);

        protected virtual ShaderContainer.Entry GetShader(Disposable monitor)
            => graphics.shaders.Load($"ChaosGraphics.{typeof(SpecializedSpotLight).Name}", informer);

        public override void Reset()
        {
            informer.Reset();
            backFacingInformer.Reset();
        }

        public bool Frontfacing(SpecializedSpotLight l, Camera camera)
            => Shapes.Intersection.ConvexHull.CheckIntersection(
                new Shapes.Convex.SphereShape(camera.Position, camera.nearClip),
                l.shape
                ) == null;

        protected override bool Add(DeferredShader target, SpecializedSpotLight l)
        {
            (Frontfacing(l, target.view) ? informer : backFacingInformer).AddInstance(l.transform, GetInstanceData(l));

            return true;
        }

        protected virtual Vector4f[] GetInstanceData(SpecializedSpotLight l)
            => new [] {
                new Vector4f(l.position, l.range),
                l.premultipliedColor.ToVec(),
                new Vector4f(l.direction, l.angle)
                };

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
