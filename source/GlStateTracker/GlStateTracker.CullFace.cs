using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{
    partial class GlStateTracker
    {
        public struct CullFace : RenderStateChange<CullFaceMode>
        {
            public CullFaceMode oldValue { get; set; }
            public CullFaceMode currentValue { get; set; }

            public void ResetSate()
                => GL.CullFace(oldValue);

            public void SetState()
                => GL.CullFace(currentValue);

            public void Query()
                => oldValue = (CullFaceMode)GL.GetInteger(GetPName.CullFaceMode);
        }
    }
}
