using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public class DirectionalLight : Light
    {
        public Rgba ambient;

        private readonly DeferredShader shader;

        public Vector4f premultipliedAmbientVec => new Vector4f(ambient.a * ambient.rgb.ToVec(), 1);

        private Vector3f _direction;
        public Vector3f direction
        {
            get { return _direction; }
            set { _direction = Vector3f.Normalize(value); }
        }

        public DirectionalLight(Vector3f direction, Rgba color)
        {
            this.direction = direction;
            this.color = color;
        }

        public DirectionalLight(DeferredShader shader, Vector3f direction, Rgba color)
        {
            this.shader = shader;
            this.direction = direction;
            this.color = color;
        }

        public override bool CheckVisible(Camera view) => true;

        public override float EstimatedScreenCoverage(Camera view) => 1;
    }
}
