namespace ChaosFramework.Graphics.OpenGl.Text
{
    partial class TextRenderer
    {
        internal class TextUsageNode
        {
            internal const int FREED = -1;

            public readonly TextInstanceData[] meta;

            public TextUsageNode next;
            public int baseMeta;

            internal TextUsageNode(int capacity, int baseMeta)
            {
                meta = new TextInstanceData[capacity];
                this.baseMeta = baseMeta;
            }
        }

        TextUsageNode firstMeta;

        TextUsageNode GetMetaNode(int nodeCapacity)
        {
            if (firstMeta == null)
                return firstMeta = new TextUsageNode(nodeCapacity, 0);

            int nodeCount = 0, freeNodes = 0, occupiedNodes = 0;
            TextUsageNode n = firstMeta, last = firstMeta;
            int ptr = 0;
            int freeSpace = textCapacity;
            while (n != null)
            {
                nodeCount++;
                if (n.baseMeta == TextUsageNode.FREED)
                {
                    freeNodes++;
                    if (n.meta.Length >= nodeCapacity)
                    {
                        n.baseMeta = ptr;
                        return n;
                    }
                }
                else
                {
                    occupiedNodes++;
                    System.Diagnostics.Debug.Assert(ptr == n.baseMeta);
                    freeSpace -= n.meta.Length;
                }
                ptr += n.meta.Length;
                last = n;
                n = n.next;
            }
            if (nodeCapacity > freeSpace)
                throw new System.InvalidOperationException("Exceeding meta node capacity.");

            if (ptr + nodeCapacity > glyphCapacity)
                throw new System.InvalidOperationException("Defragmentation needed.");

            return last.next = new TextUsageNode(nodeCapacity, ptr);
        }
    }
}
