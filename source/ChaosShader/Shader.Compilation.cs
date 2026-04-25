using ChaosFramework.Collections;
using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.InteropServices;
using System.Text;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    using CompilationErrors;

    partial class Shader
    {
        public enum VERSION_REQ : int
        {
            PROFILE_ES = 300,
            PROFILE_CORE = 330,
            VULKAN_RULES = 420,
            VS_OUT_LOCATION = 410,
            EXPLICIT_UNIFORM_BINDING = 420,
            EXPLICIT_TEXTURE_IMAGE_BINDING = 420,
        }

        static int localBlockIndex = 0;

        string GenCodeBase(int version)
        {
            StringBuilder str = new StringBuilder($"#version {version}");
            if (version >= (int)VERSION_REQ.PROFILE_CORE)
                str.Append(" core");
            else if (version >= (int)VERSION_REQ.PROFILE_ES)
                str.Append(" es");
            str.AppendLine();
            str.AppendLine();

            foreach (Variable v in variables)
                if (v.modifier.value != null)
                    v.WriteCode(str, false);

            str.Append("\nlayout(std140");
            if (version >= (int)VERSION_REQ.EXPLICIT_UNIFORM_BINDING)
                str.Append(", binding = 0");
            str.AppendLine(") uniform " + (blockName = "localUniform" + localBlockIndex++) + " {");
            foreach (Variable v in variables)
                if (v.modifier.value == null && typeSize.ContainsKey(v.type))
                {
                    str.Append('\t');
                    v.WriteCode(str, false);
                }
            str.AppendLine("};\n");

            int bindingCounter = 1;
            foreach (Variable v in variables)
                if (v.modifier.value == null && !typeSize.ContainsKey(v.type))
                {
                    if (version >= (int)VERSION_REQ.EXPLICIT_TEXTURE_IMAGE_BINDING)
                        str.Append($"layout(binding = {bindingCounter++}) ");
                    str.Append("uniform ");
                    v.WriteCode(str, false);
                }

            return ProcessDefines(str.ToString());
        }

        public TranspiledPass[] BuildPasses()
        {
            AssertAlive();

            SysCol.Dictionary<int, string> codeBases = new SysCol.Dictionary<int, string>();

            TranspiledPass[] transpiled = new TranspiledPass[passes.length];
            int passIndex = 0;
            foreach (Pass pass in passes)
            {
                int version = pass.shaderModel ?? defaultShaderModel;

                string codeBase;
                if (!codeBases.TryGetValue(version, out codeBase))
                    codeBases[version] = codeBase = GenCodeBase(version);

                pass.SetShaders(ProcessDefines(pass.vertexShader), ProcessDefines(pass.fragmentShader));
                SysCol.Dictionary<string, int> vsToFsLayoutLocations;
                string vertexShaderCode = $"{codeBase}\n{BuildVertexShaderMain(pass, out vsToFsLayoutLocations)}";
                string fragmentShaderCode = $"{codeBase}\n{BuildFragmentShaderMain(pass, vsToFsLayoutLocations)}";

                transpiled[passIndex++] = new TranspiledPass(this, pass, vertexShaderCode, fragmentShaderCode);
            }

            return transpiled;
        }

        public unsafe void CompilePass(TranspiledPass p)
        {
            AssertAlive();

            LinkedList<Tuple<Action, Action>> renderStates = new LinkedList<Tuple<Action, Action>>();
            foreach (RenderState renderState in p.pass.renderStates)
            {
                Action setter, unsetter;
                renderState.GetActions(graphics, out setter, out unsetter);
                renderStates.Add(new Tuple<Action, Action>(setter, unsetter));
            }

            graphics.dispatcher.RunAndAwait(() =>
            {
                Graphics.ThrowErrors();

                StringBuilder allErrors = new StringBuilder();
                using (GlShaderScope vs = new GlShaderScope(allErrors, ShaderType.VertexShader, p.vertexShaderCode).AssertSuccess(p))
                using (GlShaderScope fs = new GlShaderScope(allErrors, ShaderType.FragmentShader, p.fragmentShaderCode).AssertSuccess(p))
                {
                    int program = GL.CreateProgram();
                    runtimePasses[p.pass.name] = new RuntimePass
                    {
                        programHandle = program,
                        stateActions = renderStates,
                        semanticMapping = p.pass.semanticMapping
                    };

                    GL.AttachShader(program, vs.shader);
                    Graphics.ThrowErrors();
                    GL.AttachShader(program, fs.shader);
                    Graphics.ThrowErrors();
                    GL.LinkProgram(program);
                    Graphics.ThrowErrors();

                    int linkStatus;
                    GL.GetProgram(program, GetProgramParameterName.LinkStatus, out linkStatus);
                    Graphics.ThrowErrors();
                    string error = (linkStatus == 0 ? "Not Linked;" : "") + GL.GetProgramInfoLog(program);
#if IGNORE_SHADER_WARNINGS
                    if (linkStatus == 0)
#endif
                        if (error != "")
                            allErrors.Append($"Linker error in pass {p.pass.name}:\n\n{error}");

                    AssignUniformBindings(program);
                }

                if (allErrors.Length > 0)
                    throw new CompilationError($"Could not compile shader:\n{allErrors}");
            });
        }

        void AssignUniformBindings(int program)
        {
            int blockIndex = GL.GetUniformBlockIndex(program, blockName);
            if (blockIndex != -1)
            {
                GL.UniformBlockBinding(program, blockIndex, 0);
                Graphics.ThrowErrors();

                int uniformIndexCount;
                int blockDataSize;
                GL.GetActiveUniformBlock(
                    program,
                    blockIndex,
                    ActiveUniformBlockParameter.UniformBlockActiveUniforms,
                    out uniformIndexCount
                    );
                Graphics.ThrowErrors();

                GL.GetActiveUniformBlock(
                    program,
                    blockIndex,
                    ActiveUniformBlockParameter.UniformBlockDataSize,
                    out blockDataSize
                    );
                Graphics.ThrowErrors();

                uniformValueBuffer = Marshal.AllocHGlobal(totalUniformBufferSize = blockDataSize);
                int[] uniformIndices = new int[uniformIndexCount];
                GL.GetActiveUniformBlock(
                    program,
                    blockIndex,
                    ActiveUniformBlockParameter.UniformBlockActiveUniformIndices,
                    uniformIndices
                    );
                Graphics.ThrowErrors();

                for (int i = 0; i < uniformIndices.Length; i++)
                {
                    int offset, size, arrayStride;
                    int uniformIndex = uniformIndices[i];
                    string name = GL.GetActiveUniformName(program, uniformIndex);
                    GL.GetActiveUniforms(program, 1, ref uniformIndex, ActiveUniformParameter.UniformOffset, out offset);
                    Graphics.ThrowErrors();
                    if (offset == -1)
                        throw new CompilationError($"Could not retrieve Uniform Offset for {name}");

                    GL.GetActiveUniforms(program, 1, ref uniformIndex, ActiveUniformParameter.UniformSize, out size);
                    Graphics.ThrowErrors();
                    GL.GetActiveUniforms(program, 1, ref uniformIndex, ActiveUniformParameter.UniformArrayStride, out arrayStride);
                    Graphics.ThrowErrors();
                    UniformBufferVariable variable = new UniformBufferVariable((IntPtr)offset, size, arrayStride);

                    if (name.EndsWith("[0]"))
                        name = name.Substring(0, name.Length - 3);

                    variableHandles[name] = variable;
                }
            }
        }

        public void PrepareCompilation()
        {
            AssertAlive();
            ResolveImports();
            semanticToField.Clear();
            foreach (Variable var in variables)
            {
                if (var.semantic != null)
                {
                    LinkedList<string> vars;
                    if (!semanticToField.TryGetValue(var.semantic, out vars))
                        semanticToField[var.semantic] = vars = new LinkedList<string>();
                    semanticToField[var.semantic].Add(var.name);
                }
                var.semantic = null;
            }
        }

        public void Compile()
        {
            AssertAlive();
            AssertNotCompiled();

            PrepareCompilation();
            TranspiledPass[] transpiledPasses = BuildPasses();
            BuildResources(transpiledPasses);
        }

        void BuildResources(TranspiledPass[] transpiledPasses)
        {
            graphics.dispatcher.RunAndAwait(() =>
            {
                Graphics.ThrowErrors();
                foreach (SysCol.KeyValuePair<string, RuntimePass> handle in runtimePasses)
                {
                    GL.DeleteProgram(handle.Value.programHandle);
                    Graphics.ThrowErrors();
                }
            });

            runtimePasses.Clear();
            foreach (TranspiledPass pass in transpiledPasses)
                CompilePass(pass);

            BindUniforms();
        }

        void BindUniforms()
        {
            graphics.dispatcher.RunAndAwait(() =>
            {
                foreach (Variable variable in variables)
                    if (variable.type.StartsWith("sampler"))
                    {
                        foreach (SysCol.KeyValuePair<string, RuntimePass> passHandle in runtimePasses)
                        {
                            GL.UseProgram(passHandle.Value.programHandle);
                            Graphics.ThrowErrors();
                            int location = GL.GetUniformLocation(passHandle.Value.programHandle, variable.name);
                            GL.Uniform1(location, highestTextureUnit);
                            Graphics.ThrowErrors();
                        }

                        if (variableHandles.ContainsKey(variable.name))
                            throw new CompilationError($"Could not compile shader:\nMultiple definitions of {variable.name}.");

                        variableHandles[variable.name] = new SamplerVariable(highestTextureUnit++);
                    }

                if (totalUniformBufferSize > 0)
                {
                    uniformBuffer = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.UniformBuffer, uniformBuffer);
                    Graphics.ThrowErrors();
                    GL.BufferData(BufferTarget.UniformBuffer, totalUniformBufferSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
                    Graphics.ThrowErrors();
                    GL.BindBufferRange(BufferRangeTarget.UniformBuffer, 0, uniformBuffer, IntPtr.Zero, (IntPtr)totalUniformBufferSize);
                    Graphics.ThrowErrors();
                    GL.BindBuffer(BufferTarget.UniformBuffer, 0);
                    Graphics.ThrowErrors();
                }
            });

            foreach (Variable variable in variables)
                if (variable.value != null && variable.modifier.value != "const")
                    SetValue(variable.name, variable.GetInitialValue());

            fxValues.Clear();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        void AssertNotCompiled()
            => System.Diagnostics.Debug.Assert(runtimePasses.Count == 0, "Shader was already compiled.");
    }
}
