using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Shapes.Convex;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public class PointLight : Light
    {
        public Vector3f position;
        public float range;
        public float ambientRange;
        public float ambientFactor;

        internal Matrix transform;

        protected Shape shape;

        public PointLight(
            Vector3f position,
            float range,
            Rgba color,
            float ambientRange,
            float ambientFactor
            ) : this(position, range, color)
        {
            this.ambientRange = ambientRange;
            this.ambientFactor = ambientFactor;
        }

        public PointLight(Vector3f position, float range, Rgba color)
        {
            this.position = position;
            this.color = color;
            this.range = range;
            shape = new SphereShape(Vector3f.EMPTY, 1);
        }

        public override void Update()
        {
            transform = Matrix.Scaling(range, range, range) * Matrix.Translation(position);
            shape.Update(transform);
        }

        public override bool CheckVisible(Camera view)
            => Shapes.Intersection.ConvexHull.CheckIntersection(shape, view.viewFrustum) != null;

        public override float EstimatedScreenCoverage(Camera view) => range;
    }
}
