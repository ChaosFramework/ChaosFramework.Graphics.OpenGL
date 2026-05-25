using ChaosFramework.Collections.Immutable;
using ChaosFramework.Core;
using ChaosFramework.Math;
using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{
    public class Framebuffer : Disposable
    {
        public readonly ImmutableArray<Texture> textures;
        public readonly Texture depthBuffer;
        public readonly Bounds2i viewport;

        public int frameBufferName { get; private set; }

        public Framebuffer(Bounds2i viewport, Texture[] textures, Texture depthBuffer = null)
        {
            this.viewport = viewport;
            this.textures = textures;
            frameBufferName = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferName);
            Graphics.ThrowErrors();

            if (depthBuffer != null)
            {
                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthBuffer.textureIndex, 0);
                Graphics.ThrowErrors();
            }

            if (textures != null)
            {
                DrawBuffersEnum[] attachments = new DrawBuffersEnum[textures.Length];
                for (int i = 0; i < textures.Length; i++)
                {
                    GL.FramebufferTexture(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.ColorAttachment0,
                        textures[i].textureIndex,
                        0
                        );
                    Graphics.ThrowErrors();
                    attachments[i] = DrawBuffersEnum.ColorAttachment0 + i;
                }
                GL.DrawBuffers(attachments.Length, attachments);
                Graphics.ThrowErrors();
            }
        }

        public Framebuffer(int width, int height, Texture[] textures, Texture depthBuffer = null)
            : this(new Bounds2i(0, 0, width, height), textures, depthBuffer)
        { }

        protected override void DoDispose()
        {
            base.DoDispose();
            GL.DeleteFramebuffer(frameBufferName);
            Graphics.ThrowErrors();
        }
    }
}
