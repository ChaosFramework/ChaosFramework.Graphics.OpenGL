using ChaosFramework.Core;
using ChaosFramework.Math.Vectors;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using Imaging = System.Drawing.Imaging;

namespace ChaosFramework.Graphics.OpenGl
{
    public class Texture : Disposable
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

            public Vector2i size => new Vector2i(width, height);

            public int area => width * height;

            public int sizeInBits => area * TextureUtils.GetPixelFormatBitCount(internalFormat);

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
            using (Bitmap bmp = new Bitmap(sourceFile))
                return FromBitmap(dispatcher, bmp, scale, flipY);
        }

        public static Texture FromStream(Dispatcher dispatcher, System.IO.Stream sourceFile, float scale = 1, bool flipY = true)
            => FromBitmap(dispatcher, BitmapUtils.ReadBitmapFromStream(sourceFile), scale, flipY);

        public static Texture FromBitmap(Dispatcher dispatcher, Bitmap bmp, float scale = 1, bool flipY = true)
        {
            if (scale != 1 || bmp.PixelFormat != Imaging.PixelFormat.Format32bppArgb)
            {
                Bitmap tmp = bmp;
                bmp = BitmapUtils.ConvertBitmap(
                    tmp,
                    Imaging.PixelFormat.Format32bppArgb, (int)(tmp.Width * scale), (int)(tmp.Height * scale)
                    );
                tmp.Dispose();
            }

            byte[] bitmapData = BitmapUtils.GetPixelData(bmp);
            return new Texture(
                dispatcher,
                new Parameters(
                    bmp.Width,
                    bmp.Height,
                    PixelType.UnsignedByte,
                    null,
                    null,
                    PixelFormat.Rgba,
                    PixelInternalFormat.Rgba8
                ),
                BitmapUtils.GetPixelData(bmp, flipY)
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
            byte[] data = null)
            : this(dispatcher, args)
        {
            this.dispatcher = dispatcher;
            this.dispatcher.RunAndAwait(() => Construct(data));
        }

        void Construct(byte[] data)
        {
            Graphics.ThrowErrors();
            textureIndex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureIndex);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)args.magFilter);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)args.MinFilter(data != null));
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            Graphics.ThrowErrors();

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                args.internalFormat,
                args.width,
                args.height,
                0,
                args.pixelFormat,
                args.pixelType,
                data
                );
            Graphics.ThrowErrors();

            if (data != null)
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
