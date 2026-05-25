using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{
    partial class GlStateTracker
    {
        public struct BlendEquation : RenderStateChange<BlendEquationMode>
        {
            public BlendEquationMode oldValue { get; set; }
            public BlendEquationMode currentValue { get; set; }

            public void ResetSate()
                => GL.BlendEquation(oldValue);

            public void SetState()
                => GL.BlendEquation(currentValue);

            public void Query()
                => oldValue = (BlendEquationMode)GL.GetInteger(GetPName.BlendEquationRgb);
        }
    }
}
