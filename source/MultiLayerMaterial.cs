using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosFramework.Graphics.AssetContainers;
using ChaosFramework.Math.Vectors;
using OpenTK.Graphics.OpenGL;
using SysCol = System.Collections.Generic;
using System;
using System.Linq;

namespace ChaosFramework.Graphics.OpenGl
{
    using ChaosFramework.Graphics.Imaging;
    using ChaosShader;

    public class MultiLayerMaterial : Disposable
    {
        static readonly SysCol.Dictionary<Shader, Shader.SemanticHandle[]> handleCache
            = new SysCol.Dictionary<Shader, Shader.SemanticHandle[]>();

        static readonly Rgba8[] defaultCols = {
            new Rgba8(128, 128, 255, 255),
            new Rgba8(  0,   0,   0, 255),
            new Rgba8(255, 255, 255, 255),
            new Rgba8(  0,   0,   0, 255),
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

        TextureArray[] textures;

        public MultiLayerMaterial(
            Dispatcher dispatcher,
            Rgba8ImageContainer bitmaps,
            SysCol.IEnumerable<string> normal,
            SysCol.IEnumerable<string> emissive,
            SysCol.IEnumerable<string> diffuse,
            SysCol.IEnumerable<string> specular,
            TextureMinFilter minFilter = TextureMinFilter.Linear,
            TextureMagFilter magFilter = TextureMagFilter.Linear
            )
        {
            LinkedList<Rgba8Image>[] streams = new[] {
                new LinkedList<Rgba8Image>(),
                new LinkedList<Rgba8Image>(),
                new LinkedList<Rgba8Image>(),
                new LinkedList<Rgba8Image>()
            };
            SysCol.IEnumerable<string>[] keyLists = new [] { normal, emissive, diffuse, specular };

            Vector2i[] sizes = new Vector2i[4] { 0, 0, 0, 0 };
            using (Disposable tmpMonitor = new Disposable())
            {
                for (int i = 0; i < streams.Length; i++)
                    foreach (string key in keyLists[i])
                    {
                        Rgba8Image content = bitmaps.Load(key, tmpMonitor).content;
                        if (sizes[i] == 0)
                            sizes[i] = content.Size();
                        else if (content.Size() != sizes[i])
                            throw new InvalidOperationException($"");
                        streams[i].Add(content);
                    }
            }

            for (int i = 0; i < 4; ++i)
                streams[i].Insert(0, new Rgba8Image((uint)sizes[i].x, (uint)sizes[i].y, defaultCols[i]));

            Build(dispatcher, sizes, streams, minFilter, magFilter);
        }

        void Build(
            Dispatcher dispatcher,
            Vector2i[] sizes,
            LinkedList<Rgba8Image>[] allTex,
            TextureMinFilter minFilter = TextureMinFilter.Linear,
            TextureMagFilter magFilter = TextureMagFilter.Linear
            )
        {
            PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8;
            PixelFormat format = PixelFormat.Rgba;
            PixelType pixelType = PixelType.UnsignedByte;

            Graphics.ThrowErrors();

            textures = new TextureArray[4];
            for (int i = 0; i < 4; i++)
            {
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
                    allTex[i].Select(image => (Func<RawDataHandle>)image.GetRawData).ToArray()
                    );
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
