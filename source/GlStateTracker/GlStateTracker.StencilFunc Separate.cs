using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{
    using SetStencilFuncSeparate = System.Tuple<StencilFace, StencilFunction, int, int>;

    partial class GlStateTracker
    {
        public struct StencilFuncSeparate : RenderStateChange<SetStencilFuncSeparate>
        {
            public SetStencilFuncSeparate oldValue { get; set; }
            public SetStencilFuncSeparate currentValue { get; set; }

            public void ResetSate()
                => GL.StencilFuncSeparate(oldValue.Item1, oldValue.Item2, oldValue.Item3, oldValue.Item4);

            public void SetState()
                => GL.StencilFuncSeparate(currentValue.Item1, currentValue.Item2, currentValue.Item3, currentValue.Item4);

            public void Query()
                => oldValue = new SetStencilFuncSeparate(
                    StencilFace.Front,
                    (StencilFunction)GL.GetInteger(GetPName.StencilFunc),
                    GL.GetInteger(GetPName.StencilRef),
                    GL.GetInteger(GetPName.StencilWritemask)
                    );
        }
    }
}
