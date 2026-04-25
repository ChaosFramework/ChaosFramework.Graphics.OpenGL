using ChaosFramework.Shapes.Convex;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public class SegmentLight : Light
    {
        Matrix transform;
        public Rgba color2;
        public float range, range2;
        public Vector3f position, position2;

        protected Shape shape;

        protected internal Rgba premultipliedColor2
            => new Rgba(color2.rgb * color2.a, color2.a);

        public SegmentLight(
            Vector3f position,
            float range,
            Rgba color,
            Vector3f position2,
            float range2,
            Rgba color2
            )
        {
            this.position = position; this.position2 = position2;
            this.range = range; this.range2 = range2;
            this.color = color; this.color2 = color2;
            shape = new CapsuleShape(0, new Vector3f(0, 1, 0), 1);
        }

        public override void Update()
        {
            // TODO: Stop working with matrices when we have actual data
            float radius = Max(range, range2);

            Vector3f d = Vector3f.Normalize(position2 - position);
            Vector3f pos1 = position - range * d, pos2 = position2 + range2 * d;
            d = pos2 - pos1;
            float len = d.Length();
            d *= (1 / len);
            Vector3f localX, localY = d, localZ;
            if (d == new Vector3f(0, 0, 1))
            {
                localX = new Vector3f(1, 0, 0);
                localZ = new Vector3f(0, -1, 0);
            }
            else
            {
                localX = Vector3f.Normalize(Vector3f.Cross(localY, new Vector3f(0, 0, 1)));
                localZ = Vector3f.Cross(localX, localY);
            }

            Matrix m = Matrix.IDENTITY;
            m.m00 = localX.x; m.m01 = localX.y; m.m02 = localX.z;
            m.m10 = localY.x; m.m11 = localY.y; m.m12 = localY.z;
            m.m20 = localZ.x; m.m21 = localZ.y; m.m22 = localZ.z;
            transform = Matrix.Scaling(radius, len, radius) * m * Matrix.Translation(pos1);
            shape.Update(transform);
            base.Update();
        }

        public override bool CheckVisible(Camera view)
            => Shapes.Intersection.ConvexHull.CheckIntersection(shape, view.viewFrustum) != null;

        //TODO: calculate a sensible value here?!
        public override float EstimatedScreenCoverage(Camera view) => 9;
    }
}
