using ChaosFramework.Core;
using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{

    public class TextureArray : Texture
    {
        public override TextureTarget textureTarget => TextureTarget.Texture2DArray;
        public readonly int numLevels;

        /// <param name="levels">
        ///     An array of not-null raw pixel data arrays filled with the raw data
        ///     matching <paramref name="internalFormat"/>.
        /// </param>
        public TextureArray(
            Dispatcher dispatcher,
            Parameters args,
            byte[][] levels
            )
            : base(dispatcher, args)
        {
            numLevels = levels.Length;
            this.dispatcher.RunAndAwait(() =>
            {
                CreateTexture();
                AssignData(levels);
            });
        }

        public TextureArray(
            Dispatcher dispatcher,
            Parameters args,
            int numLevels
            )
            : base(dispatcher, args)
        {
            this.numLevels = numLevels;
            this.dispatcher.RunAndAwait(CreateTexture);
        }

        void AssignData(byte[][] levels)
        {
            GL.BindTexture(TextureTarget.Texture2DArray, textureIndex);
            for (int i = 0; i < levels.Length; i++)
            {
                Graphics.ThrowErrors();
                GL.TexSubImage3D(
                    TextureTarget.Texture2DArray,
                    0,
                    0, 0, i,
                    args.width, args.height, 1,
                    args.pixelFormat,
                    args.pixelType,
                    levels[i]
                    );
                Graphics.ThrowErrors();
            }
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
            Graphics.ThrowErrors();
        }

        void CreateTexture()
        {
            Graphics.ThrowErrors();
            textureIndex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, textureIndex);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)args.magFilter);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)args.MinFilter(true));
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            Graphics.ThrowErrors();
            GL.TexImage3D(
                TextureTarget.Texture2DArray,
                0,
                args.internalFormat,
                args.width,
                args.height,
                numLevels,
                0,
                args.pixelFormat,
                args.pixelType,
                System.IntPtr.Zero
                );
            Graphics.ThrowErrors();
        }
    }
}
