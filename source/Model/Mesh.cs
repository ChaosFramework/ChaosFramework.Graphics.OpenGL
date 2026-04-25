using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Core;
using OpenTK.Graphics.OpenGL;
using System;

namespace ChaosFramework.Graphics.OpenGl.Model
{
    using Shapes;

    public class Mesh : Disposable
    {
        readonly bool ownsBuffers;
        public readonly MeshData data;
        public readonly MeshBuffers buffers;

        public Mesh(Dispatcher dispatcher, MeshData data)
            : this(data, new MeshBuffers(dispatcher, data))
        {
            ownsBuffers = true;
        }

        public Mesh(MeshData data, MeshBuffers buffers)
        {
            this.data = data;
            this.buffers = buffers;
            buffers.Apply(data);
        }

        protected override bool needsDispose => ownsBuffers;

        public int GetOrCreateVao(ChaosShader.Shader.SemanticMapping mapping)
        {
            mapping.AddUser(this);
            return buffers.GetOrCreateVAO(mapping);
        }

        void Draw()
        {
            GL.DrawElements(PrimitiveType.Triangles, data.faceCount * 3, DrawElementsType.UnsignedInt, 0);
            Graphics.ThrowErrors();
        }

        public void Draw(ChaosShader.Shader shader, string pass)
        {
            GL.BindVertexArray(GetOrCreateVao(shader.BeginPass(pass)));
            Graphics.ThrowErrors();
            Draw();
            shader.EndPass();
        }

        public void Draw(ChaosShader.Shader shader)
        {
            if (shader.passes.length != 1)
                throw new InvalidOperationException("Pass must be specified for shaders with more than one passes.");

            GL.BindVertexArray(GetOrCreateVao(shader.BeginPass(shader.passes.first.name)));
            Graphics.ThrowErrors();
            Draw();
            shader.EndPass();
        }

        public void DrawInstanced(ChaosShader.Shader shader, string pass, MatrixInstancer instanceData)
        {
            if (instanceData.numInstances == 0)
                return;

            ChaosShader.Shader.SemanticMapping semantics = shader.BeginPass(pass);
            Graphics.ThrowErrors();
            GL.BindVertexArray(GetOrCreateVao(semantics));
            Graphics.ThrowErrors();
            instanceData.Bind(semantics);
            GL.DrawElementsInstanced(
                PrimitiveType.Triangles,
                data.faceCount * 3,
                DrawElementsType.UnsignedInt,
                IntPtr.Zero,
                instanceData.numInstances
                );
            Graphics.ThrowErrors();
            shader.EndPass();
            instanceData.Unbind(semantics);
            Graphics.ThrowErrors();
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            if (ownsBuffers)
                buffers.Dispose();
        }
    }
}
