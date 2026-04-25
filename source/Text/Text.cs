using ChaosFramework.Core;
using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Graphics.Text;

namespace ChaosFramework.Graphics.OpenGl.Text
{
    public class Text : Disposable
    {
        readonly TextRenderer renderer;
        internal readonly int charCapacity;

        internal TextRenderer.TextInstanceData meta;

        internal TextGeometry _geometry;
        public TextGeometry geometry => _geometry;

        public Rgba color
        {
            get { return meta.color; }
            set { meta.color = value; }
        }

        public Vector3f channelMultipliers
        {
            get { return meta.channelMultipliers; }
            set { meta.channelMultipliers = value; }
        }

        Matrix _transform = Matrix.NAN;
        public Matrix transform
        {
            get { return _transform; }
            set { meta.transform = Matrix.Transpose(_transform = value); }
        }

        public Text(TextRenderer renderer, int charCapacity)
        {
            this.renderer = renderer;
            this.charCapacity = charCapacity;
            channelMultipliers = new Vector3f(0, 1, 0);
            color = Rgba.OPAQUE_WHITE;
            transform = Matrix.IDENTITY;
        }

        public void UpdateText(Font font, string text, LayoutInfo layout)
        {
            meta.fontIndex = renderer.GetFontIndex(font);

            TextGeometryDescription args = new TextGeometryDescription(font, text, layout);
            _geometry = renderer.GetGeometry(args, this);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            renderer.UnuseGeometry(this);
        }
    }
}
