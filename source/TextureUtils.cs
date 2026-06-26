using ChaosFramework.Math;
using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.InteropServices;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl
{
    using Imaging;

    public static class TextureUtils
    {
        static readonly SysCol.Dictionary<PixelInternalFormat, int> glPixelFormatSizes = new SysCol.Dictionary<PixelInternalFormat, int>();

        static TextureUtils()
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

        public static int GetPixelFormatBitCount(PixelInternalFormat fmt)
        {
            int output;
            if (glPixelFormatSizes.TryGetValue(fmt, out output))
                return output;
            else
                throw new NotSupportedException($"Unknown OpenGL pixel format: {fmt}");
        }

        public static unsafe Rgba8Image ConvertToBitmap(Texture tex)
        {
            int fb, oldPack;
            byte[] pixelData;
            GL.GetInteger(GetPName.ReadFramebufferBinding, out fb);
            Graphics.ThrowErrors();
            using (Framebuffer readFbo = new Framebuffer(new Bounds2i(0, tex.args.size), [tex]))
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readFbo.frameBufferName);
                Graphics.ThrowErrors();
                GL.FramebufferTexture(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, tex.textureIndex, 0);
                Graphics.ThrowErrors();
                GL.GetInteger(GetPName.PackAlignment, out oldPack);
                Graphics.ThrowErrors();
                GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
                Graphics.ThrowErrors();

                pixelData = new byte[tex.args.sizeInBits / 8];
                GL.ReadPixels(0, 0, tex.args.width, tex.args.height, tex.args.pixelFormat, tex.args.pixelType, pixelData);
                Graphics.ThrowErrors();
            }

            GL.PixelStore(PixelStoreParameter.PackAlignment, oldPack);
            Graphics.ThrowErrors();
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fb);
            Graphics.ThrowErrors();

            Rgba8Image result = new Rgba8Image((uint)tex.args.width, (uint)tex.args.height);
            using (RawDataHandle dest = result.GetRawData())
            using (RawDataHandle source = RawDataHandle.Create(pixelData))
            {
                NativeMemory.Copy((void*)source.firstElementAddress, (void*)dest.firstElementAddress, (uint)pixelData.Length);
            }

            return result;
        }

        public static unsafe Rgba8Image TakeScreenshot(Graphics graphics, Action draw)
        {
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            int width = viewport[2];
            int height = viewport[3];

            using (Texture texture = new Texture(
                       graphics.dispatcher,
                       new Texture.Parameters(
                           width,
                           height,
                           pixelType: PixelType.UnsignedByte,
                           internalFormat: PixelInternalFormat.Rgba8,
                           pixelFormat: PixelFormat.Rgba
                       )
                   ))
            using (Framebuffer framebuffer = new Framebuffer(width, height, new Texture[] { texture }))
            {
                Framebuffer oldFrameBuffer = graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
                GL.ClearColor(0, 0, 0, 0);
                Graphics.ThrowErrors();
                GL.Clear(ClearBufferMask.ColorBufferBit);
                Graphics.ThrowErrors();
                draw();
                graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, oldFrameBuffer);

                Graphics.ThrowErrors();
                GL.BindTexture(TextureTarget.Texture2D, texture.textureIndex);
                Graphics.ThrowErrors();

                GL.CopyTexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, viewport[0], viewport[1], viewport[2], viewport[3], 0);
                Graphics.ThrowErrors();
                int packAlignment = GL.GetInteger(GetPName.PackAlignment);
                GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
                Graphics.ThrowErrors();

                byte[] data = new byte[viewport[2] * viewport[3] * 4];
                GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                Graphics.ThrowErrors();


                Rgba8Image result = new Rgba8Image((uint)width, (uint)height);
                using (RawDataHandle dest = result.GetRawData())
                using (RawDataHandle source = RawDataHandle.Create(data))
                {
                    NativeMemory.Copy((void*)source.firstElementAddress, (void*)dest.firstElementAddress, (uint)data.Length);
                }

                GL.PixelStore(PixelStoreParameter.PackAlignment, packAlignment);
                Graphics.ThrowErrors();
                return result;
            }
        }
    }
}
