using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;
using OpenGLPixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl
{
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

        /*
        public static Bitmap ConvertToBitmap(Texture tex)
        {
            int fb, oldPack;
            GL.GetInteger(GetPName.ReadFramebufferBinding, out fb);
            Graphics.ThrowErrors();
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            Graphics.ThrowErrors();
            GL.FramebufferTexture(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, tex.textureIndex, 0);
            Graphics.ThrowErrors();
            GL.GetInteger(GetPName.PackAlignment, out oldPack);
            Graphics.ThrowErrors();
            GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
            Graphics.ThrowErrors();

            byte[] pixelData = new byte[tex.args.sizeInBits / 8];
            GL.ReadPixels(0, 0, tex.args.width, tex.args.height, tex.args.pixelFormat, tex.args.pixelType, pixelData);
            Graphics.ThrowErrors();
            GL.PixelStore(PixelStoreParameter.PackAlignment, oldPack);
            Graphics.ThrowErrors();
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fb);
            Graphics.ThrowErrors();

            Bitmap result = new Bitmap(tex.args.width, tex.args.height, DrawingPixelFormat.Format32bppRgb);
            BitmapUtils.SetBitmapBytes(result, pixelData);
            return result;
        }

        public static Bitmap TakeScreenshot(Graphics graphics, Action draw)
        {
            using (Texture texture = new Texture(
                graphics.dispatcher,
                new Texture.Parameters(
                    graphics.width,
                    graphics.height,
                    pixelType: PixelType.UnsignedByte,
                    internalFormat: PixelInternalFormat.Rgba8,
                    pixelFormat: OpenGLPixelFormat.Rgba
                    )
                ))
            using (Framebuffer framebuffer = new Framebuffer(graphics.width, graphics.height, new Texture[] { texture }))
            {
                Framebuffer oldFrameBuffer = graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
                GL.ClearColor(0, 0, 0, 0);
                Graphics.ThrowErrors();
                GL.Clear(ClearBufferMask.ColorBufferBit);
                Graphics.ThrowErrors();
                draw();
                graphics.stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, oldFrameBuffer);

                int[] viewport = new int[4];
                GL.GetInteger(GetPName.Viewport, viewport);
                Graphics.ThrowErrors();
                GL.BindTexture(TextureTarget.Texture2D, texture.textureIndex);
                Graphics.ThrowErrors();

                GL.CopyTexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, viewport[0], viewport[1], viewport[2], viewport[3], 0);
                Graphics.ThrowErrors();
                int packAlignment = GL.GetInteger(GetPName.PackAlignment);
                GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
                Graphics.ThrowErrors();

                byte[] data = new byte[viewport[2] * viewport[3] * 3];
                GL.GetTexImage(TextureTarget.Texture2D, 0, OpenGLPixelFormat.Rgb, PixelType.UnsignedByte, data);
                Graphics.ThrowErrors();

                for (int i = 0; i < data.Length; i += 3)
                {
                    byte tmp = data[i];
                    data[i] = data[i + 2];
                    data[i + 2] = tmp;
                }

                Bitmap bm = new Bitmap(viewport[2], viewport[3], DrawingPixelFormat.Format24bppRgb);
                BitmapUtils.SetBitmapBytes(bm, data);
                bm.RotateFlip(RotateFlipType.RotateNoneFlipY);

                GL.PixelStore(PixelStoreParameter.PackAlignment, packAlignment);
                Graphics.ThrowErrors();
                return bm;
            }
        }
        */
    }
}
