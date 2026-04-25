using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Collections;
using ChaosFramework.Components;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.Particles
{
    public abstract class ParticleSystem
        : Component
    {
        public class Params
            : CParams<ShaderContainer.Entry, TextureContainer.Entry, Vector2i, Camera, int, int, string[], bool>
        {
            public ShaderContainer.Entry shader => v1;
            public TextureContainer.Entry maskTexture => v2;
            public Vector2i numParticlesInTexture => v3;
            public Camera view => v4;
            public int expectedInstances => v5;
            public int updateLayer => v6;
            public string[] instanceSemantics => v7;
            public bool useSharedBuffer => v8;

            public Params(
                ShaderContainer.Entry shader,
                TextureContainer.Entry maskTexture,
                Vector2i numParticlesInTexture,
                Camera view,
                int expectedInstances,
                int updateLayer,
                string[] instanceSemantics,
                bool useSharedBuffer = true
                ) : base(
                    shader,
                    maskTexture,
                    numParticlesInTexture,
                    view,
                    expectedInstances,
                    updateLayer,
                    instanceSemantics,
                    useSharedBuffer
                    )
            { }
        }


        public Camera view { get; private set; }
        public ShaderContainer.Entry shader { get; private set; }
        public TextureContainer.Entry maskTexture { get; private set; }
        public Graphics graphics => shader.content.graphics;

        protected MatrixInstancer instancer { get; private set; }
        protected Vector2i numParticlesInTexture { get; private set; }

        AdvancedLinkedList<Particle> _particles = new AdvancedLinkedList<Particle>();
        public SysCol.ICollection<Particle> particles => _particles;

        int updateLayer;

        protected override void Create(CreateParameters cparams)
        {
            if (cparams is not Params args)
                throw new System.ArgumentException($"expected {typeof(Params).FullName}", nameof(cparams));

            shader = args.shader;
            maskTexture = args.maskTexture;
            numParticlesInTexture = args.numParticlesInTexture;
            view = args.view;
            updateLayer = args.updateLayer;
            instancer = new MatrixInstancer(
                args.shader.content.graphics,
                args.instanceSemantics,
                args.expectedInstances,
                args.useSharedBuffer
                );
        }

        public override void SetUpdateCalls()
        {
            base.SetUpdateCalls();
            scene.updateLayers[updateLayer].Add(Update);
        }

        public void Teleport(Vector3f offset)
        {
            foreach (Particle p in _particles)
                p.Teleport(offset);
        }

        public virtual void PrepareVertices()
        {
            if (shader == null)
                throw new System.InvalidOperationException("Particle System was not initialized.");

            instancer.Reset();
            foreach (Particle p in _particles)
                p.SetInstanceData(instancer, view);

            instancer.UpdateBuffer();
        }

        protected void Draw(string pass)
        {
            if (instancer.numInstances == 0)
                return;

            view.SetValues(shader, Matrix.IDENTITY);
            shader.SetValue("texAtlasSize", 1.0f / numParticlesInTexture);
            Sprite.DrawPositionInstanced(graphics, shader, instancer, pass);
        }

        void Update()
        {
            foreach (Particle p in _particles)
                if (!p.Update())
                    _particles.RemoveCurrent();
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            _particles.Clear();
            instancer.Dispose();
        }
    }
}
