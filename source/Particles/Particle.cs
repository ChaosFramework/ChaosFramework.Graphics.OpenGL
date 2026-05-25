using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Components;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Graphics.OpenGl.Particles
{
    public abstract class Particle
    {
        public ParticleSystem parent;
        public Vector3f position;
        public float size = 1;
        protected Vector2i particleIndex = new Vector2i(0, 0);

        public Time ftime;
        public Particle(Time ftime) { this.ftime = ftime; }
        public virtual void Teleport(Vector3f offset) { position += offset; }
        public virtual void SetInstanceData(MatrixInstancer instancer, Camera view)
            => instancer.AddInstance(GetTransform(view), GetInstanceInformation(), GetColor().ToVec());

        protected virtual Matrix GetTransform(Camera view)
            => Matrix.Scaling(size) * view.billBoard * Matrix.Translation(position);

        protected virtual Rgba GetColor() => Rgba.OPAQUE_WHITE;
        protected virtual Vector4f GetInstanceInformation() => new Vector4f(particleIndex.x, particleIndex.y, 0, 0);
        public abstract bool Update();
    }
}
