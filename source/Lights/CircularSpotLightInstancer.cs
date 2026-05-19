using ChaosFramework.Core;
using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using System.Linq;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public class CircularSpotLightInstancer : SpotLightInstancer<CircularSpotLight>
    {
        static readonly string[] REGISTERS = SPOTLIGHT_REGISTERS.Concat(new[] { "FALLOFF" }).ToArray();

        public CircularSpotLightInstancer(Graphics graphics, int expectedInstances)
            : base(graphics, expectedInstances, REGISTERS)
        { }

        protected override Vector4f[] GetInstanceData(CircularSpotLight l)
            => new [] {
                new Vector4f(l.position, l.range),
                l.premultipliedColor.ToVec(),
                new Vector4f(l.direction, l.angle),
                l.falloff
                };

        protected override MeshContainer.Entry GetMesh(Disposable monitor)
            => graphics.meshes.Load($"${nameof(CircularSpotLight)}", monitor);
    }
}
