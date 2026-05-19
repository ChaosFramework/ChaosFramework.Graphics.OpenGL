using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    using AssetContainers;

    public class MaskedSpotLight : SpotLight
    {
        public TextureContainer.Entry mask;

        public MaskedSpotLight(
            Vector3f position,
            Rgba color,
            float range,
            float angle,
            TextureContainer.Entry mask
            ) : base(position, color, range, angle)
        {
            this.mask = mask;
        }
    }
}
