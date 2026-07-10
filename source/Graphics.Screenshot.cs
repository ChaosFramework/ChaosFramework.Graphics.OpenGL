using System;
using System.Runtime.InteropServices;
using ChaosFramework.Graphics.Imaging;
using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{
    public partial class Graphics
    {
        public unsafe Rgba8Image TakeScreenshot(Action draw)
        {
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            int width = viewport[2];
            int height = viewport[3];

            using (Texture texture = new(
                dispatcher,
                new Texture.Parameters(
                    width,
                    height,
                    pixelType: PixelType.UnsignedByte,
                    internalFormat: PixelInternalFormat.Rgba8,
                    pixelFormat: PixelFormat.Rgba
                )))
            using (Framebuffer framebuffer = new(width, height, [texture]))
            {
                Framebuffer oldFrameBuffer = stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
                GL.ClearColor(0, 0, 0, 0);
                ThrowErrors();
                GL.Clear(ClearBufferMask.ColorBufferBit);
                ThrowErrors();
                draw();
                stateTracker.BindFramebuffer(FramebufferTarget.Framebuffer, oldFrameBuffer);

                ThrowErrors();
                GL.BindTexture(TextureTarget.Texture2D, texture.textureIndex);
                ThrowErrors();

                GL.CopyTexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, viewport[0], viewport[1], viewport[2], viewport[3], 0);
                ThrowErrors();
                int packAlignment = GL.GetInteger(GetPName.PackAlignment);
                GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
                ThrowErrors();

                byte[] data = new byte[viewport[2] * viewport[3] * 4];
                GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                ThrowErrors();

                Rgba8Image result = new((uint)width, (uint)height);
                using (RawDataHandle dest = result.GetRawData())
                using (RawDataHandle source = RawDataHandle.Create(data))
                    NativeMemory.Copy((void*)source.firstElementAddress, (void*)dest.firstElementAddress, (uint)data.Length);

                GL.PixelStore(PixelStoreParameter.PackAlignment, packAlignment);
                ThrowErrors();
                return result;
            }
        }
    }
}
