using ChaosUtil.Primitives;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    public class ShaderParam : System.IComparable<ShaderParam>
    {
        public bool isIn { get; internal set; } = false;
        public bool isOut { get; internal set; } = false;
        public string name { get; internal set; }
        public string type { get; internal set; }
        public string semantic { get; internal set; }

        public override bool Equals(object other)
            => Equals(other as ShaderParam);

        public bool Equals(ShaderParam other)
            => other != null && other.type == type && semantic == other.semantic;

        public override int GetHashCode()
            => HashCode.Combine(type, semantic);

        int System.IComparable<ShaderParam>.CompareTo(ShaderParam other)
            => semantic.CompareTo(other.semantic);
    }
}
