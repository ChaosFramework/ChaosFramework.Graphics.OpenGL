using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.InteropServices;

namespace ChaosFramework.Graphics.OpenGl.Instancing
{
    public abstract class InstancerBase : Disposable
    {
        protected const int VECTOR_SIZE = 16;

        public int numInstances { get; private set; }

        protected readonly int stride;

        readonly bool useSharedBuffer;
        readonly int inherentSemanticCount;
        readonly string[] streamInputSemantics;
        readonly int expectedInstances;
        readonly LinkedList<Vector4f> internalDataForSharedBuffer;
        readonly Graphics graphics;

        int privateBuffer;
        IntPtr unmanagedData;
        int overflow = 0;
        int sizeInBytes;

        public InstancerBase(Graphics graphics, string[] streamInputSemantics, int maxExpectedInstances, bool useSharedBuffer = true)
            : this(graphics, streamInputSemantics, maxExpectedInstances, useSharedBuffer, 0)
        { }

        protected InstancerBase(
            Graphics graphics,
            string[] streamInputSemantics,
            int expectedInstances,
            bool useSharedBuffer,
            int inherentSemanticCount)
        {
            this.graphics = graphics;
            this.streamInputSemantics = streamInputSemantics == null ? new string[0] : streamInputSemantics;
            this.useSharedBuffer = useSharedBuffer;
            this.expectedInstances = expectedInstances;
            this.inherentSemanticCount = inherentSemanticCount;
            stride = (this.inherentSemanticCount + this.streamInputSemantics.Length) * VECTOR_SIZE;

            if (useSharedBuffer)
                internalDataForSharedBuffer = new LinkedList<Vector4f>();
            else
                unmanagedData = Marshal.AllocHGlobal(expectedInstances * stride);

            graphics.dispatcher.RunAndAwait(Build);
        }

        void Build()
        {
            sizeInBytes = stride * expectedInstances;
            if (!useSharedBuffer)
            {
                privateBuffer = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, privateBuffer);
                Graphics.ThrowErrors();
                GL.BufferData(BufferTarget.ArrayBuffer, sizeInBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                Graphics.ThrowErrors();
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                Graphics.ThrowErrors();
            }
            else
                graphics.FitInstancingBuffer(sizeInBytes);
        }

        internal int GetBuffer()
        {
            if (useSharedBuffer)
            {
                UpdateSharedBuffer();
                return graphics.sharedInstancingBuffer;
            }
            else
                return privateBuffer;
        }

        unsafe void ResizePrivateBufferIfNeeded()
        {
            if (numInstances >= expectedInstances + overflow)
            {
                int oldSize = stride * (expectedInstances + overflow);
                if (overflow == 0)
                    overflow = System.Math.Max(1, expectedInstances / 8);
                else
                    overflow *= 2;

                IntPtr newData = Marshal.AllocHGlobal(stride * (expectedInstances + overflow));
                NativeMemory.Copy(
                    (void*)unmanagedData,
                    (void*)newData,
                    (UIntPtr)oldSize
                    );
                Marshal.FreeHGlobal(unmanagedData);
                unmanagedData = newData;
            }
        }

        protected void SetCustomValues(Vector4f[] customValues)
        {
            System.Diagnostics.Debug.Assert(customValues != null, "customValues must not be null");
            System.Diagnostics.Debug.Assert(
                customValues.Length == inherentSemanticCount + streamInputSemantics.Length,
                "The number of custom values for this instance does not match the number of allocated registers."
                );

            if (useSharedBuffer)
                internalDataForSharedBuffer.Add(customValues);
            else
            {
                ResizePrivateBufferIfNeeded();
                IntPtr offset = unmanagedData + stride * numInstances;
                for (int i = 0; i < customValues.Length; ++i)
                    Marshal.StructureToPtr(customValues[i], offset + VECTOR_SIZE * i, false);
            }
            numInstances++;
        }

        public void Reset()
        {
            numInstances = 0;
            internalDataForSharedBuffer?.Clear();
        }

        public void UpdateBuffer()
        {
            if (useSharedBuffer)
                return;

            GL.BindBuffer(BufferTarget.ArrayBuffer, privateBuffer);
            Graphics.ThrowErrors();
            if (sizeInBytes < stride * (expectedInstances + overflow))
            {
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    sizeInBytes = stride * (expectedInstances + overflow),
                    unmanagedData,
                    BufferUsageHint.DynamicDraw
                    );
                Graphics.ThrowErrors();
            }
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, stride * numInstances, unmanagedData);
            Graphics.ThrowErrors();
        }

        internal unsafe void UpdateSharedBuffer()
        {
            if (numInstances == 0)
                return;

            if (expectedInstances < numInstances)
                graphics.FitInstancingBuffer(stride * numInstances);

            Vector4f[] managedArray = internalDataForSharedBuffer.ToArray();
            GL.BindBuffer(BufferTarget.ArrayBuffer, graphics.sharedInstancingBuffer);
            Graphics.ThrowErrors();
            fixed (Vector4f* pinned = managedArray)
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, stride * numInstances, (IntPtr)pinned);
            Graphics.ThrowErrors();
        }

        public virtual void Bind(Shader.SemanticMapping mapping)
        {
            if (useSharedBuffer)
                UpdateSharedBuffer();
            else
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, privateBuffer);
                Graphics.ThrowErrors();
            }

            int targetBaseStream;
            for (int i = 0; i < streamInputSemantics.Length; i++)
                if (mapping.mapping.TryGetValue(streamInputSemantics[i], out targetBaseStream))
                {
                    int offset = (inherentSemanticCount + i) * VECTOR_SIZE;
                    GL.VertexAttribPointer(targetBaseStream, 4, VertexAttribPointerType.Float, false, stride, offset);
                    Graphics.ThrowErrors();
                    GL.EnableVertexAttribArray(targetBaseStream);
                    Graphics.ThrowErrors();
                    GL.VertexAttribDivisor(targetBaseStream, 1);
                    Graphics.ThrowErrors();
                }
        }

        public virtual void Unbind(ChaosShader.Shader.SemanticMapping mapping)
        {
            int targetBaseStream;
            for (int s = 0; s < streamInputSemantics.Length; s++)
                if (mapping.mapping.TryGetValue(streamInputSemantics[s], out targetBaseStream))
                    for (int i = 0; i < streamInputSemantics.Length; i++)
                    {
                        GL.DisableVertexAttribArray(targetBaseStream);
                        Graphics.ThrowErrors();
                        GL.VertexAttribDivisor(targetBaseStream, 0);
                        Graphics.ThrowErrors();
                    }
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            graphics.dispatcher.Dispatch(FreeResources);
        }

        void FreeResources()
        {
            Marshal.FreeHGlobal(unmanagedData);
            GL.DeleteBuffer(privateBuffer);
            Graphics.ThrowErrors();
        }
    }

    public class CustomInstancer : InstancerBase
    {
        public CustomInstancer(Graphics graphics, string[] registers, int expectedInstances, bool useSharedBuffer)
            : base(graphics, registers, expectedInstances, useSharedBuffer)
        { }

        public void AddInstance(params Vector4f[] streamInputValues)
            => SetCustomValues(streamInputValues);
    }

    public class MatrixInstancer : InstancerBase
    {
        const string INSTANCE_TRANSFORM_SEMANTIC = "INSTANCE_TRANSFORM";

        public MatrixInstancer(Graphics graphics, string[] streamInputSemantics, int expectedInstances, bool useSharedBuffer = true)
            : base(graphics, streamInputSemantics, expectedInstances, useSharedBuffer, 4)
        { }

        public void AddInstance(Matrix transform, params Vector4f[] customValues)
        {
            transform = Matrix.Transpose(transform);

            Vector4f[] allValues = new Vector4f[customValues.Length + 4];
            allValues[0] = transform.row0;
            allValues[1] = transform.row1;
            allValues[2] = transform.row2;
            allValues[3] = transform.row3;
            Array.Copy(customValues, 0, allValues, 4, customValues.Length);

            SetCustomValues(allValues);
        }

        public override void Bind(Shader.SemanticMapping mapping)
        {
            base.Bind(mapping);

            int targetBaseStream;
            if (mapping.mapping.TryGetValue(INSTANCE_TRANSFORM_SEMANTIC, out targetBaseStream))
                for (int i = 0; i < 4; i++)
                {
                    GL.VertexAttribPointer(targetBaseStream + i, 4, VertexAttribPointerType.Float, false, stride, i * VECTOR_SIZE);
                    Graphics.ThrowErrors();
                    GL.EnableVertexAttribArray(targetBaseStream + i);
                    Graphics.ThrowErrors();
                    GL.VertexAttribDivisor(targetBaseStream + i, 1);
                    Graphics.ThrowErrors();
                }
        }

        public override void Unbind(Shader.SemanticMapping mapping)
        {
            base.Unbind(mapping);

            int targetBaseStream;
            if (mapping.mapping.TryGetValue(INSTANCE_TRANSFORM_SEMANTIC, out targetBaseStream))
                for (int i = 0; i < 4; i++)
                {
                    GL.VertexAttribDivisor(targetBaseStream + i, 0);
                    Graphics.ThrowErrors();
                    GL.DisableVertexAttribArray(targetBaseStream + i);
                    Graphics.ThrowErrors();
                }
        }
    }
}
