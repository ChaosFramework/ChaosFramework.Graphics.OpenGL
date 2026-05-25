using ChaosFramework.Core;
using Type = System.Type;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public interface LightInstancerBase
    {
        Type LightType();
        void Reset();
        bool Add(DeferredShader shader, Light l);
        void Render(DeferredShader shader);
    }

    public abstract class LightInstancer<Light>
        : Disposable
        , LightInstancerBase
        where Light: Lights.Light
    {
        Type LightInstancerBase.LightType() => typeof(Light);

        bool LightInstancerBase.Add(DeferredShader shader, Lights.Light l)
            => Add(shader, (Light)l);

        public abstract void Render(DeferredShader shader);

        public abstract void Reset();

        protected abstract bool Add(DeferredShader shader, Light l);
    }
}
