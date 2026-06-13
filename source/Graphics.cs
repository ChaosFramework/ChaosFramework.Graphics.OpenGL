using ChaosFramework.Core;
using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Platform;
using ChaosFramework.Shapes;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using OpenTK.Windowing.Desktop;
using System;
using System.Threading;
using static ChaosFramework.Math.Clamping;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl
{
    public class Graphics : Disposable
    {
        static Thread graphicsThread;

        static void DisposeMaterial(Material mat) => mat.Dispose();

        [System.Diagnostics.Conditional("ThrowErrors")]
        public static void ThrowErrors()
        {
            if (System.Threading.Thread.CurrentThread != graphicsThread)
                throw new InvalidOperationException("ThrowErrors must be called from the GL thread!");
            ErrorCode error = GL.GetError();

            if (error != ErrorCode.NoError)
                throw new InvalidOperationException(error.ToString());
        }

        public readonly PlatformContext platformContext;
        public readonly Dispatcher dispatcher;
        public readonly Shaders shaders;
        public readonly int versionMajor, versionMinor;
        public readonly int glVersionMajor, glVersionMinor;
        public readonly int coreProfile;

        public event Action windowsChanged;

        internal readonly MeshContainer meshes;
        internal readonly TextureContainer textures;

        internal int sharedInstancingBuffer = -1;
        int maxInstancingSize = 0;

        readonly SysCol.HashSet<string> supportedExtensions;

        /// <summary>
        ///     Some GPUs (particularly intel, as it appears) need GL.Flush commands
        ///     after rendering with a specific shader program,
        ///     because they appear to mix up shader inputs otherwise.
        /// </summary>
        public bool needsFlush { get; private set; }

        public TextureContainer.Entry whitePixel { get; private set; }
        public MaterialContainer.Entry defaultMaterial { get; private set; }

        public int emptyVAO { get; private set; }
        public int triangleVAO { get; private set; }

        public GlStateTracker stateTracker { get; private set; }

        public Graphics(
            PlatformContext platformContext,
            int versionMajor,
            int versionMinor,
            Action<Graphics> loadingScreen = null
            ) : this(Dispatcher.dispatcher, platformContext, versionMajor, versionMinor, loadingScreen)
        { }

        public Graphics(
            Dispatcher dispatcher,
            PlatformContext platformContext,
            int versionMajor,
            int versionMinor,
            Action<Graphics> loadingScreen = null
            )
        {
            this.dispatcher = dispatcher;
            this.platformContext = platformContext;
            this.versionMajor = versionMajor;
            this.versionMinor = versionMinor;

            coreProfile = 100 * versionMajor + 10 * versionMinor;
            supportedExtensions = new SysCol.HashSet<string>();

            graphicsThread = System.Threading.Thread.CurrentThread;

            platformContext.Setup();

            GL.LoadBindings(new OpenTK.Windowing.GraphicsLibraryFramework.GLFWBindingsContext());
            int numExts = GL.GetInteger(GetPName.NumExtensions);
            for (int i = 0; i < numExts; i++)
                supportedExtensions.Add(GL.GetString(StringNameIndexed.Extensions, i));
            glVersionMajor = GL.GetInteger(GetPName.MajorVersion);
            glVersionMinor = GL.GetInteger(GetPName.MinorVersion);

            CreateDevice();

            new Shaders(this, ref shaders);
            loadingScreen?.Invoke(this);

            meshes = new MeshContainer(StreamSources.meshes, dispatcher, false);
            meshes.AddFactory("$Sprite", _ => new Model.Mesh(dispatcher, MeshData.sprite));

            textures = new TextureContainer(StreamSources.textures, dispatcher, false);
            whitePixel = textures.Load("$WhitePixel", this);

            defaultMaterial = MaterialContainer.Entry.Mock((_, __) => new Material(this), DisposeMaterial);
            ThrowErrors();
        }

        Material CreateDefaultMaterial(MaterialContainer.Key key, CancellationToken ct) => new Material(this);

        public bool SupportsExtension(string extensionName)
            => supportedExtensions.Contains(extensionName);

        public bool SupportsExtensions(params string[] extensionNames)
        {
            foreach (string ext in extensionNames)
                if (!supportedExtensions.Contains("GL_" + ext))
                    return false;

            return true;
        }

        public void FitInstancingBuffer(int maxSizeInBytes)
        {
            if (sharedInstancingBuffer == -1)
            {
                sharedInstancingBuffer = GL.GenBuffer();
                ThrowErrors();
            }

            if (maxSizeInBytes > maxInstancingSize)
            {
                maxInstancingSize = Max(maxSizeInBytes, maxInstancingSize);
                GL.BindBuffer(BufferTarget.ArrayBuffer, sharedInstancingBuffer);
                ThrowErrors();
                GL.BufferData(BufferTarget.ArrayBuffer, maxInstancingSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                ThrowErrors();
            }
        }

        private void CreateDevice()
        {
            string vendor = GL.GetString(StringName.Vendor);
            needsFlush = vendor.ToLower().Contains("intel");

            GL.BlendFuncSeparate(
                BlendingFactorSrc.SrcColor,
                BlendingFactorDest.OneMinusSrcColor,
                BlendingFactorSrc.SrcAlpha,
                BlendingFactorDest.One
                );
            ThrowErrors();
            GL.Disable(EnableCap.DepthTest);
            ThrowErrors();
            emptyVAO = GL.GenVertexArray();
            ThrowErrors();
            triangleVAO = GL.GenVertexArray();
            ThrowErrors();

            stateTracker = new GlStateTracker(this);
        }

        protected override void DoDispose()
        {
            base.DoDispose();

            dispatcher.Dispatch(DestroyEverything);
        }

        void DestroyEverything()
        {
            ThrowErrors();

            GL.DeleteVertexArray(emptyVAO);
            ThrowErrors();
            GL.DeleteVertexArray(triangleVAO);
            ThrowErrors();
            if (sharedInstancingBuffer != -1)
                GL.DeleteBuffer(sharedInstancingBuffer);
            ThrowErrors();

            defaultMaterial.content.Dispose();
            shaders.Dispose();
            meshes.Dispose();
            textures.Dispose();

            // TODO: figure out what needs to be done here
            // graphicsContext.Dispose();
        }
    }
}
