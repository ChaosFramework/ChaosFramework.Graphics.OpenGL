using System;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    abstract class ShaderVariable
    { }

    sealed class UniformBufferVariable : ShaderVariable
    {
        internal IntPtr offset;
        internal int size;
        internal int arrayStride;
        internal UniformBufferVariable(IntPtr offset, int size, int arrayStride)
        {
            this.offset = offset;
            this.size = size;
            this.arrayStride = arrayStride;
        }
    }

    sealed class SamplerVariable : ShaderVariable
    {
        internal int textureUnit;
        internal SamplerVariable(int textureUnit)
        {
            this.textureUnit = textureUnit;
        }
    }
}
