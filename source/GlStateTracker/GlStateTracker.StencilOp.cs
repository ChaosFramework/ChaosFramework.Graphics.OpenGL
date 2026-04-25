using OpenTK.Graphics.OpenGL;
using GlStencilOp = OpenTK.Graphics.OpenGL.StencilOp;

namespace ChaosFramework.Graphics.OpenGl
{
    using SetStencilOp = System.Tuple<StencilOp, StencilOp, StencilOp>;

    partial class GlStateTracker
    {
        public struct StencilOp : RenderStateChange<SetStencilOp>
        {
            public SetStencilOp oldValue { get; set; }
            public SetStencilOp currentValue { get; set; }

            public void ResetSate()
                => GL.StencilOp(oldValue.Item1, oldValue.Item2, oldValue.Item3);

            public void SetState()
                => GL.StencilOp(currentValue.Item1, currentValue.Item2, currentValue.Item3);

            public void Query()
                => oldValue = new SetStencilOp(
                    (GlStencilOp)GL.GetInteger(GetPName.StencilFail),
                    (GlStencilOp)GL.GetInteger(GetPName.StencilPassDepthFail),
                    (GlStencilOp)GL.GetInteger(GetPName.StencilPassDepthPass));
        }
    }
}
