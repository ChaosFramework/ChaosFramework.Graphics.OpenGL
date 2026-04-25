using ChaosFramework.Collections;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.InteropServices;
using System.Text;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    public partial class Shader : CodeBlock
    {
        public readonly Graphics graphics;
        public readonly int defaultShaderModel;

        SysCol.Dictionary<string, SemanticHandle> semanticHandles = new SysCol.Dictionary<string, SemanticHandle>();
        SysCol.Dictionary<string, ShaderVariable> variableHandles = new SysCol.Dictionary<string, ShaderVariable>();
        SysCol.Dictionary<ShaderVariable, object> fxValues = new SysCol.Dictionary<ShaderVariable, object>();
        SysCol.Dictionary<string, RuntimePass> runtimePasses = new SysCol.Dictionary<string, RuntimePass>();
        LinkedList<RuntimePass> activePassStack = new LinkedList<RuntimePass>();

        public Shader(Graphics graphics, CodeBlock code, int defaultShaderModel)
            : this(graphics, code.importSource, defaultShaderModel)
        {
            code.CloneTo(this);
        }

        public Shader(Graphics graphics, ShaderCodeContainer importSource, int defaultShaderModel)
            : base(importSource ?? Shaders.code)
        {
            this.graphics = graphics;
            this.defaultShaderModel = defaultShaderModel;
        }

        public Shader(Graphics graphics, ShaderCodeContainer importContext, string code, int defaultShaderModel)
            : base(importContext ?? Shaders.code, code)
        {
            this.graphics = graphics;
            this.defaultShaderModel = defaultShaderModel;
        }

        public Shader(Graphics graphics, ShaderCodeContainer importContext, byte[] codeAsBytes, int defaultShaderModel)
            : this(graphics, importContext, Encoding.ASCII.GetString(codeAsBytes), defaultShaderModel)
        { }

        public override void WriteCode(StringBuilder str, bool untransformed)
        {
            AssertAlive();
            base.WriteCode(str);
            str.Append("\n----------------------\n//Passes\n");
            foreach (Pass p in passes)
                p.WriteCode(str);
        }

        public override ShaderComponent Clone()
        {
            Shader clone = new Shader(graphics, importSource, defaultShaderModel);
            CloneTo(clone);
            if (runtimePasses.Count > 0)
                clone.Compile();

            return clone;
        }

        public SemanticHandle GetParameterBySemantic(string semantic)
        {
            SemanticHandle handle;
            if (!semanticHandles.TryGetValue(semantic, out handle))
                semanticHandles[semantic] = handle = new SemanticHandle(this, semantic);

            return handle;
        }

        public SemanticMapping BeginPass(string passName)
        {
            AssertAlive();
            AssertCompiled();

            Graphics.ThrowErrors();
            RuntimePass runtimePass;
            if (!runtimePasses.TryGetValue(passName, out runtimePass))
                throw new ArgumentException($"No pass called {passName} found.");

            if (activePassStack.length > short.MaxValue)
                throw new InvalidOperationException("Maximum pass stack count exceeded.");

            CommitChanges();
            activePassStack.Insert(0, runtimePass);
            GL.UseProgram(runtimePass.programHandle);
            Graphics.ThrowErrors();
            foreach (Tuple<Action, Action> state in runtimePass.stateActions)
            {
                state.Item1();
                Graphics.ThrowErrors();
            }

            return runtimePass.semanticMapping;
        }

        public void EndPass()
        {
            AssertAlive();
            AssertCompiled();

            if (graphics.needsFlush)
                GL.Flush();

            Graphics.ThrowErrors();
            if (activePassStack.length <= 0)
                throw new InvalidOperationException("No Passes to end.");

            RuntimePass runtimePass = activePassStack.RemoveAt(0);
            foreach (Tuple<Action, Action> state in runtimePass.stateActions)
                state.Item2();
        }

        void FreeResources()
        {
            if (uniformBuffer >= 0)
            {
                GL.DeleteBuffer(uniformBuffer);
                Graphics.ThrowErrors();
            }

            foreach (SysCol.KeyValuePair<string, RuntimePass> pass in runtimePasses)
            {
                GL.DeleteProgram(pass.Value.programHandle);
                Graphics.ThrowErrors();
            }
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            Marshal.FreeHGlobal(uniformValueBuffer);
            graphics.dispatcher.Dispatch(FreeResources);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        void AssertCompiled()
            => System.Diagnostics.Debug.Assert(runtimePasses.Count > 0, "Shader was not compiled.");
    }
}
