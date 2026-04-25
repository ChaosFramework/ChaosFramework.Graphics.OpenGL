using ChaosFramework.Math.Vectors;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.Lights.Intrinsic
{
    public class PointLightIntrinsics : DeferredShaderIntrinsicLights<PointLight>
    {
        static Vector4f PosRange(PointLight l) => new Vector4f(l.position, l.range);
        static Vector4f Color(PointLight l) => l.premultipliedColor.ToVec();
        static Vector4f Ambient(PointLight l) => new Vector4f(
            l.ambientRange == 0f ? 1f : l.ambientRange,
            l.ambientRange == 0f ? 0f : l.ambientFactor,
            0,
            0
            );

        public PointLightIntrinsics(ushort maxLights)
            : base(maxLights)
        { }

        protected override string GetShaderKey()
            => "ChaosGraphics.PointLight";

        protected override string GetShadeMethodName()
            => "shade";

        protected override SysCol.IEnumerable<ShadeInput> CreateShadeInputs()
        {
            yield return new ShadeInput("vec3", "WORLDPOS");
            yield return new ShadeInput("vec3", "NORMAL");
            yield return new ShadeInput("vec4", "DIFFUSE");
            yield return new ShadeInput("vec4", "SPECULAR");
            yield return new ShadeInstanceInput<Vector4f>("vec4", "POSITION_RANGE", maxLights, PosRange, new Vector4f(0, 0, 0, 1));
            yield return new ShadeInstanceInput<Vector4f>("vec4", "LIGHTCOLOR", maxLights, Color, 0);
            yield return new ShadeInstanceInput<Vector4f>("vec4", "AMBIENT", maxLights, Ambient, new Vector4f(1, 0, 0, 0));
        }
    }
}
