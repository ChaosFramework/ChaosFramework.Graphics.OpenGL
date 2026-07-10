using ChaosFramework.Core;
using ChaosFramework.Graphics.Imaging;
using ChaosFramework.Math.Vectors;
using OpenTK.Graphics.OpenGL;
using System;
using System.IO;

namespace ChaosFramework.Graphics.OpenGl
{
    public partial class Texture : Disposable
    {
        public struct Parameters
        {
            public readonly int width;
            public readonly int height;

            public readonly PixelFormat pixelFormat;
            public readonly PixelInternalFormat internalFormat;

            public readonly PixelType pixelType;

            public readonly TextureMagFilter magFilter;

            readonly TextureMinFilter? minFilter;

            // TODO: Re-evaluate on which graphics cards 1 pixel wide or high textures cause mayhem
            public bool mayBreakBecauseTiny => width == 1 || height == 1;

            public float ratio => (float)width / height;

            public readonly Vector2i size => new(width, height);

            public int area => width * height;

            public Parameters(
                int width,
                int height,
                PixelType pixelType = PixelType.UnsignedByte,
                TextureMinFilter? minFilter = null,
                TextureMagFilter? magFilter = null,
                PixelFormat pixelFormat = PixelFormat.Rgba,
                PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8
                )
            {
                this.width = width;
                this.height = height;
                this.pixelType = pixelType;
                this.pixelFormat = pixelFormat;
                this.internalFormat = internalFormat;

                this.minFilter = minFilter;

                bool mayBreakBecauseTiny = width == 1 || height == 1;
                this.magFilter = magFilter
                    ?? (mayBreakBecauseTiny
                        ? TextureMagFilter.Nearest
                        : TextureMagFilter.Linear
                       );
            }

            public TextureMinFilter MinFilter(bool hasData)
                => minFilter ?? (mayBreakBecauseTiny
                    ? TextureMinFilter.Nearest
                    : hasData
                        ? TextureMinFilter.LinearMipmapLinear
                        : TextureMinFilter.Linear
                   );
        }

        public static Texture FromFile(Dispatcher dispatcher, string sourceFile, float scale = 1, bool flipY = true)
        {
            using (FileStream str = File.OpenRead(sourceFile))
                return FromBitmap(dispatcher, Imaging.Formats.Png.FromStream(str, flipY), scale);
        }

        public static Texture FromStream(Dispatcher dispatcher, System.IO.Stream sourceFile, float scale = 1, bool flipY = true)
            => FromBitmap(dispatcher, Imaging.Formats.Png.FromStream(sourceFile, flipY), scale);

        public static Texture FromBitmap(Dispatcher dispatcher, Rgba8Image bmp, float scale = 1)
        {
            if (scale != 1)
            {
                throw new NotImplementedException("Scaling while loading sounds like a bad idea tbh.");
            }

            return new Texture(
                dispatcher,
                new Parameters(
                    (int)bmp.width,
                    (int)bmp.height,
                    PixelType.UnsignedByte,
                    null,
                    null,
                    PixelFormat.Rgba,
                    PixelInternalFormat.Rgba8
                ),
                bmp.GetRawData
                );
        }

        public readonly Parameters args;
        protected readonly Dispatcher dispatcher;

        public virtual TextureTarget textureTarget => TextureTarget.Texture2D;
        public int textureIndex { get; protected set; }

        protected Texture(Dispatcher dispatcher, Parameters args)
        {
            this.dispatcher = dispatcher;
            this.args = args;
        }

        public Texture(
            Dispatcher dispatcher,
            Parameters args,
            Func<RawDataHandle> rawDataGetter = null)
            : this(dispatcher, args)
        {
            this.dispatcher = dispatcher;
            this.dispatcher.RunAndAwait(() => Construct(rawDataGetter));
        }

        void Construct(Func<RawDataHandle> rawDataGetter)
        {
            Graphics.ThrowErrors();
            textureIndex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureIndex);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)args.magFilter);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)args.MinFilter(rawDataGetter != null));
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            Graphics.ThrowErrors();

            using (RawDataHandle rawData = rawDataGetter?.Invoke())
            {
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    args.internalFormat,
                    (int)args.width,
                    (int)args.height,
                    0,
                    args.pixelFormat,
                    args.pixelType,
                    rawData?.firstElementAddress ?? IntPtr.Zero
                    );
                Graphics.ThrowErrors();
            }

            if (rawDataGetter != null)
            {
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                Graphics.ThrowErrors();
            }
        }

        public void SetTextureWrap(TextureWrapMode both)
            => SetTextureWrap(both, both);

        public void SetTextureWrap(TextureWrapMode s, TextureWrapMode t)
            => dispatcher.RunAndAwait(() =>
            {
                GL.BindTexture(TextureTarget.Texture2D, textureIndex);
                Graphics.ThrowErrors();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)s);
                Graphics.ThrowErrors();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)t);
                Graphics.ThrowErrors();
            });

        protected override void DoDispose()
        {
            base.DoDispose();
            dispatcher.Dispatch(FreeResource);
        }

        void FreeResource()
        {
            GL.DeleteTexture(textureIndex);
        }
    }
}
