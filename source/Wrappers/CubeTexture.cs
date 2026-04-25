using ChaosFramework.Core;
using OpenTK.Graphics.OpenGL;
using System.IO;

namespace ChaosFramework.Graphics.OpenGl
{
    public class CubeTexture : Texture
    {
        public override TextureTarget textureTarget => TextureTarget.TextureCubeMap;

        CubeTexture(Dispatcher dispatcher, Parameters args)
            : base(dispatcher, args)
        { }

        public static CubeTexture FromStreams(
            Dispatcher dispatcher,
            Stream left,
            Stream right,
            Stream bottom,
            Stream top,
            Stream back,
            Stream front
            )
        {
            int width, height;
            byte[] firstData = BitmapUtils.GetPixelData(left, out width, out height, false);
            CubeTexture result = new CubeTexture(dispatcher, new Parameters(width, height));
            dispatcher.RunAndAwait(() =>
            {
                result.textureIndex = GL.GenTexture();
                GL.BindTexture(TextureTarget.TextureCubeMap, result.textureIndex);
                Graphics.ThrowErrors();

                result.AssignFace(TextureTarget.TextureCubeMapNegativeX, firstData);
                result.AssignFace(TextureTarget.TextureCubeMapPositiveX, BitmapUtils.GetPixelData(right, false));
                result.AssignFace(TextureTarget.TextureCubeMapNegativeY, BitmapUtils.GetPixelData(bottom, false));
                result.AssignFace(TextureTarget.TextureCubeMapPositiveY, BitmapUtils.GetPixelData(top, false));
                result.AssignFace(TextureTarget.TextureCubeMapNegativeZ, BitmapUtils.GetPixelData(back, false));
                result.AssignFace(TextureTarget.TextureCubeMapPositiveZ, BitmapUtils.GetPixelData(front, false));
                GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);
                Graphics.ThrowErrors();

                result.SetSamplerState();
            });

            return result;
        }

        void SetSamplerState()
        {
            GL.TexParameter(
                TextureTarget.TextureCubeMap,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear
                );
            Graphics.ThrowErrors();
            GL.TexParameter(
                TextureTarget.TextureCubeMap,
                TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.LinearMipmapLinear
                );
            Graphics.ThrowErrors();

            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Graphics.ThrowErrors();
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            Graphics.ThrowErrors();
        }

        void AssignFace(TextureTarget target, byte[] data)
        {
            GL.TexImage2D(target, 0, PixelInternalFormat.Rgba8, args.width, args.height, 0, args.pixelFormat, args.pixelType, data);
            Graphics.ThrowErrors();
        }
    }
}
