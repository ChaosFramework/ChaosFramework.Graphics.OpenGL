using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{
    partial class GlStateTracker
    {
        public struct DepthMask : RenderStateChange<bool>
        {
            public bool oldValue { get; set; }
            public bool currentValue { get; set; }

            public void ResetSate()
                => GL.DepthMask(oldValue);

            public void SetState()
                => GL.DepthMask(currentValue);

            public void Query()
                => oldValue = true;
        }
    }
}
