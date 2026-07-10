using ChaosFramework.Core;
using ChaosFramework.Graphics.Imaging;
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
            Rgba8Image leftImg = Imaging.Formats.Png.FromStream(left, false);
            CubeTexture result = new CubeTexture(dispatcher, new Parameters((int)leftImg.w, (int)leftImg.h));
            dispatcher.RunAndAwait(() =>
            {
                result.textureIndex = GL.GenTexture();
                GL.BindTexture(TextureTarget.TextureCubeMap, result.textureIndex);
                Graphics.ThrowErrors();

                result.AssignFace(TextureTarget.TextureCubeMapNegativeX, leftImg);
                result.AssignFace(TextureTarget.TextureCubeMapPositiveX, Imaging.Formats.Png.FromStream(right, false));
                result.AssignFace(TextureTarget.TextureCubeMapNegativeY, Imaging.Formats.Png.FromStream(bottom, false));
                result.AssignFace(TextureTarget.TextureCubeMapPositiveY, Imaging.Formats.Png.FromStream(top, false));
                result.AssignFace(TextureTarget.TextureCubeMapNegativeZ, Imaging.Formats.Png.FromStream(back, false));
                result.AssignFace(TextureTarget.TextureCubeMapPositiveZ, Imaging.Formats.Png.FromStream(front, false));
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

        void AssignFace(TextureTarget target, Rgba8Image image)
        {
            using (RawDataHandle rawData = image.GetRawData())
            {
                GL.TexImage2D(target, 0, PixelInternalFormat.Rgba8, args.width, args.height, 0, args.pixelFormat, args.pixelType, rawData.firstElementAddress);
                Graphics.ThrowErrors();
            }
        }
    }
}
