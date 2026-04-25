using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Core;
using System.Collections.Generic;
using System.IO;

namespace ChaosFramework.Graphics.OpenGl
{
    using ChaosShader;

    public class Material : Disposable
    {
        public struct LayerKeys
        {
            public string normal, emissive, diffuse, specular, reflective;

            public LayerKeys(string normal, string emissive, string diffuse, string specular, string reflective)
            {
                this.normal = normal;
                this.emissive = emissive;
                this.diffuse = diffuse;
                this.specular = specular;
                this.reflective = reflective;
            }
        }

        static Dictionary<Shader, Shader.SemanticHandle[]> handleCache = new Dictionary<Shader, Shader.SemanticHandle[]>();

        static Shader.SemanticHandle[] GetOrRegisterHandles(Shader shader)
        {
            Shader.SemanticHandle[] fxHandles;
            if (!handleCache.TryGetValue(shader, out fxHandles))
            {
                fxHandles = new Shader.SemanticHandle[] {
                    shader.GetParameterBySemantic("NORMAL_MAP"),
                    shader.GetParameterBySemantic("EMISSIVE_MAP"),
                    shader.GetParameterBySemantic("DIFFUSE_MAP"),
                    shader.GetParameterBySemantic("SPECULAR_MAP"),
                    shader.GetParameterBySemantic("REFLECTIVE_MAP")
                };
                handleCache.Add(shader, fxHandles);
                shader.AddOnDispose(() => handleCache.Remove(shader));
            }

            return fxHandles;
        }

        public static LayerKeys Parse(StreamReader reader, string key)
        {
            LayerKeys result = default(LayerKeys);
            while (!reader.EndOfStream)
            {
                string[] split = reader.ReadLine().Split(':');
                if (split.Length < 2)
                    continue;

                string textureKey = split[1].Trim();
                if (textureKey.StartsWith("./"))
                {
                    textureKey = textureKey.Remove(0, 2);
                    int lastSlash = key.LastIndexOf('\\');
                    if (lastSlash > 0)
                        textureKey = key.Substring(0, lastSlash) + "\\" + textureKey;

                    textureKey = ChaosUtil.Platform.Paths.Normalization.NormalizeRelative(textureKey);
                }

                switch (split[0].Trim().ToLower())
                {
                    case "emissive":
                        result.emissive = textureKey;
                        break;

                    case "diffuse":
                        result.diffuse = textureKey;
                        break;

                    case "specular":
                        result.specular = textureKey;
                        break;

                    case "normal":
                        result.normal = textureKey;
                        break;

                    case "reflective":
                        result.reflective = textureKey;
                        break;

                    default:
                        throw new InvalidDataException($"unknown semantic {split[0]} in '{key}'");
                }
            }

            return result;
        }

        public readonly TextureContainer.Entry normalMap;
        public readonly TextureContainer.Entry emissiveMap;
        public readonly TextureContainer.Entry diffuseMap;
        public readonly TextureContainer.Entry specularMap;
        public readonly TextureContainer.Entry reflectiveMap;

        readonly Graphics graphics;

        internal Material(Graphics graphics)
            : this(graphics.textures, "$NormalMap", null, "$DiffuseMap", null, null)
        { }

        public Material(TextureContainer source, string normal, string emissive, string diffuse, string specular, string reflective)
            : this(source, new LayerKeys(normal, emissive, diffuse, specular, reflective))
        { }

        public Material(TextureContainer source, Stream stream, string key)
            : this(source, Parse(new StreamReader(stream), key))
        { }

        public Material(TextureContainer source, LayerKeys keys)
        {
            normalMap = keys.normal == null ? null : source.Load(keys.normal, this);
            emissiveMap = keys.emissive == null ? null : source.Load(keys.emissive, this);
            diffuseMap = keys.diffuse == null ? null : source.Load(keys.diffuse, this);
            specularMap = keys.specular == null ? null : source.Load(keys.specular, this);
            reflectiveMap = keys.reflective == null ? null : source.Load(keys.reflective, this);
        }

        public Material(
            TextureContainer.Entry normalMap,
            TextureContainer.Entry emissiveMap,
            TextureContainer.Entry diffuseMap,
            TextureContainer.Entry specularMap,
            TextureContainer.Entry reflectiveMap)
        {
            (this.normalMap = normalMap)?.AddMonitors(this);
            (this.emissiveMap = emissiveMap)?.AddMonitors(this);
            (this.diffuseMap = diffuseMap)?.AddMonitors(this);
            (this.specularMap = specularMap)?.AddMonitors(this);
            (this.reflectiveMap = reflectiveMap)?.AddMonitors(this);
        }

        public virtual void SetValues(Shader fx)
        {
            Shader.SemanticHandle[] fxHandles = GetOrRegisterHandles(fx);
            if (fxHandles[0] != null) fx.SetValue(fxHandles[0], normalMap ?? fx.graphics.defaultMaterial.content.normalMap);
            if (fxHandles[1] != null) fx.SetValue(fxHandles[1], emissiveMap ?? fx.graphics.defaultMaterial.content.emissiveMap);
            if (fxHandles[2] != null) fx.SetValue(fxHandles[2], diffuseMap ?? fx.graphics.defaultMaterial.content.diffuseMap);
            if (fxHandles[3] != null) fx.SetValue(fxHandles[3], specularMap ?? fx.graphics.defaultMaterial.content.specularMap);
            if (fxHandles[4] != null) fx.SetValue(fxHandles[4], reflectiveMap ?? fx.graphics.defaultMaterial.content.reflectiveMap);
        }
    }
}
