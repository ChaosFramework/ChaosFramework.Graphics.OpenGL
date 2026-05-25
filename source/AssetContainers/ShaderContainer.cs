using System;
using ChaosFramework.Core;
using System.Reflection;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Collections;
using ChaosFramework.IO.Containers;
using ChaosFramework.IO.Streams;
using ChaosFramework.Math;

namespace ChaosFramework.Graphics.OpenGl.AssetContainers
{
    using ChaosShader.CompilationErrors;

    public static class ShaderContainerEntryExtensions
    {
        public static void SetValue(this ShaderContainer.Entry @this, string variable, Matrix value)
            => @this.content.SetValue(variable, value);

        public static void SetValue(this ShaderContainer.Entry @this, Shader.SemanticHandle semantic, Matrix value)
            => @this.content.SetValue(semantic, value);

        public static void SetValue<FieldType>(this ShaderContainer.Entry @this, string variable, FieldType value)
            => @this.content.SetValue(variable, value);

        public static void SetValue<FieldType>(this ShaderContainer.Entry @this, Shader.SemanticHandle semantic, FieldType value)
            => @this.content.SetValue(semantic, value);

        public static void SetValue<EntryType>(this ShaderContainer.Entry @this, string variable, AssetContainer<EntryType>.Entry value)
            where EntryType : class
            => @this.content.SetValue(variable, value);

        public static void SetValue<EntryType>(
            this ShaderContainer.Entry @this,
            Shader.SemanticHandle semantic,
            AssetContainer<EntryType>.Entry value
            )
            where EntryType : class
            => @this.content.SetValue(semantic, value);

        public static Shader.SemanticMapping BeginPass(this ShaderContainer.Entry shader, string pass)
            => shader.content.BeginPass(pass);

        public static void EndPass(this ShaderContainer.Entry shader)
            => shader.content.EndPass();
    }

    public class ShaderContainer : ParameterizedAssetContainer<Shader, int>
    {
        bool initialized = false;
        public readonly Graphics graphics;
        public readonly Dispatcher dispatcher;

        readonly ShaderCodeContainer importSource;

        public ShaderContainer(StreamSource streamSource, Graphics graphics, ShaderCodeContainer importSource)
            : this(streamSource, graphics, importSource, false, graphics.coreProfile)
        { }

        public ShaderContainer(
            StreamSource streamSource,
            Graphics graphics,
            ShaderCodeContainer importSource,
            bool dispatch,
            int defaultShaderVersion
            )
            : base(streamSource, false)
        {
            defaultParameter = defaultShaderVersion;
            this.graphics = graphics;
            this.importSource = importSource ?? Shaders.code;
            dispatcher = dispatch ? graphics.dispatcher : null;
            LinkedList<Tuple<int, MethodInfo>> initializers = new LinkedList<Tuple<int, MethodInfo>>();

            initialized = true;
            foreach (Entry s in this)
                Compile(s.key, s.content);
        }

        protected override Shader LoadFromStream(Key key, System.IO.Stream resource, CancellationToken cancel)
        {
            byte[] bytes = new byte[resource.Length];
            resource.Read(bytes, 0, bytes.Length);
            Shader shader = new Shader(graphics, importSource, bytes, ((ParameterizedKey)key).param);

            if (initialized)
                Compile(key, shader);

            return shader;
        }

        void Compile(Key key, Shader shader)
        {
            try
            {
                shader.Compile();
            }
            catch (CompilationError ex)
            {
                throw new AssetLoadException<Shader>(key, ex);
            }
        }

        protected override void DisposeItem(Shader obj)
            => obj?.Dispose();
    }
}
