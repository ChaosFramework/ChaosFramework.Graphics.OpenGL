using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{
    using SetStencilFunc = System.Tuple<StencilFunction, int, int>;

    partial class GlStateTracker
    {
        public struct StencilFunc : RenderStateChange<SetStencilFunc>
        {
            public SetStencilFunc oldValue { get; set; }
            public SetStencilFunc currentValue { get; set; }

            public void ResetSate()
                => GL.StencilFunc(oldValue.Item1, oldValue.Item2, oldValue.Item3);

            public void SetState()
                => GL.StencilFunc(currentValue.Item1, currentValue.Item2, currentValue.Item3);

            public void Query()
                => oldValue = new SetStencilFunc(
                    (StencilFunction)GL.GetInteger(GetPName.StencilFunc),
                    GL.GetInteger(GetPName.StencilRef),
                    GL.GetInteger(GetPName.StencilWritemask)
                    );
        }
    }
}
