using ChaosFramework.Core;
using ChaosFramework.Graphics.Imaging;
using OpenTK.Graphics.OpenGL;
using System.IO;
using System.Threading.Tasks;

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
            Task<Rgba8Image>[] loadTasks = [
                Task.Run(() => Imaging.Formats.Png.FromStream(left, false)),
                Task.Run(() => Imaging.Formats.Png.FromStream(right, false)),
                Task.Run(() => Imaging.Formats.Png.FromStream(bottom, false)),
                Task.Run(() => Imaging.Formats.Png.FromStream(top, false)),
                Task.Run(() => Imaging.Formats.Png.FromStream(back, false)),
                Task.Run(() => Imaging.Formats.Png.FromStream(front, false)),
            ];
            Task loadAll = Task.WhenAll(loadTasks);
            loadAll.Wait();
            if (!loadAll.IsCompletedSuccessfully)
                throw loadAll.Exception;

            Rgba8Image leftImg = loadTasks[0].Result;
            CubeTexture result = new CubeTexture(dispatcher, new Parameters((int)leftImg.w, (int)leftImg.h));

            dispatcher.RunAndAwait(() =>
            {
                result.textureIndex = GL.GenTexture();
                GL.BindTexture(TextureTarget.TextureCubeMap, result.textureIndex);
                Graphics.ThrowErrors();

                result.AssignFace(TextureTarget.TextureCubeMapNegativeX, leftImg);
                result.AssignFace(TextureTarget.TextureCubeMapPositiveX, loadTasks[1].Result);
                result.AssignFace(TextureTarget.TextureCubeMapNegativeY, loadTasks[2].Result);
                result.AssignFace(TextureTarget.TextureCubeMapPositiveY, loadTasks[3].Result);
                result.AssignFace(TextureTarget.TextureCubeMapNegativeZ, loadTasks[4].Result);
                result.AssignFace(TextureTarget.TextureCubeMapPositiveZ, loadTasks[5].Result);
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
