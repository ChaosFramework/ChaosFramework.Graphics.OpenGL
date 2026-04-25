using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{
    using SetBlendFunc = System.Tuple<BlendingFactor, BlendingFactor>;

    partial class GlStateTracker
    {
        public struct BlendFunc : RenderStateChange<SetBlendFunc>
        {
            public SetBlendFunc oldValue { get; set; }
            public SetBlendFunc currentValue { get; set; }

            public void ResetSate()
                => GL.BlendFunc(oldValue.Item1, oldValue.Item2);

            public void SetState()
                => GL.BlendFunc(currentValue.Item1, currentValue.Item2);

            public void Query()
                => oldValue = new SetBlendFunc(
                    (BlendingFactor)GL.GetInteger(GetPName.BlendSrc),
                    (BlendingFactor)GL.GetInteger(GetPName.BlendDst)
                    );
        }
    }
}
