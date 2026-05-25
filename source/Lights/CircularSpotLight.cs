using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public class CircularSpotLight : SpotLight
    {
        public float falloff = 0;

        public CircularSpotLight(
            Vector3f position,
            Rgba color,
            float range,
            float angle,
            float falloff
            ) : base(position, color, range, angle)
        {
            this.falloff = falloff;
        }
    }
}
