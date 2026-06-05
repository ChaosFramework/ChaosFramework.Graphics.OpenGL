using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Platform.Paths;
using ChaosUtil.Primitives;
using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.InteropServices;
using static ChaosFramework.Math.Clamping;

namespace ChaosFramework.Graphics.OpenGl
{
    using Model;

    public abstract class AnimatedInstancerBase : InstancingAttribute
    {
        IntPtr? instanceBuffer;
        Texture transformTex;
        bool invalidated = false;
        bool needsTextureAlloc = false;
        int instanceStride;
        int maxBones;

        protected abstract ShaderContainer.Entry GetShader(string source);
        protected abstract MaterialContainer.Entry GetMaterial(string source);
        protected abstract MeshContainer.Entry GetMesh(string source);

        public ShaderContainer.Entry shader { get; protected set; }
        public MaterialContainer.Entry material { get; protected set; }
        public MeshContainer.Entry mesh { get; protected set; }

        public AnimatedMeshData animesh => (AnimatedMeshData)mesh.content.data;

        public AnimatedInstancerBase() { }
        public AnimatedInstancerBase(
            int maxInstances,
            string meshSource,
            string materialSource,
            string overrideEffect = null,
            params string[] customRegisters)
            : base(maxInstances, new object[]
            {
                Normalization.NormalizeRelative(meshSource),
                Normalization.NormalizeRelative(materialSource),
                Normalization.NormalizeRelative(overrideEffect),
                customRegisters
            })
        { }

        public AnimatedInstancerBase(
            int maxInstances,
            string meshSource,
            string materialSource,
            string overrideEffect,
            string[] customRegisters,
            params object[] customArgs
            )
            : base(
                maxInstances,
                new object[] {
                    Normalization.NormalizeRelative(meshSource),
                    Normalization.NormalizeRelative(materialSource),
                    Normalization.NormalizeRelative(overrideEffect),
                    customRegisters
                }.Concat(customArgs)
              )
        { }

        protected abstract void Initialize_2(Graphics graphics, object[] parameters);

        public override sealed void Initialize(Graphics graphics, int maxInstances, params object[] parameters)
        {
            informer = new MatrixInstancer(graphics, (string[])parameters[3], maxInstances);
            mesh = GetMesh((string)parameters[0]);
            material = GetMaterial((string)parameters[1]) ?? graphics.defaultMaterial;
            shader = parameters[2] == null
                ? graphics.shaders.skinnedInstanceNormalMap
                : GetShader((string)parameters[2]);
            maxBones = ((AnimatedMeshData)mesh.content.data).groupNames.length;
            instanceStride = maxBones * 0x40;
            transformTex = new Texture(
                graphics.dispatcher,
                new Texture.Parameters(
                    width: 4 * maxBones,
                    height: maxInstances,
                    pixelType: PixelType.Float,
                    pixelFormat: PixelFormat.Rgba,
                    internalFormat: PixelInternalFormat.Rgba32f,
                    minFilter: TextureMinFilter.Nearest
                    )
                );
            instanceBuffer = Marshal.AllocHGlobal(maxInstances * instanceStride);
            Initialize_2(graphics, parameters);
        }

        public unsafe void AddInstance(Matrix[] bones, Matrix baseTransform, params Vector4f[] customRegisters)
        {
            invalidated = true;
            if (informer.numInstances >= maxInstances)
            {
                int newMaxInstances = Max(1, maxInstances * 2);
                IntPtr newDataPtr = Marshal.AllocHGlobal(newMaxInstances * instanceStride);
                NativeMemory.Copy(
                    (void*)instanceBuffer.Value,
                    (void*)newDataPtr,
                    (UIntPtr)(maxInstances * instanceStride)
                    );

                maxInstances = newMaxInstances;
                instanceBuffer = newDataPtr;
                needsTextureAlloc = true;
            }

            for (int i = 0; i < bones.Length; i++)
                Marshal.StructureToPtr(
                    bones[i],
                    IntPtr.Add(instanceBuffer.Value, informer.numInstances * instanceStride + 0x40 * i),
                    false
                    );

            informer.AddInstance(baseTransform, customRegisters);
        }

        protected virtual void DrawInstances(Camera view, string pass)
        {
            if (informer.numInstances == 0)
                return;

            if (invalidated)
            {
                GL.BindTexture(TextureTarget.Texture2D, transformTex.textureIndex);
                Graphics.ThrowErrors();
                if (needsTextureAlloc)
                    GL.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        PixelInternalFormat.Rgba32f,
                        4 * maxBones,
                        maxInstances,
                        0,
                        PixelFormat.Rgba,
                        PixelType.Float,
                        instanceBuffer.Value);
                else
                    GL.TexSubImage2D(
                        TextureTarget.Texture2D,
                        0,
                        0,
                        0,
                        transformTex.args.width,
                        informer.numInstances,
                        PixelFormat.Rgba,
                        PixelType.Float,
                        instanceBuffer.Value);
                Graphics.ThrowErrors();
                invalidated = false;
                needsTextureAlloc = false;
            }

            shader.content.SetValue("boneTransforms", transformTex);
            view.SetValues(shader, Matrix.IDENTITY, Matrix.IDENTITY);
            material.content.SetValues(shader);
            mesh.content.DrawInstanced(shader, pass, informer);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (instanceBuffer != null)
                Marshal.FreeHGlobal(instanceBuffer.Value);

            transformTex?.Dispose();
        }
    }
}
