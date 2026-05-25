using ChaosFramework.Math.Vectors;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.Lights.Intrinsic
{
    public class DirectionalLightIntrinsics
        : DeferredShaderIntrinsicLights<DirectionalLight>
    {
        static Vector3f Direction(DirectionalLight l) => l.direction;
        static Vector4f Color(DirectionalLight l) => l.premultipliedColor.ToVec();
        static Vector4f Ambient(DirectionalLight l) => l.premultipliedAmbientVec;

        public DirectionalLightIntrinsics(ushort maxLights)
            : base(maxLights)
        { }

        protected override string GetShaderKey()
            => "ChaosGraphics.DirectionalLight";

        protected override string GetShadeMethodName()
            => "shade";

        protected override SysCol.IEnumerable<ShadeInput> CreateShadeInputs()
        {
            yield return new ShadeInput("vec3", "WORLDPOS");
            yield return new ShadeInput("vec3", "NORMAL");
            yield return new ShadeInput("vec4", "DIFFUSE");
            yield return new ShadeInput("vec4", "SPECULAR");
            yield return new ShadeInstanceInput<Vector3f>("vec3", "DIRECTION", maxLights, Direction, new Vector3f(0, 0, 1));
            yield return new ShadeInstanceInput<Vector4f>("vec4", "LIGHTCOLOR", maxLights, Color, 0);
            yield return new ShadeInstanceInput<Vector4f>("vec4", "AMBIENTCOLOR", maxLights, Ambient, 0);
        }
    }
}
