using ChaosFramework.Math.Vectors;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.Lights.Intrinsic
{
    public abstract class SpotLightIntrinsics<Light> : DeferredShaderIntrinsicLights<Light>
        where Light : SpotLight
    {
        static Vector4f PosRange(SpotLight l) => new Vector4f(l.position, l.range);
        static Vector4f Color(SpotLight l) => l.premultipliedColor.ToVec();
        static Vector4f Angle(SpotLight l) => l.angle;

        public SpotLightIntrinsics(ushort maxLights)
            : base(maxLights)
        { }

        protected abstract float ExtraData(Light l);
        Vector4f DirectionExtraData(Light l) => new Vector4f(l.direction, ExtraData(l));

        protected override string GetShaderKey()
            => $"ChaosGraphics.{typeof(Light).Name}";

        protected override string GetShadeMethodName()
            => "shade";

        protected override SysCol.IEnumerable<ShadeInput> CreateShadeInputs()
        {
            yield return new ShadeInput("vec3", "WORLDPOS");
            yield return new ShadeInput("vec3", "NORMAL");
            yield return new ShadeInput("vec4", "DIFFUSE");
            yield return new ShadeInput("vec4", "SPECULAR");
            yield return new ShadeInstanceInput<Vector4f>("vec4", "POSITION_RANGE", maxLights, PosRange, new Vector4f(0, 0, 0, 1));
            yield return new ShadeInstanceInput<Vector4f>("vec4", "LIGHT_COLOR", maxLights, Color, 0);
            yield return new ShadeInstanceInput<Vector4f>("vec4", "DIRECTION_FALLOFF", maxLights, DirectionExtraData, new Vector4f(1, 0, 0, 0));
            yield return new ShadeInstanceInput<Vector4f>("vec4", "ANGLE", maxLights, Angle, 0.0f);
        }
    }
}
