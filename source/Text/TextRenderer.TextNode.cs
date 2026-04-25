using SysCol = System.Collections.Generic;
using ChaosFramework.Graphics.Text;

namespace ChaosFramework.Graphics.OpenGl.Text
{
    partial class TextRenderer
    {
        internal class TextNode
        {
            internal const int FREED = -1;

            internal readonly SysCol.HashSet<Text> users;
            internal readonly int capacity;

            internal TextNode next;
            internal TextGeometry geometry;
            internal TextUsageNode usageData;
            internal int baseVertex;

            internal TextNode(int capacity, int baseVertex)
            {
                this.capacity = capacity;
                this.baseVertex = baseVertex;
                users = new SysCol.HashSet<Text>();
            }
        }

        TextNode firstText;

        TextNode GetTextNode(int nodeCapacity)
        {
            if (firstText == null)
                return firstText = new TextNode(nodeCapacity, 0);

            TextNode n = firstText;
            TextNode last = firstText;

            int ptr = 0;
            int freeSpace = glyphCapacity;
            while (n != null)
            {
                if (n.baseVertex == TextNode.FREED)
                {
                    if (n.capacity >= nodeCapacity)
                    {
                        n.baseVertex = ptr;
                        return n;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(ptr == n.baseVertex);
                    freeSpace -= n.capacity;
                }
                ptr += n.capacity;
                last = n;
                n = n.next;
            }
            if (nodeCapacity > freeSpace)
                throw new System.InvalidOperationException("Exceeding text node capacity.");

            if (ptr + nodeCapacity > glyphCapacity)
                throw new System.InvalidOperationException("Defragmentation needed.");

            return last.next = new TextNode(nodeCapacity, ptr);
        }
    }
}
