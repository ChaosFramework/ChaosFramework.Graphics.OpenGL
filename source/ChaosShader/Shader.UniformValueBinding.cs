using ChaosFramework.Collections;
using ChaosFramework.IO.Containers;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.InteropServices;
using static ChaosFramework.Math.Clamping;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    partial class Shader
    {
        internal readonly static SysCol.Dictionary<string, Type> typeByName = new SysCol.Dictionary<string, Type>();
        internal readonly static SysCol.Dictionary<string, int> typeSize = new SysCol.Dictionary<string, int>();

        static Shader()
        {
            typeByName["float"] = typeof(float);
            typeByName["vec2"] = typeof(Vector2f);
            typeByName["vec3"] = typeof(Vector3f);
            typeByName["vec4"] = typeof(Vector4f);
            typeByName["int"] = typeof(int);
            typeByName["ivec2"] = typeof(Vector2i);
            typeByName["ivec3"] = typeof(Vector3i);
            typeByName["ivec4"] = typeof(Vector4i);
            typeByName["mat4"] = typeof(Matrix);
            foreach (SysCol.KeyValuePair<string, Type> t in typeByName)
                typeSize[t.Key] = Marshal.SizeOf(t.Value);
        }

        public static int GetNumAttribs(string type)
        {
            int numBytes;
            if (!typeSize.TryGetValue(type, out numBytes))
                numBytes = 16;
            return (numBytes + 15) / 16;
        }

        int uniformBuffer = -1;
        SysCol.Dictionary<string, LinkedList<string>> semanticToField = new SysCol.Dictionary<string, LinkedList<string>>();
        int highestTextureUnit = 0;
        int totalUniformBufferSize = 0;
        string blockName;
        IntPtr uniformValueBuffer;
        int bufferChangeStart, bufferChangeEnd;
        SysCol.Dictionary<Tuple<TextureUnit, TextureTarget>, int> textureBinds
            = new SysCol.Dictionary<Tuple<TextureUnit, TextureTarget>, int>();

        public void CommitChanges()
        {
            AssertAlive();
            Graphics.ThrowErrors();
            if (uniformBuffer >= 0)
            {
                GL.BindBufferRange(BufferRangeTarget.UniformBuffer, 0, uniformBuffer, IntPtr.Zero, (IntPtr)totalUniformBufferSize);
                Graphics.ThrowErrors();
                if (bufferChangeEnd > bufferChangeStart)
                {
                    GL.BufferSubData(
                        BufferTarget.UniformBuffer,
                        (IntPtr)bufferChangeStart,
                        (bufferChangeEnd - bufferChangeStart),
                        IntPtr.Add(uniformValueBuffer, bufferChangeStart)
                        );

                    Graphics.ThrowErrors();
                    bufferChangeStart = int.MaxValue;
                    bufferChangeEnd = 0;
                }
            }

            foreach (SysCol.KeyValuePair<Tuple<TextureUnit, TextureTarget>, int> binding in textureBinds)
            {
                GL.ActiveTexture(binding.Key.Item1);
                Graphics.ThrowErrors();
                GL.BindTexture(binding.Key.Item2, binding.Value);
                Graphics.ThrowErrors();
            }
        }

        public void SetValue(string variable, Matrix value)
            => SetValue<Matrix>(variable, Matrix.Transpose(value));

        public void SetValue(SemanticHandle semantic, Matrix value)
            => SetValue<Matrix>(semantic, Matrix.Transpose(value));

        public void SetValue<FieldType>(string variable, FieldType value)
            => SetValue(GetVariableHandle(variable), value);

        public void SetValue<FieldType>(SemanticHandle semantic, FieldType value)
        {
            foreach (string variable in semantic.variableNames)
                SetValue(GetVariableHandle(variable), value);
        }

        public void SetValue<EntryType>(string variable, AssetContainer<EntryType>.Entry value)
            where EntryType : class
            => SetValue(GetVariableHandle(variable), value?.content);

        public void SetValue<EntryType>(SemanticHandle semantic, AssetContainer<EntryType>.Entry value)
            where EntryType : class
        {
            foreach (string variable in semantic.variableNames)
                SetValue(GetVariableHandle(variable), value?.content);
        }

        unsafe void SetValue<T>(ShaderVariable variable, T value)
        {
            AssertAlive();
            if (variable == null)
                return;

            UniformBufferVariable uniformHandle;
            SamplerVariable samplerHandle;
            if ((uniformHandle = variable as UniformBufferVariable) != null)
            {
                Type actualRuntimeType = value.GetType();
                if (actualRuntimeType.IsArray)
                {
                    Type elementType = actualRuntimeType.GetElementType();
                    Array array = (Array)(object)value;
                    int elementTypeSize = Marshal.SizeOf(elementType);
                    if (array.Length > uniformHandle.size)
                        throw new InvalidOperationException($"Array is too large. (Size was {array.Length}, max allowed is {uniformHandle.size}");

                    GCHandle arrayHandle = GCHandle.Alloc(array);
                    bufferChangeStart = Min(bufferChangeStart, (int)uniformHandle.offset);
                    bufferChangeEnd = Clamp(
                        bufferChangeEnd,
                        totalUniformBufferSize,
                        (int)uniformHandle.offset + (array.Length - 1) * uniformHandle.arrayStride + elementTypeSize
                        );

                    for (int i = 0; i < array.Length; i++)
                        // TODO: For some reason this specific line can cause an AccessViolation that is not repeatable.
                        //       This may be related to the driver locking on this memory when it is being copied to the GPU
                        NativeMemory.Copy(
                            (void*)Marshal.UnsafeAddrOfPinnedArrayElement(array, i),
                            (void*)IntPtr.Add(
                                uniformValueBuffer,
                                (int)uniformHandle.offset + i * uniformHandle.arrayStride),
                            (UIntPtr)elementTypeSize
                            );
                    arrayHandle.Free();
                }
                else if (!typeof(T).IsClass)
                {
                    bufferChangeStart = Min(bufferChangeStart, (int)uniformHandle.offset);
                    bufferChangeEnd = Clamp(
                        bufferChangeEnd,
                        totalUniformBufferSize,
                        (int)uniformHandle.offset + uniformHandle.size * Marshal.SizeOf(typeof(T))
                        );

                    Marshal.StructureToPtr(value, IntPtr.Add(uniformValueBuffer, (int)uniformHandle.offset), false);
                }
            }
            else if ((samplerHandle = variable as SamplerVariable) != null)
            {
                Texture textureValue = value as Texture;
                Tuple<TextureUnit, TextureTarget> bindingKey = new Tuple<TextureUnit, TextureTarget>(
                    TextureUnit.Texture0 + samplerHandle.textureUnit,
                    textureValue == null ? TextureTarget.Texture2D : textureValue.textureTarget
                    );
                textureBinds[bindingKey] = textureValue == null ? 0 : textureValue.textureIndex;
            }
            else
                throw new NotSupportedException($"Effect handle type {variable.GetType().Name} is not supported.");
        }

        ShaderVariable GetVariableHandle(string name)
        {
            AssertAlive();
            if (name == null)
                return null;

            ShaderVariable handle;
            if (variableHandles.TryGetValue(name, out handle))
                return handle;

            return null;
        }
    }
}
