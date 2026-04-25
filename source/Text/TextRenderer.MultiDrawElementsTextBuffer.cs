using ChaosFramework.Collections;
using SysCol = System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using ChaosFramework.Graphics.Text;

namespace ChaosFramework.Graphics.OpenGl.Text
{
    partial class TextRenderer
    {
        internal class MultiDrawElementsTextBuffer : TextBuffer
        {
            static readonly GenericComparer<TextGeometry> textGeometryComparer = new GenericComparer<TextGeometry>(Equals, GetHashCode);

            static bool Equals(TextGeometry a, TextGeometry b) => a == b;
            static int GetHashCode(TextGeometry x) => x.GetHashCode();

            readonly MultiDrawElementsTextRenderer renderer;

            internal int[] elementCounts, baseVertex;

            internal MultiDrawElementsTextBuffer(MultiDrawElementsTextRenderer renderer)
                : base(renderer, new SysCol.Dictionary<TextGeometry, TextNodeUsage>(textGeometryComparer))
            {
                this.renderer = renderer;
            }

            internal override TextNode GetNodeForGeometry(Text text)
            {
                TextNode n;
                if (!renderer.geometries.TryGetValue(text, out n))
                    throw new System.Exception($"Could not retrieve node for non-multidraw text: {text.geometry.text}");

                return n;
            }

            internal override void CollectCommands()
            {
                int commandCount = writeData.Values.Count;
                elementCounts = new int[commandCount];
                baseVertex = new int[commandCount];

                int cmdIndex = 0;
                foreach (TextNodeUsage f in writeData.Values)
                {
                    elementCounts[cmdIndex] = f.node.geometry.numPrintedChars * 6;
                    baseVertex[cmdIndex] = f.node.baseVertex * 4;
                    cmdIndex++;
                }
            }

            internal override bool SkipDraw()
                => elementCounts == null || elementCounts.Length == 0;

            internal override void ExecuteCommands()
            {
                GL.MultiDrawElementsBaseVertex(
                    PrimitiveType.Triangles,
                    elementCounts,
                    DrawElementsType.UnsignedShort,
                    new System.IntPtr[elementCounts.Length],
                    elementCounts.Length,
                    baseVertex
                    );
                Graphics.ThrowErrors();
            }
        }
    }
}
