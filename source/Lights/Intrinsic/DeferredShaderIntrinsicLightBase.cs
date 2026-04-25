using ChaosFramework.Core;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.Lights.Intrinsic
{
    using AssetContainers;
    using ChaosShader;
    using System;

    public abstract class DeferredShaderIntrinsicLights
    {
        public class ShadeInput
        {
            public readonly string type;
            public readonly string semantic;

            internal virtual Array untypedData => null;

            public ShadeInput(string type, string semantic)
            {
                this.type = type;
                this.semantic = semantic;
            }

            internal virtual void Reset() { }
        }

        public abstract class LightShadeInput<Light>
            : ShadeInput
            where Light : Lights.Light
        {
            public LightShadeInput(string type, string semantic)
                : base(type, semantic)
            { }

            internal abstract void SetValue(Light l, int i);
        }

        protected const string TEMPLATE_METHOD_NAME = "fs_add";

        [ThreadStatic]
        static readonly MD5 hash = MD5.Create();

        static bool IsMaxLightsDefine(Define x) => x.type == "MAX_LIGHTS";
        static bool IsShadeMethodDefine(Define x) => x.type == "SHADE_METHOD";
        static bool IsShadeArgsDefine(Define x) => x.type == "SHADE_ARGS";
        static bool IsIntrinsicsDefine(Define x) => x.type == "INTRINSICS";

        static string ToHex(byte b) => b.ToString("X2");

        static string TransformKey(string key)
            => $"x{string.Join(null, hash.ComputeHash(Encoding.UTF8.GetBytes(key)).Select(ToHex))}";

        public readonly ushort maxLights;

        readonly string shaderKey;
        readonly string shadeMethodName;
        readonly string specializedFragmentShaderMethodName;
        readonly string specializedShadeMethodName;
        protected readonly ShadeInput[] allShadeInputs;

        public DeferredShaderIntrinsicLights(ushort maxLights)
        {
            this.maxLights = maxLights;
            shaderKey = GetShaderKey();
            shadeMethodName = GetShadeMethodName();
            specializedShadeMethodName = $"{TransformKey(shaderKey)}_{shadeMethodName}_{maxLights}_{GetWhateverCustomizationIsNeeded()}";
            specializedFragmentShaderMethodName = $"fs_{specializedShadeMethodName}";
            allShadeInputs = CreateShadeInputs().ToArray();
        }

        protected abstract string GetShadeMethodName();
        protected abstract string GetShaderKey();
        protected abstract SysCol.IEnumerable<ShadeInput> CreateShadeInputs();
        protected virtual string GetWhateverCustomizationIsNeeded() => string.Empty;

        public abstract void Clear();

        public abstract bool AddLight(DeferredShader s, Light l);

        public void SetValues(Shader shader)
        {
            foreach (ShadeInput arg in allShadeInputs)
                if (arg.untypedData != null)
                    shader.SetValue($"{specializedShadeMethodName}{arg.semantic}", arg.untypedData);
        }

        public void AddIntrinsics(ShaderCodeContainer shaderSource, CodeBlock code)
        {
            if (code.GetMethod(specializedFragmentShaderMethodName) != null)
                throw new System.InvalidOperationException($"fragment shader '{specializedFragmentShaderMethodName}' not found");

            using (Disposable monitor = new Disposable())
            {
                CodeBlock lightShader = shaderSource.Load(shaderKey, monitor);

                ShaderCodeContainer.Entry templateCodeBlock = shaderSource.Load("ChaosGraphics.DeferredShaderIntrinsics", monitor);
                CodeBlock specialized = (CodeBlock)templateCodeBlock.content.Clone();

                // generate light buffers
                StringBuilder shadeArguments = new StringBuilder();
                {
                    bool more = false;
                    Method fragmentShaderMethodTemplate = (Method)specialized.GetMethod(TEMPLATE_METHOD_NAME).Clone();

                    foreach (ShadeInput input in allShadeInputs)
                    {
                        if (more)
                            shadeArguments.Append(", ");
                        else
                            more = true;

                        if (input.untypedData != null)
                        {
                            code.AddVariable(new Variable(
                                default(CodeBlock.Modifier),
                                input.type,
                                $"{specializedShadeMethodName}{input.semantic}[{maxLights}]",
                                null,
                                null
                                ));
                            shadeArguments.Append(specializedShadeMethodName);
                            shadeArguments.Append(input.semantic);
                            shadeArguments.Append("[i]");
                        }
                        else
                            shadeArguments.Append(fragmentShaderMethodTemplate.parameters.Single(x => x.semantic == input.semantic).name);
                    }
                }

                // generate shade method
                {
                    Method shadeMethodTemplate = lightShader.GetMethod(shadeMethodName);

                    // TODO: validate signature
                    Method specializedShadeMethod = (Method)shadeMethodTemplate.Clone();
                    specializedShadeMethod.name = specializedShadeMethodName;

                    // TODO: allow shade method to call other methods and import those as well
                    code.AddMethod(specializedShadeMethod);
                }

                // generate fragment shader
                {
                    specialized.defines.Single(IsMaxLightsDefine).name = maxLights.ToString();
                    specialized.defines.Single(IsShadeMethodDefine).name = specializedShadeMethodName;
                    specialized.defines.Single(IsShadeArgsDefine).name = shadeArguments.ToString();

                    StringBuilder codeBuilder = new StringBuilder();
                    specialized.WriteCode(codeBuilder);

                    CodeBlock processedCode = new CodeBlock(null, specialized.ProcessDefines(codeBuilder.ToString()));

                    Method processedMethod = processedCode.GetMethod(TEMPLATE_METHOD_NAME);
                    processedMethod.name = specializedFragmentShaderMethodName;
                    code.AddMethod(processedMethod);
                }
            }

            code.defines.Single(IsIntrinsicsDefine).name += $", {specializedFragmentShaderMethodName}";
        }
    }
}
