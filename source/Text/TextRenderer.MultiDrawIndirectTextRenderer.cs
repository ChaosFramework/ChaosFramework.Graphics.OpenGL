using static ChaosFramework.Math.Clamping;
using SysCol = System.Collections.Generic;
using ChaosFramework.Graphics.Text;

namespace ChaosFramework.Graphics.OpenGl.Text
{
    partial class TextRenderer
    {
        internal class MultiDrawIndirectTextRenderer : TextRenderer
        {
            internal SysCol.Dictionary<TextGeometryDescription, TextNode> geometries
                = new SysCol.Dictionary<TextGeometryDescription, TextNode>();

            public MultiDrawIndirectTextRenderer(
                Graphics graphics,
                int maxTexts,
                int glyphCapacity,
                int maxCharsPerGeometry,
                params Font[] fonts
                ) : base(graphics, maxTexts, glyphCapacity, maxCharsPerGeometry, fonts)
            { }

            public override TextBuffer CreateRenderContext()
                => new MultiDrawIndirectTextBuffer(this);

            internal override bool GetOrCreateTextNode(Text user, out TextNode result)
            {
                if (geometries.TryGetValue(user.geometry.args, out result))
                    return true;

                geometries[user.geometry.args] = result = GetTextNode(Max(user.charCapacity, user.geometry.numPrintedChars));
                return false;
            }

            internal override void UnuseGeometry(Text text)
            {
                TextNode result;
                if (text.geometry != null && geometries.TryGetValue(text.geometry.args, out result))
                {
                    result.users.Remove(text);
                    if (result.users.Count == 0)
                    {
                        geometries.Remove(text.geometry.args);
                        FreeNode(result);
                    }
                }
            }
        }
    }
}
