using System;
using System.Runtime.InteropServices;
using ChaosFramework.Graphics.Imaging;
using ChaosFramework.Math;
using OpenTK.Graphics.OpenGL;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl
{
    public partial class Texture
    {
        static readonly SysCol.Dictionary<PixelInternalFormat, int> glPixelFormatSizes = [];

        static Texture()
        {
            glPixelFormatSizes[PixelInternalFormat.R8] = 16;
            glPixelFormatSizes[PixelInternalFormat.R16] = 16;
            glPixelFormatSizes[PixelInternalFormat.R16f] = 16;
            glPixelFormatSizes[PixelInternalFormat.R32f] = 32;

            glPixelFormatSizes[PixelInternalFormat.Rg8] = 16;
            glPixelFormatSizes[PixelInternalFormat.Rg16] = 32;
            glPixelFormatSizes[PixelInternalFormat.Rg16f] = 32;
            glPixelFormatSizes[PixelInternalFormat.Rg32f] = 64;

            glPixelFormatSizes[PixelInternalFormat.Rgb4] = 12;
            glPixelFormatSizes[PixelInternalFormat.Rgb8] = 24;
            glPixelFormatSizes[PixelInternalFormat.Rgb16] = 48;
            glPixelFormatSizes[PixelInternalFormat.Rgb16f] = 48;
            glPixelFormatSizes[PixelInternalFormat.Rgb32f] = 96;

            glPixelFormatSizes[PixelInternalFormat.Rgba4] = 16;
            glPixelFormatSizes[PixelInternalFormat.Rgba8] = 32;
            glPixelFormatSizes[PixelInternalFormat.Rgba16] = 64;
            glPixelFormatSizes[PixelInternalFormat.Rgba16f] = 64;
            glPixelFormatSizes[PixelInternalFormat.Rgba32f] = 128;

            glPixelFormatSizes[PixelInternalFormat.Rgb10A2] = 32;
        }

        static int GetPixelFormatBitCount(PixelInternalFormat fmt)
            => glPixelFormatSizes.TryGetValue(fmt, out int output)
                ? output
                : throw new NotSupportedException($"Unknown OpenGL pixel format: {fmt}");

        int sizeInBits => args.area * GetPixelFormatBitCount(args.internalFormat);

        public unsafe Rgba8Image ToRgba8()
        {
            int oldPack;
            byte[] pixelData;
            GL.GetInteger(GetPName.ReadFramebufferBinding, out int fb);
            Graphics.ThrowErrors();
            using (Framebuffer readFbo = new(new Bounds2i(0, args.size), [this]))
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readFbo.frameBufferName);
                Graphics.ThrowErrors();
                GL.FramebufferTexture(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, textureIndex, 0);
                Graphics.ThrowErrors();
                GL.GetInteger(GetPName.PackAlignment, out oldPack);
                Graphics.ThrowErrors();
                GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
                Graphics.ThrowErrors();

                pixelData = new byte[sizeInBits / 8];
                GL.ReadPixels(0, 0, args.width, args.height, args.pixelFormat, args.pixelType, pixelData);
                Graphics.ThrowErrors();
            }

            GL.PixelStore(PixelStoreParameter.PackAlignment, oldPack);
            Graphics.ThrowErrors();
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fb);
            Graphics.ThrowErrors();

            Rgba8Image result = new((uint)args.width, (uint)args.height);
            using (RawDataHandle dest = result.GetRawData())
            using (RawDataHandle source = RawDataHandle.Create(pixelData))
                NativeMemory.Copy((void*)source.firstElementAddress, (void*)dest.firstElementAddress, (uint)pixelData.Length);

            return result;
        }
    }
}
