using ChaosFramework.Math.Vectors;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.Lights.Intrinsic
{
    public class SegmentLightIntrinsics : DeferredShaderIntrinsicLights<SegmentLight>
    {
        static Vector4f PosRange1(SegmentLight l) => new Vector4f(l.position, l.range);
        static Vector4f PosRange2(SegmentLight l) => new Vector4f(l.position2, l.range2);
        static Vector4f Color1(SegmentLight l) => l.color.ToVec();
        static Vector4f Color2(SegmentLight l) => l.color2.ToVec();

        public SegmentLightIntrinsics(ushort maxLights)
            : base(maxLights)
        { }

        protected override string GetShaderKey()
            => "ChaosGraphics.SegmentLight";

        protected override string GetShadeMethodName()
            => "shade";

        protected override SysCol.IEnumerable<ShadeInput> CreateShadeInputs()
        {
            yield return new ShadeInput("vec3", "WORLDPOS");
            yield return new ShadeInput("vec3", "NORMAL");
            yield return new ShadeInput("vec4", "DIFFUSE");
            yield return new ShadeInput("vec4", "SPECULAR");
            yield return new ShadeInstanceInput<Vector4f>("vec4", "POSITION_RANGE_1", maxLights, PosRange1, new Vector4f(0, 0, 0, 1));
            yield return new ShadeInstanceInput<Vector4f>("vec4", "LIGHT_COLOR_1", maxLights, Color1, 0);
            yield return new ShadeInstanceInput<Vector4f>("vec4", "POSITION_RANGE_2", maxLights, PosRange2, new Vector4f(0, 0, 0, 1));
            yield return new ShadeInstanceInput<Vector4f>("vec4", "LIGHT_COLOR_2", maxLights, Color2, 0);
        }
    }
}
