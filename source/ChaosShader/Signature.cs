using ChaosUtil.Primitives;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    public abstract class Signature : ShaderComponent
    {
        public string type { get; internal set; }
        public string name { get; internal set; }

        public Signature(string type, string name)
        {
            this.type = type;
            this.name = name;
        }

        public override bool Equals(object other)
            => Equals(other as Signature);

        public bool Equals(Signature other)
            => other != null && other.type == type && other.name == name;

        public override int GetHashCode()
            => HashCode.Combine(type, name);

        public override string ToString() => $"{GetType()}{{ {type}; {name} }}";
    }
}
