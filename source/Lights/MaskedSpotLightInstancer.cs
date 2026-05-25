using ChaosFramework.Core;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public class MaskedSpotLightInstancer : SpotLightInstancer<MaskedSpotLight>
    {
        readonly TextureContainer.Entry mask;

        public MaskedSpotLightInstancer(Graphics graphics, int expectedInstances, TextureContainer.Entry mask)
            : base(graphics, expectedInstances, SPOTLIGHT_REGISTERS)
        {
            this.mask = mask;
        }

        protected override MeshContainer.Entry GetMesh(Disposable monitor)
            => graphics.meshes.Load($"${nameof(CircularSpotLight)}", monitor);

        public override void Render(DeferredShader target)
        {
            shader.SetValue("mask", mask);
            base.Render(target);
        }
    }
}
