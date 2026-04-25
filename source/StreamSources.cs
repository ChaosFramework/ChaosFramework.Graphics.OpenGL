using ChaosFramework.IO.Streams;
using ChaosFramework.IO.Streams.Sources;
using System.IO;
using static System.Text.Encoding;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl
{
    using AssetContainers;
    using ChaosShader;

    public static class StreamSources
    {
        class ColoredNormalMapStreamSource
               : StreamSource
        {
            const string KEY = "ChaosGraphics.ColoredNormalMap";

            byte[] resource;

            public ColoredNormalMapStreamSource()
            {
                StreamSource dependencies = new StreamSourceCollection(fxSource, cfxSource);
                ShaderCodeContainer importSource = new ShaderCodeContainer(dependencies);

                CodeBlock result = new CodeBlock(importSource, ASCII.GetString(Properties.Resources.FX_NormalMap));
                result.ResolveImports();

                CodeBlock overrideCode = new CodeBlock(importSource, ASCII.GetString(Properties.Resources.CFX_MaterialDefault));
                result.GetMethod("sampleMaterial").Override(overrideCode.GetMethod("sampleColoredMaterial"));
                result.GetMethod("fs_sampleWorldNormalMap").Override(overrideCode.GetMethod("fs_sampleColoredWorldNormalMap"));

                System.Text.StringBuilder bldr = new System.Text.StringBuilder();
                result.WriteCode(bldr);
                resource = ASCII.GetBytes(bldr.ToString());

                importSource.Dispose();
            }

            public bool alive
                => true;

            public bool ContainsKey(string key)
                => key.ToLower().Equals(KEY.ToLower());

            public SysCol.IEnumerable<string> EnumerateKeys()
                => Collections.Util.Yield(KEY);

            public Stream OpenRead(string key)
            {
                if (ContainsKey(key))
                    return new MemoryStream(resource);
                else
                    throw new KeyNotFoundException(key);
            }
        }

        static readonly StreamSource fxSource
            = new PrefixedResourceStreamSource("FX_", "ChaosGraphics.", Properties.Resources.ResourceManager);

        static readonly StreamSource cfxSource
            = new PrefixedResourceStreamSource("CFX_", "ChaosGraphics.", Properties.Resources.ResourceManager);

        static readonly StreamSource generatedSource
            = new ColoredNormalMapStreamSource();

        public static readonly StreamSource shaderCode
            = new StreamSourceCollection(
                   fxSource,
                   cfxSource,
                   generatedSource
               );

        public static readonly StreamSource shaders
            = new StreamSourceCollection(
                fxSource,
                generatedSource
                );

        public static readonly StreamSource meshes
            = new PrefixedResourceStreamSource("Mesh_", "$", Properties.Resources.ResourceManager);

        public static readonly StreamSource textures
            = new PrefixedResourceStreamSource("Tex_", "$", Properties.Resources.ResourceManager);
    }
}
