using ChaosFramework.Graphics.Colors;
using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Shapes;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.Model
{
    public class MeshBuffers : Disposable
    {
        static readonly SysCol.Dictionary<Type, VertexAttribPointerType> vertexPointerTypeMapping
            = new SysCol.Dictionary<Type, VertexAttribPointerType>();

        static readonly SysCol.Dictionary<VertexAttribPointerType, int> attribSize
            = new SysCol.Dictionary<VertexAttribPointerType, int>();

        static MeshBuffers()
        {
            vertexPointerTypeMapping[typeof(float)] = VertexAttribPointerType.Float;
            vertexPointerTypeMapping[typeof(Vector2f)] = VertexAttribPointerType.Float;
            vertexPointerTypeMapping[typeof(Vector3f)] = VertexAttribPointerType.Float;
            vertexPointerTypeMapping[typeof(Vector4f)] = VertexAttribPointerType.Float;
            vertexPointerTypeMapping[typeof(Matrix)] = VertexAttribPointerType.Float;
            vertexPointerTypeMapping[typeof(int)] = VertexAttribPointerType.Int;
            vertexPointerTypeMapping[typeof(Vector2i)] = VertexAttribPointerType.Int;
            vertexPointerTypeMapping[typeof(Vector3i)] = VertexAttribPointerType.Int;
            vertexPointerTypeMapping[typeof(Vector4i)] = VertexAttribPointerType.Int;

            attribSize[VertexAttribPointerType.Float] = 4;
            attribSize[VertexAttribPointerType.Int] = 4;
        }

        static Vector2f[] FlipTexCoords(Vector2f[] input)
        {
            Vector2f[] output = new Vector2f[input.Length];
            for (int i = 0; i < output.Length; i++)
                output[i] = new Vector2f(input[i].x, 1 - input[i].y);

            return output;
        }

        protected SysCol.Dictionary<string, Action<int>> buffersBySemantic = null;
        readonly SysCol.Dictionary<ChaosShader.Shader.SemanticMapping, int> VAOs = new SysCol.Dictionary<ChaosShader.Shader.SemanticMapping, int>();

        protected readonly Dispatcher dispatcher;

        protected int indexBuffer = -1;
        protected int positionBuffer = -1, normalBuffer = -1, tangentBuffer = -1;
        protected int[] texCoordBuffer;
        protected int[] customStreamBuffers;

        public MeshBuffers(Dispatcher dispatcher, MeshData data)
        {
            this.dispatcher = dispatcher;
            dispatcher.RunAndAwait(() => Construct(data));
        }

        protected void Construct(MeshData data)
        {
            GenerateBuffers(data);
            Apply(data);
        }

        internal void Apply(MeshData data)
        {
            dispatcher.RunAndAwait(() =>
            {
                WriteIndexDataToBuffers(data);
                WriteVertexDataToBuffers(data);
            });
        }

        protected virtual void GenerateBuffers(MeshData data)
        {
            buffersBySemantic = new SysCol.Dictionary<string, Action<int>>();

            indexBuffer = GL.GenBuffer();
            if (data.pos != null)
            {
                positionBuffer = GL.GenBuffer();
                buffersBySemantic["POSITION0"] = buffersBySemantic["POSITION"] = _ =>
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, positionBuffer);
                    Graphics.ThrowErrors();
                    GL.VertexAttribPointer(_, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), IntPtr.Zero);
                    Graphics.ThrowErrors();
                };
            }

            if (data.nor != null)
            {
                normalBuffer = GL.GenBuffer();
                buffersBySemantic["NORMAL0"] = buffersBySemantic["NORMAL"] = _ =>
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, normalBuffer);
                    Graphics.ThrowErrors();
                    GL.VertexAttribPointer(_, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), IntPtr.Zero);
                    Graphics.ThrowErrors();
                };
            }

            if (data.tan != null)
            {
                tangentBuffer = GL.GenBuffer();
                buffersBySemantic["TANGENT0"] = buffersBySemantic["TANGENT"] = _ =>
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, tangentBuffer);
                    Graphics.ThrowErrors();
                    GL.VertexAttribPointer(_, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), IntPtr.Zero);
                    Graphics.ThrowErrors();
                };
            }

            texCoordBuffer = new int[data.numTexCoordPairs];
            GL.GenBuffers(data.numTexCoordPairs, texCoordBuffer);
            Graphics.ThrowErrors();

            for (int texCoordIndex = 0; texCoordIndex < texCoordBuffer.Length; texCoordIndex++)
            {
                int i = texCoordIndex;
                buffersBySemantic["TEXCOORD" + i] = _ =>
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordBuffer[i]);
                    Graphics.ThrowErrors();
                    GL.VertexAttribPointer(_, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), IntPtr.Zero);
                    Graphics.ThrowErrors();
                };
                if (i == 0)
                    buffersBySemantic["TEXCOORD"] = buffersBySemantic["TEXCOORD0"];
            };

            customStreamBuffers = new int[data.customData.length];
            GL.GenBuffers(data.customData.length, customStreamBuffers);
            Graphics.ThrowErrors();

            for (int iterator = 0; iterator < data.customData.length; iterator++)
            {
                int i = iterator;
                MeshData.CustomStream stream = data.customData[i];
                Type t = stream.ElementType();
                VertexAttribPointerType registerType = vertexPointerTypeMapping[t];
                int dataTypeSize = Marshal.SizeOf(t);
                int numComponents = dataTypeSize / attribSize[registerType];
                Action<int> bind = va =>
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, customStreamBuffers[i]);
                    Graphics.ThrowErrors();
                    GL.VertexAttribPointer(va, numComponents, registerType, false, dataTypeSize, IntPtr.Zero);
                    Graphics.ThrowErrors();
                };
                foreach (string semantic in stream.semantics)
                    buffersBySemantic[semantic] = bind;
            }
            Graphics.ThrowErrors();
        }

        internal int GetOrCreateVAO(ChaosShader.Shader.SemanticMapping semantics)
        {
            Graphics.ThrowErrors();
            if (VAOs.TryGetValue(semantics, out int VAO))
                return VAO;

            int vertexArrayObject = GL.GenVertexArray();
            VAOs[semantics] = vertexArrayObject;
            GL.BindVertexArray(vertexArrayObject);
            Graphics.ThrowErrors();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            Graphics.ThrowErrors();
            foreach (SysCol.KeyValuePair<string, int> mapping in semantics.mapping)
                if (buffersBySemantic.TryGetValue(mapping.Key, out Action<int> bufferBind))
                {
                    bufferBind(mapping.Value);
                    GL.EnableVertexAttribArray(mapping.Value);
                    Graphics.ThrowErrors();
                }

            GL.BindVertexArray(0);
            Graphics.ThrowErrors();
            return vertexArrayObject;
        }


        public void WriteIndexDataToBuffers(MeshData data)
        {
            GL.BindVertexArray(0);
            Graphics.ThrowErrors();
            if (indexBuffer == -1)
                throw new InvalidOperationException("Mesh originally had no indices. Don't change that.");
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            Graphics.ThrowErrors();
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                data.ind.length * sizeof(uint),
                (uint[])data.ind.GetUnsafeUnderlyingArray(),
                BufferUsageHint.StaticDraw
                );
            Graphics.ThrowErrors();
        }

        public virtual void WriteVertexDataToBuffers(MeshData data)
        {
            GL.BindVertexArray(0);
            Graphics.ThrowErrors();
            if (data.pos != null)
            {
                if (positionBuffer == -1)
                    throw new InvalidOperationException("Mesh originally had no position. Don't change that.");
                GL.BindBuffer(BufferTarget.ArrayBuffer, positionBuffer);
                Graphics.ThrowErrors();
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    data.pos.length * sizeof(float) * 3,
                    (Vector3f[])data.pos.GetUnsafeUnderlyingArray(),
                    BufferUsageHint.StaticDraw
                    );
                Graphics.ThrowErrors();
            }
            if (data.nor != null)
            {
                if (normalBuffer == -1)
                    throw new InvalidOperationException("Mesh originally had no normals. Don't change that.");
                GL.BindBuffer(BufferTarget.ArrayBuffer, normalBuffer);
                Graphics.ThrowErrors();
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    data.nor.length * sizeof(float) * 3,
                    (Vector3f[])data.nor.GetUnsafeUnderlyingArray(),
                    BufferUsageHint.StaticDraw
                    );
                Graphics.ThrowErrors();
            }
            if (data.tan != null)
            {
                if (tangentBuffer == -1)
                    throw new InvalidOperationException("Mesh originally had no tangents. Don't change that.");
                GL.BindBuffer(BufferTarget.ArrayBuffer, tangentBuffer);
                Graphics.ThrowErrors();
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    data.tan.length * sizeof(float) * 4,
                    (Vector4f[])data.tan.GetUnsafeUnderlyingArray(),
                    BufferUsageHint.StaticDraw
                    );
                Graphics.ThrowErrors();
            }
            if (data.tex != null)
                if (texCoordBuffer.Length != data.tex.length)
                    throw new InvalidOperationException($"Mesh originally had {data.tex.length} texCoord pairs. Don't change that.");
                else
                    for (int i = 0; i < data.tex.length; i++)
                    {
                        GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordBuffer[i]);
                        Graphics.ThrowErrors();
                        GL.BufferData(
                            BufferTarget.ArrayBuffer,
                            data.tex[i].length * sizeof(float) * 2,
                            FlipTexCoords(data.tex[i].GetUnsafeUnderlyingArray()),
                            BufferUsageHint.StaticDraw
                            );
                        Graphics.ThrowErrors();
                    }
            if (data.customData != null)
                if (customStreamBuffers.Length != data.customData.length)
                    throw new InvalidOperationException($"Mesh originally had {data.customData.length} custom buffers. Don't change that.");
                else
                    for (int i = 0; i < data.customData.length; i++)
                    {
                        GL.BindBuffer(BufferTarget.ArrayBuffer, customStreamBuffers[i]);
                        Graphics.ThrowErrors();
                        Array buffer = data.customData[i].GetElements();
                        switch (data.customData[i].ElementType().Name)
                        {
                            //TODO: find a better way to do this
                            case nameof(Single): SetGLBufferData<float>(buffer, i); break;
                            case nameof(Vector2f): SetGLBufferData<Vector2f>(buffer, i); break;
                            case nameof(Vector3f): SetGLBufferData<Vector3f>(buffer, i); break;
                            case nameof(Vector4f): SetGLBufferData<Vector4f>(buffer, i); break;
                            case nameof(Rgba): SetGLBufferData<Rgba>(buffer, i); break;
                            default:
                                throw new NotSupportedException(buffer.GetType().GetElementType().FullName + " is not supported for custom streams.");
                        }
                    }
            Graphics.ThrowErrors();
        }

        void SetGLBufferData<T>(Array buffer, int i) where T : struct
        {
            GL.BufferData(BufferTarget.ArrayBuffer, buffer.Length * Marshal.SizeOf(typeof(T)), (T[])buffer, BufferUsageHint.StaticDraw);
            Graphics.ThrowErrors();
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            buffersBySemantic.Clear();
            buffersBySemantic = null;
            dispatcher.Dispatch(() =>
            {
                GL.DeleteVertexArrays(VAOs.Count, VAOs.Values.ToArray());
                Graphics.ThrowErrors();
                LinkedList<int> allBuffers = new LinkedList<int>(indexBuffer, positionBuffer, normalBuffer, tangentBuffer);
                allBuffers.Add(texCoordBuffer);
                allBuffers.Add(customStreamBuffers);
                GL.DeleteBuffers(allBuffers.length, allBuffers.ToArray());
                Graphics.ThrowErrors();
            });
        }
    }
}
