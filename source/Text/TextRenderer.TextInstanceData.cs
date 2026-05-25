using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using System.Runtime.InteropServices;

namespace ChaosFramework.Graphics.OpenGl.Text
{
    partial class TextRenderer
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct TextInstanceData
        {
            public static readonly int SIZE_IN_BYTES = Marshal.SizeOf<TextInstanceData>();

            public Matrix transform;
            public Rgba color;
            public Vector3f channelMultipliers;
            public float fontIndex;
        }
    }
}
