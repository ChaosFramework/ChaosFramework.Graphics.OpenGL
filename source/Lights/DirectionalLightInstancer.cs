using ChaosFramework.Math.Vectors;
using ChaosFramework.Math;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using System;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    using AssetContainers;

    public sealed class DirectionalLightInstancer : LightInstancer<DirectionalLight>
    {
        static readonly int SZ_VEC3 = Marshal.SizeOf<Vector3f>();
        static readonly int SZ_VEC4 = Marshal.SizeOf<Vector4f>();
        static readonly int STRIDE = SZ_VEC3 + SZ_VEC4 + SZ_VEC4;

        public ShaderContainer.Entry shader;

        readonly Graphics graphics;
        int maxInstances;

        IntPtr unmanagedData;
        int numInstances;

        int vao;
        int vbo;

        int? newGlBufferSize = null;

        public DirectionalLightInstancer(Graphics graphics, int expectedInstances)
        {
            this.graphics = graphics;
            this.maxInstances = expectedInstances;

            shader = graphics.shaders.Load($"ChaosGraphics.{nameof(DirectionalLight)}", this);

            graphics.dispatcher.RunAndAwait(CreateGlResources);
            CreateBuffers();
        }

        void CreateGlResources()
        {
            vao = GL.GenVertexArray();
            Graphics.ThrowErrors();

            GL.BindVertexArray(vao);
            Graphics.ThrowErrors();

            vbo = GL.GenBuffer();
            Graphics.ThrowErrors();

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            Graphics.ThrowErrors();

            System.Collections.Generic.Dictionary<string, int> mapping
                = shader.content.passes.first.semanticMapping.mapping;
            int dirAttr = mapping["DIRECTION"];
            GL.VertexAttribPointer(dirAttr, 3, VertexAttribPointerType.Float, false, STRIDE, 0);
            Graphics.ThrowErrors();
            GL.EnableVertexAttribArray(dirAttr);
            Graphics.ThrowErrors();
            GL.VertexAttribDivisor(dirAttr, 1);
            Graphics.ThrowErrors();

            int colAttr = mapping["LIGHTCOLOR"];
            GL.VertexAttribPointer(colAttr, 4, VertexAttribPointerType.Float, false, STRIDE, SZ_VEC3);
            Graphics.ThrowErrors();
            GL.EnableVertexAttribArray(colAttr);
            Graphics.ThrowErrors();
            GL.VertexAttribDivisor(colAttr, 1);
            Graphics.ThrowErrors();

            int ambientAttr = mapping["AMBIENTCOLOR"];
            GL.VertexAttribPointer(ambientAttr, 4, VertexAttribPointerType.Float, false, STRIDE, SZ_VEC3 + SZ_VEC4);
            Graphics.ThrowErrors();
            GL.EnableVertexAttribArray(ambientAttr);
            Graphics.ThrowErrors();
            GL.VertexAttribDivisor(ambientAttr, 1);
            Graphics.ThrowErrors();
        }

        void CreateBuffers()
        {
            System.Diagnostics.Debug.Assert(unmanagedData == default(IntPtr));
            newGlBufferSize = STRIDE * maxInstances;
            unmanagedData = Marshal.AllocHGlobal(newGlBufferSize.Value);
        }

        unsafe void Enlarge()
        {
            System.Diagnostics.Debug.Assert(unmanagedData != default(IntPtr));

            int oldSizeInBytes = STRIDE * maxInstances;
            newGlBufferSize = oldSizeInBytes << 1;

            IntPtr newUnmanagedData = Marshal.AllocHGlobal(newGlBufferSize.Value);
            NativeMemory.Copy(
                (void*)unmanagedData,
                (void*)newUnmanagedData,
                (UIntPtr)oldSizeInBytes
                );
            maxInstances <<= 1;

            Marshal.FreeHGlobal(unmanagedData);
            unmanagedData = newUnmanagedData;
        }

        public override void Reset()
        {
            numInstances = 0;
        }

        protected override bool Add(DeferredShader target, DirectionalLight l)
        {
            if (numInstances == maxInstances - 1)
                Enlarge();

            int offset = STRIDE * numInstances++;
            Marshal.StructureToPtr(l.direction, IntPtr.Add(unmanagedData, offset), false);
            Marshal.StructureToPtr(l.premultipliedColor.ToVec(), IntPtr.Add(unmanagedData, offset + SZ_VEC3), false);
            Marshal.StructureToPtr(l.premultipliedAmbientVec, IntPtr.Add(unmanagedData, offset + SZ_VEC3 + SZ_VEC4), false);

            return true;
        }

        public override void Render(DeferredShader target)
        {
            GL.BindVertexArray(vao);
            Graphics.ThrowErrors();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            Graphics.ThrowErrors();
            if (newGlBufferSize.HasValue)
            {
                GL.BufferData(BufferTarget.ArrayBuffer, newGlBufferSize.Value, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                Graphics.ThrowErrors();
                newGlBufferSize = null;
            }
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, STRIDE * numInstances, unmanagedData);
            Graphics.ThrowErrors();

            target.SetValues(shader);
            target.view.SetValues(shader, Matrix.IDENTITY);
            shader.BeginPass("Simple");
            GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 3, numInstances);
            Graphics.ThrowErrors();
            shader.EndPass();
        }

        void DeleteGlResources()
        {
            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vbo);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            graphics.dispatcher.RunAndAwait(DeleteGlResources);
        }
    }
}
