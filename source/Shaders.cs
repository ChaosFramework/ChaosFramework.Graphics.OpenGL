using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Core;

namespace ChaosFramework.Graphics.OpenGl
{
    public class Shaders : Disposable
    {
        internal static readonly ShaderCodeContainer code = new ShaderCodeContainer(StreamSources.shaderCode);

        public readonly ShaderContainer.Entry text,
                                              managedText,
                                              normalMap,
                                              shadow,
                                              skinnedNormalMap,
                                              instancedNormalMap,
                                              skinnedInstanceNormalMap,
                                              coloredNormalMap,
                                              spriteEffect,
                                              fxaa,
                                              taa,
                                              particleMask;

        public readonly Graphics graphics;

        internal readonly ShaderContainer shaders;

        internal Shaders(Graphics graphics, ref Shaders s)
        {
            this.graphics = graphics;
            s = this;
            shaders = new ShaderContainer(StreamSources.shaders, graphics, code);

            spriteEffect = shaders.Load("ChaosGraphics.Sprite", this);
            normalMap = shaders.Load("ChaosGraphics.NormalMap", this);
            particleMask = shaders.Load("ChaosGraphics.ParticleSystem", this);
            instancedNormalMap = shaders.Load("ChaosGraphics.InstancedNormalMap", this);
            fxaa = shaders.Load("ChaosGraphics.FXAA", this);
            taa = shaders.Load("ChaosGraphics.TAA", this);
            skinnedNormalMap = shaders.Load("ChaosGraphics.SkinnedNormalMap", this);
            skinnedInstanceNormalMap = shaders.Load("ChaosGraphics.SkinnedInstancedNormalMap", this);
            text = shaders.Load("ChaosGraphics.Text", this);
            managedText = shaders.Load("ChaosGraphics.ManagedText", this);
            coloredNormalMap = shaders.Load("ChaosGraphics.ColoredNormalMap", this);
        }

        public ShaderContainer.Entry Load(string key, Disposable monitor1, params Disposable[] monitors)
            => shaders.Load(key, monitor1, monitors);

        public ShaderContainer.Entry Load(string key, int param, Disposable monitor1, params Disposable[] monitors)
            => shaders.Load(key, param, monitor1, monitors);

        public bool TryLoad(string key, out ShaderContainer.Entry entry, Disposable monitor1, params Disposable[] monitors)
            => shaders.TryLoad(key, out entry, monitor1, monitors);

        public bool TryLoad(string key, int param, out ShaderContainer.Entry entry, Disposable monitor1, params Disposable[] monitors)
            => shaders.TryLoad(key, param, out entry, monitor1, monitors);

        protected override void DoDispose()
        {
            base.DoDispose();
            shaders.Dispose();
            code.Dispose();
        }
    }
}
