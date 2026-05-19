using ChaosFramework.Math.Vectors;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.Lights.Intrinsic
{
    public class CircularSpotLightIntrinsics : SpotLightIntrinsics<CircularSpotLight>
    {
        public CircularSpotLightIntrinsics(ushort maxLights)
            : base(maxLights)
        { }

        protected override float ExtraData(CircularSpotLight l)
            => l.falloff;
    }
}
