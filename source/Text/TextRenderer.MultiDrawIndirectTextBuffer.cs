using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using SysCol = System.Collections.Generic;
using ChaosFramework.Graphics.Text;

namespace ChaosFramework.Graphics.OpenGl.Text
{
    partial class TextRenderer
    {
        internal class MultiDrawIndirectTextBuffer : TextBuffer
        {
            internal struct DrawIndirectCommand
            {
                internal int count, instanceCount, firstIndex, baseVertex, baseInstance;
            }

            readonly MultiDrawIndirectTextRenderer renderer;
            internal DrawIndirectCommand[] indirectCmds;

            internal MultiDrawIndirectTextBuffer(MultiDrawIndirectTextRenderer renderer)
                : base(renderer, new SysCol.Dictionary<TextGeometry, TextNodeUsage>())
            {
                this.renderer = renderer;
            }

            internal override TextNode GetNodeForGeometry(Text text)
            {
                TextNode n;
                if (!renderer.geometries.TryGetValue(text.geometry.args, out n))
                    throw new System.ArgumentException("This geometry does not exist.");

                return n;
            }

            internal override void CollectCommands()
            {
                int indirectCmdIndex = 0;
                indirectCmds = new DrawIndirectCommand[writeData.Values.Count];
                foreach (TextNodeUsage n in writeData.Values)
                    indirectCmds[indirectCmdIndex++] = new DrawIndirectCommand
                    {
                        count = n.node.geometry.numPrintedChars * 6,
                        instanceCount = n.instanceCount,
                        firstIndex = 0,
                        baseVertex = n.node.baseVertex * 4,
                        baseInstance = 0
                    };
            }

            internal override bool SkipDraw()
                => indirectCmds == null || indirectCmds.Length == 0;

            internal override void ExecuteCommands()
            {
                GCHandle handle = GCHandle.Alloc(indirectCmds);
                try
                {
                    GL.MultiDrawElementsIndirect(
                        PrimitiveType.Triangles,
                        DrawElementsType.UnsignedShort,
                        Marshal.UnsafeAddrOfPinnedArrayElement(indirectCmds, 0),
                        indirectCmds.Length,
                        0
                        );
                    Graphics.ThrowErrors();
                }
                finally
                {
                    handle.Free();
                }
            }
        }
    }
}
