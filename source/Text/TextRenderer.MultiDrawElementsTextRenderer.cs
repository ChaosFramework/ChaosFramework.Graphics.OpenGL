using static ChaosFramework.Math.Clamping;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.Text
{
    partial class TextRenderer
    {
        internal class MultiDrawElementsTextRenderer : TextRenderer
        {
            internal readonly SysCol.Dictionary<Text, TextNode> geometries = new SysCol.Dictionary<Text, TextNode>();

            public MultiDrawElementsTextRenderer(
                Graphics graphics,
                int maxTexts,
                int glyphCapacity,
                int maxCharsPerGeometry,
                params Font[] fonts
                ) : base(graphics, maxTexts, glyphCapacity, maxCharsPerGeometry, fonts)
            { }

            public override TextBuffer CreateRenderContext()
                => new MultiDrawElementsTextBuffer(this);

            internal override bool GetOrCreateTextNode(Text user, out TextNode result)
            {
                geometries[user] = result = GetTextNode(Max(user.charCapacity, user.geometry.numPrintedChars));
                return false;
            }

            internal override void UnuseGeometry(Text text)
            {
                TextNode result;
                if (geometries.TryGetValue(text, out result))
                {
                    result.users.Remove(text);
                    if (result.users.Count == 0)
                    {
                        geometries.Remove(text);
                        FreeNode(result);
                    }
                }
            }
        }
    }
}
