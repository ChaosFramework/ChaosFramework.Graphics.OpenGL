using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{
    partial class GlStateTracker
    {
        public struct DepthFunc : RenderStateChange<DepthFunction>
        {
            public DepthFunction oldValue { get; set; }
            public DepthFunction currentValue { get; set; }

            public void ResetSate()
                => GL.DepthFunc(oldValue);

            public void SetState()
                => GL.DepthFunc(currentValue);

            public void Query()
                => oldValue = (DepthFunction)GL.GetInteger(GetPName.DepthFunc);
        }
    }
}
