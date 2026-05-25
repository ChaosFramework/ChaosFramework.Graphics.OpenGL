using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{
    using SetEnable = System.Tuple<EnableCap, bool>;

    partial class GlStateTracker
    {

        public struct Enable : RenderStateChange<SetEnable>
        {
            public SetEnable oldValue { get; set; }
            public SetEnable currentValue { get; set; }

            public void ResetSate()
            {
                if (oldValue.Item2)
                    GL.Enable(oldValue.Item1);
                else
                    GL.Disable(oldValue.Item1);
            }

            public void SetState()
            {
                if (currentValue.Item2)
                    GL.Enable(currentValue.Item1);
                else
                    GL.Disable(currentValue.Item1);
            }

            public void Query()
                => oldValue = new SetEnable(currentValue.Item1, GL.IsEnabled(currentValue.Item1));
        }
    }
}
