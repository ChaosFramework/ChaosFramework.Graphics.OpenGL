using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosFramework.Graphics.AssetContainers;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math.Vectors;
using OpenTK.Graphics.OpenGL;
using ChaosUtil.Primitives;
using System.Drawing;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl
{
    using ChaosShader;

    public class MultiLayerMaterial : Disposable
    {
        static readonly SysCol.Dictionary<Shader, Shader.SemanticHandle[]> handleCache
            = new SysCol.Dictionary<Shader, Shader.SemanticHandle[]>();

        static readonly Rgba[] defaultCols = {
            new Rgba(0.5f, 0.5f, 1.0f, 1.0f),
            Rgba.OPAQUE_BLACK,
            Rgba.OPAQUE_WHITE,
            Rgba.OPAQUE_BLACK
        };

        static Shader.SemanticHandle[] GetOrRegisterHandles(Shader shader)
        {
            Shader.SemanticHandle[] handles;
            if (handleCache.TryGetValue(shader, out handles))
                return handles;

            handles = new Shader.SemanticHandle[] {
                shader.GetParameterBySemantic("NORMAL_MAP"),
                shader.GetParameterBySemantic("EMISSIVE_MAP"),
                shader.GetParameterBySemantic("DIFFUSE_MAP"),
                shader.GetParameterBySemantic("SPECULAR_MAP"),
            };

            handleCache[shader] = handles;
            shader.AddOnDispose(() => handleCache.Remove(shader));

            return handles;
        }

        static byte[] GetBytesForScaledBitmap(Bitmap source, Vector2i scaleTo, bool flipY = true)
        {
            if (source != null)
                using (Bitmap scaled = BitmapUtils.ScaleBitmap(source, scaleTo.x, scaleTo.y))
                    return BitmapUtils.GetPixelData(scaled, flipY);
            else
                return null;
        }

        TextureArray[] textures;

        public MultiLayerMaterial(
            Dispatcher dispatcher,
            BitmapContainer bitmaps,
            SysCol.IEnumerable<string> normal,
            SysCol.IEnumerable<string> emissive,
            SysCol.IEnumerable<string> diffuse,
            SysCol.IEnumerable<string> specular,
            PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8,
            PixelFormat format = PixelFormat.Rgba,
            PixelType pixelType = PixelType.UnsignedByte,
            TextureMinFilter minFilter = TextureMinFilter.Linear,
            TextureMagFilter magFilter = TextureMagFilter.Linear
            )
        {
            LinkedList<byte[]>[] streams = new[] {
                new LinkedList<byte[]>(),
                new LinkedList<byte[]>(),
                new LinkedList<byte[]>(),
                new LinkedList<byte[]>()
            };

            Vector2i[] sizes = new Vector2i[4] { 2, 2, 2, 2 };
            using (Disposable tmpMonitor = new Disposable())
            {
                SysCol.IEnumerable<string>[] maps = new[] {
                    normal ?? Array<string>.empty,
                    emissive ?? Array<string>.empty,
                    diffuse ?? Array<string>.empty,
                    specular ?? Array<string>.empty
                };

                for (int i = 0; i < maps.Length; i++)
                    foreach (string key in maps[i])
                        sizes[i] = sizes[i].Max(bitmaps.Load(key, tmpMonitor).content.Size);

                for (int i = 0; i < maps.Length; i++)
                    foreach (string key in maps[i])
                        streams[i].Add(GetBytesForScaledBitmap(bitmaps.Load(key, tmpMonitor).content, sizes[i]));
            }

            for (int i = 0; i < 4; ++i)
                streams[i].Insert(0, BitmapUtils.CreateSingleColorImageData(
                    defaultCols[i],
                    sizes[i].x,
                    sizes[i].y,
                    sizes[i].x * TextureUtils.GetPixelFormatBitCount(internalFormat) / 8
                    ));

            Build(dispatcher,
                sizes,
                streams[0].ToArray(),
                streams[1].ToArray(),
                streams[2].ToArray(),
                streams[3].ToArray(),
                internalFormat,
                format,
                pixelType,
                minFilter,
                magFilter
            );
        }

        void Build(
            Dispatcher dispatcher,
            Vector2i[] sizes,
            byte[][] normal,
            byte[][] emissive,
            byte[][] diffuse,
            byte[][] specular,
            PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8,
            PixelFormat format = PixelFormat.Rgba,
            PixelType pixelType = PixelType.UnsignedByte,
            TextureMinFilter minFilter = TextureMinFilter.Linear,
            TextureMagFilter magFilter = TextureMagFilter.Linear
            )
        {

            Graphics.ThrowErrors();
            byte[][][] allTex = { normal, emissive, diffuse, specular };

            textures = new TextureArray[4];
            for (int i = 0; i < 4; i++)
            {
                int numExpectedBytes = (sizes[i].x * sizes[i].y * TextureUtils.GetPixelFormatBitCount(internalFormat) + 7) / 8;

                textures[i] = new TextureArray(
                    dispatcher,
                    new Texture.Parameters(
                        sizes[i].x,
                        sizes[i].y,
                        pixelType: pixelType,
                        pixelFormat: format,
                        internalFormat: internalFormat,
                        minFilter: minFilter,
                        magFilter: magFilter
                        ),
                    allTex[i]
                    );

                for (int j = 0; j < allTex[i].Length; j++)
                {
                    if (allTex[i][j] != null && numExpectedBytes > allTex[i][j].Length)
                        throw new System.InvalidOperationException(
                            $"The data array for Multilayer Material Layer ({i}, {j}) is shorter"
                            + $" than the expected minimum count of {numExpectedBytes} bytes."
                            );
                }
            }
            Graphics.ThrowErrors();
        }

        public virtual void SetValues(Shader shader)
        {
            Shader.SemanticHandle[] handles = GetOrRegisterHandles(shader);

            for (int i = 0; i < 4; i++)
                if (handles[i] != null)
                    shader.SetValue(handles[i], textures[i]);
        }

        protected override void DoDispose()
        {
            base.DoDispose();

            foreach (TextureArray t in textures)
                t.Dispose();
        }
    }
}
