using ChaosFramework.Core;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using static ChaosFramework.Math.Clamping;
using SysCol = System.Collections.Generic;
using ChaosFramework.Graphics.Text;

namespace ChaosFramework.Graphics.OpenGl.Text
{
    partial class TextRenderer
    {
        public abstract class TextBuffer : Disposable
        {
            internal class TextNodeUsage
            {
                internal readonly TextNode node;

                internal int instanceCount;

                public TextNodeUsage(TextNode node)
                {
                    this.node = node;
                }
            }

            internal readonly int numMetaPerRow;
            internal readonly Texture metaDataTex;
            internal readonly SysCol.Dictionary<TextGeometry, TextNodeUsage> writeData;

            readonly System.IntPtr metaDataBuffer;

            int maxMetaIndex = -1;
            bool needsBuffering = false;

            internal TextBuffer(TextRenderer renderer, SysCol.Dictionary<TextGeometry, TextNodeUsage> writeData)
            {
                this.writeData = writeData;

                const int NUM_PIXELS_PER_TEXT = 6;
                const int TEXTUREWIDTH = 512;
                int requiredHeight = (renderer.textCapacity * NUM_PIXELS_PER_TEXT + TEXTUREWIDTH - 1) / TEXTUREWIDTH;
                metaDataTex = new Texture(
                    renderer.graphics.dispatcher,
                    new Texture.Parameters(
                        TEXTUREWIDTH,
                        requiredHeight,
                        internalFormat: PixelInternalFormat.Rgba32f,
                        pixelFormat: PixelFormat.Rgba,
                        pixelType: PixelType.Float,
                        minFilter: TextureMinFilter.Nearest
                        )
                    );
                numMetaPerRow = TEXTUREWIDTH / NUM_PIXELS_PER_TEXT;
                metaDataBuffer = Marshal.AllocHGlobal(metaDataTex.args.width * metaDataTex.args.height * sizeof(float) * 4);
                maxMetaIndex = -1;
            }

            internal abstract TextNode GetNodeForGeometry(Text text);
            internal abstract void CollectCommands();
            internal abstract bool SkipDraw();
            internal abstract void ExecuteCommands();

            public void Clear()
            {
                maxMetaIndex = -1;
                writeData.Clear();
            }

            public void Add(params Text[] texts)
                => Add((SysCol.IEnumerable<Text>)texts);

            public void Add(SysCol.IEnumerable<Text> texts)
            {
                int rowSize = sizeof(float) * 4 * metaDataTex.args.width;

                foreach (Text text in texts)
                {
                    TextNodeUsage usage;
                    if (!writeData.TryGetValue(text.geometry, out usage))
                        writeData[text.geometry] = usage = new TextNodeUsage(GetNodeForGeometry(text));

                    int metaIndex = usage.node.usageData.baseMeta + usage.instanceCount++;
                    maxMetaIndex = Max(maxMetaIndex, metaIndex);
                    int x = metaIndex % numMetaPerRow, y = metaIndex / numMetaPerRow;
                    Marshal.StructureToPtr(
                        text.meta,
                        System.IntPtr.Add(metaDataBuffer, y * rowSize + x * TextInstanceData.SIZE_IN_BYTES),
                        false
                        );
                }
            }

            public void Flush()
            {
                int oldTex = GL.GetInteger(GetPName.TextureBinding2D);
                Graphics.ThrowErrors();
                GL.BindTexture(TextureTarget.Texture2D, metaDataTex.textureIndex);
                Graphics.ThrowErrors();

                if (maxMetaIndex >= 0)
                {
                    GL.TexSubImage2D(
                        TextureTarget.Texture2D,
                        0,
                        0,
                        0,
                        metaDataTex.args.width,
                        (maxMetaIndex + numMetaPerRow) / numMetaPerRow,
                        PixelFormat.Rgba,
                        PixelType.Float,
                        metaDataBuffer);
                    Graphics.ThrowErrors();
                }

                GL.BindTexture(TextureTarget.Texture2D, oldTex);
                Graphics.ThrowErrors();

                CollectCommands();
            }

            protected override void DoDispose()
            {
                base.DoDispose();
                metaDataTex.Dispose();
                Marshal.FreeHGlobal(metaDataBuffer);
                Clear();
            }
        }
    }
}
