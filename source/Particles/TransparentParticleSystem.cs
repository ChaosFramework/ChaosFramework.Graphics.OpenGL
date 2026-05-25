using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math;

namespace ChaosFramework.Graphics.OpenGl.Particles
{
    public abstract class TransparentParticleSystem
        : ParticleSystem
        , Transparent
    {
        public virtual void DrawMask(TransparencyRenderer renderer)
        {
            if (instancer.numInstances == 0)
                return;

            view.SetValues(renderer.maskEffect, Matrix.IDENTITY);
            renderer.maskEffect.SetValue("tex", maskTexture);
            renderer.maskEffect.SetValue("texAtlasSize", 1.0f / numParticlesInTexture);
            Sprite.DrawPositionInstanced(
                renderer.graphics,
                renderer.maskEffect,
                instancer,
                renderer.hasSolidWorld ? "ParticleInstanced" : "ParticleInstancedNoWorld"
                );
        }

        public abstract void DrawTransparent(TransparencyRenderer renderer);
    }
}
