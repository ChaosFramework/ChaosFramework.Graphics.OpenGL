using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Shapes.Convex;
using static ChaosFramework.Math.Constants;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public class SpotLight : Light
    {
        public Vector3f up = new Vector3f(0, 1, 0);
        public Vector3f position = new Vector3f(0, -1, 4);
        public TextureContainer.Entry mask;

        public Matrix transform { get; private set; }

        Vector3f _direction = new Vector3f(0, 0, 1);
        public Vector3f direction
        {
            get { return _direction; }
            set { _direction = Vector3f.Normalize(value); }
        }

        public float range = 40;
        public float angle = PI_HALF / 2;
        public float falloff = 0;

        internal Shape shape;

        public SpotLight(
            Vector3f position,
            Rgba color,
            float range,
            float angle,
            float falloff
            )
        {
            this.position = position;
            base.color = color;
            this.range = range;
            this.angle = angle;
            this.falloff = falloff;

            shape = new SphereShape(Vector3f.EMPTY, 1.0f); // TODO: make this a proper cone shape
        }

        public override void Update()
        {
            float tan = (float)System.Math.Tan(angle);
            Matrix orientation = Matrix.IDENTITY;
            Vector3f right = Vector3f.Normalize(Vector3f.Cross(up, direction));
            Vector3f n_direction = Vector3f.Normalize(direction);
            Vector3f n_up = Vector3f.Cross(n_direction, right);
            orientation.m00 = right.x; orientation.m01 = right.y; orientation.m02 = right.z;
            orientation.m10 = n_up.x; orientation.m11 = n_up.y; orientation.m12 = n_up.z;
            orientation.m20 = n_direction.x; orientation.m21 = n_direction.y; orientation.m22 = n_direction.z;
            transform = Matrix.Scaling(tan * range, tan * range, range) * orientation * Matrix.Translation(position);
            shape.Update(transform);
        }

        public override bool CheckVisible(Camera view)
            => Shapes.Intersection.ConvexHull.CheckIntersection(shape, view.viewFrustum) != null;

        public override float EstimatedScreenCoverage(Camera view) => 0.5f;
    }
}
