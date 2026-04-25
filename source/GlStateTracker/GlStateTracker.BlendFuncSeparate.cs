using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{
    using SetBlendFuncSeparate = System.Tuple<BlendingFactorSrc, BlendingFactorDest, BlendingFactorSrc, BlendingFactorDest>;

    partial class GlStateTracker
    {
        public struct BlendFuncSeperate : RenderStateChange<SetBlendFuncSeparate>
        {
            public SetBlendFuncSeparate oldValue { get; set; }
            public SetBlendFuncSeparate currentValue { get; set; }


            public void ResetSate()
                => GL.BlendFuncSeparate(oldValue.Item1, oldValue.Item2, oldValue.Item3, oldValue.Item4);

            public void SetState()
                => GL.BlendFuncSeparate(currentValue.Item1, currentValue.Item2, currentValue.Item3, currentValue.Item4);

            public void Query()
            {
                oldValue = new SetBlendFuncSeparate(
                    (BlendingFactorSrc)GL.GetInteger(GetPName.BlendSrcRgb), (BlendingFactorDest)GL.GetInteger(GetPName.BlendDstRgb),
                    (BlendingFactorSrc)GL.GetInteger(GetPName.BlendSrcAlpha), (BlendingFactorDest)GL.GetInteger(GetPName.BlendDstAlpha)
                    );
            }
        }
    }
}
