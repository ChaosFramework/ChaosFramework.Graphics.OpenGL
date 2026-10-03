using System.IO;
using System.Linq;
using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosFramework.Graphics.Imaging;
using ChaosFramework.Graphics.Text;
using ChaosFramework.IO;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.Shapes;
using ChaosUtil.Primitives;
using OpenTK.Graphics.OpenGL;
using static ChaosFramework.Math.Clamping;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.Text
{
    using Model;
    using WeakReferencedMesh = System.Tuple<System.WeakReference, Model.MeshBuffers>;

    public partial class Font : Disposable, GlyphDimensionProvider
    {
        const int FREE_MESH_BUFFER = 66;

        readonly SysCol.Dictionary<UnicodeChars, GlyphDimensions> chars = new SysCol.Dictionary<UnicodeChars, GlyphDimensions>();

        internal SysCol.Dictionary<TextGeometryDescription, System.WeakReference<TextMesh>> bufferedGeos
            = new SysCol.Dictionary<TextGeometryDescription, System.WeakReference<TextMesh>>();

        public static Font FromStream(Graphics graphics, BinaryReader rd)
            => new Font(graphics, rd);

        readonly Vector2i sdfBounds, colBounds;
        readonly byte sdfRadius;
        public FontTextureDimensions textureDimensions => new FontTextureDimensions(sdfBounds, colBounds, sdfRadius);

        readonly Graphics graphics;

        LinkedList<MeshBuffers> freeMeshes = new LinkedList<MeshBuffers>();
        AdvancedLinkedList<WeakReferencedMesh> occupiedMeshes = new AdvancedLinkedList<WeakReferencedMesh>();

        public Texture col { get; private set; }
        public Texture sdf { get; private set; }

        public SysCol.IEnumerable<UnicodeChars> GetGlyphs()
            => chars.Keys;

        public GlyphDimensions GetGlyph(char c)
            => GetGlyph((UnicodeChars)c);

        public GlyphDimensions GetGlyph(UnicodeChars c)
            => chars[chars.ContainsKey(c) ? c : UnicodeChars.Null];

        internal Font(Graphics graphics)
        {
            this.graphics = graphics;
            sdfRadius = 0;
            chars = new SysCol.Dictionary<UnicodeChars, GlyphDimensions>();
            if (!chars.ContainsKey(UnicodeChars.Null))
                chars[UnicodeChars.Null] = new GlyphDimensions(
                    new Bounds2f(0, 0, 1, 1),
                    new Bounds2i(),
                    new Bounds2i(),
                    new Vector2f(1, 1)
                    );
        }

        internal Font(Graphics graphics, BinaryReader rd)
        {
            this.graphics = graphics;
            sdfRadius = rd.Read<byte>();

            chars = rd.Read<SysCol.Dictionary<UnicodeChars, GlyphDimensions>>();
            if (!chars.ContainsKey(UnicodeChars.Null))
                chars[UnicodeChars.Null] = new GlyphDimensions(
                    new Bounds2f(0, 0, 1, 1),
                    new Bounds2i(),
                    new Bounds2i(),
                    new Vector2f(1, 1)
                    );

            sdfBounds = rd.Read<Vector2i>();
            byte[] sdfBytes = rd.Read<byte[]>();
            if (sdfBounds.x % 4 != 0)
            {
                int newWidth = (sdfBounds.x + 3) / 4 * 4;
                byte[] tmp = new byte[newWidth * sdfBounds.y];
                for (int i = 0; i < sdfBounds.y; i++)
                    System.Array.Copy(sdfBytes, sdfBounds.x * i, tmp, newWidth * i, sdfBounds.x);

                sdfBounds.x = newWidth;
                sdfBytes = tmp;
            }

            Rgba8Image colImg = Imaging.Formats.Png.FromStream(rd.BaseStream, false);
            colBounds = colImg.Size();

            System.Action bind = () =>
            {
                col = Texture.FromBitmap(graphics.dispatcher, colImg);
                sdf = new Texture(
                    graphics.dispatcher,
                    new Texture.Parameters(
                        sdfBounds.x,
                        sdfBounds.y,
                        pixelType: PixelType.UnsignedByte,
                        minFilter: TextureMinFilter.Linear,
                        magFilter: TextureMagFilter.Linear,
                        pixelFormat: PixelFormat.Red,
                        internalFormat: PixelInternalFormat.R8
                        ),
                    () => RawDataHandle.Create(sdfBytes)
                    );
                GL.BindTexture(TextureTarget.Texture2D, col.textureIndex);
                Graphics.ThrowErrors();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                Graphics.ThrowErrors();
                GL.BindTexture(TextureTarget.Texture2D, 0);
                Graphics.ThrowErrors();
            };

            graphics.dispatcher.RunAndAwait(bind);
        }

        public void SetValues(ChaosShader.Shader fx)
        {
            fx.SetValue("sdfParams", new Vector4f(0.5f, 2.117f, 1, 0));
            fx.SetValue("channelMultipliers", new Vector4f(0, 1, 0, 0));
            fx.SetValue(fx.GetParameterBySemantic("TEX"), col);
            fx.SetValue(fx.GetParameterBySemantic("SDF"), sdf);
        }

        public bool UpdateText(ref TextMesh target, string text, LayoutInfo layout)
            => UpdateText(ref target, new TextGeometryDescription(this, text, layout));

        internal bool UpdateText(ref TextMesh target, TextGeometryDescription args)
        {
            if (target != null && target.geo.args == args)
                return false;

            System.WeakReference<TextMesh> output;
            lock (bufferedGeos)
            {
                if (bufferedGeos.TryGetValue(args, out output) && output.TryGetTarget(out target))
                    return true;
            }

            TextMesh newTarget = null;
            graphics.dispatcher.RunAndAwait(() =>
            {
                lock (bufferedGeos)
                {
                    // TODO: Consider common textgeometry buffer with managed text
                    TextGeometry geo = new TextGeometry(args);
                    newTarget = new TextMesh(this, GetFreeMesh(), geo);
                    bufferedGeos[args] = new System.WeakReference<TextMesh>(newTarget);
                    occupiedMeshes.Add(new WeakReferencedMesh(new System.WeakReference(newTarget), newTarget.mesh.buffers));
                }
            });
            target = newTarget;
            return true;
        }

        MeshBuffers GetFreeMesh()
        {
            foreach (WeakReferencedMesh occupied in occupiedMeshes)
                if (!occupied.Item1.IsAlive)
                {
                    if (freeMeshes.length < FREE_MESH_BUFFER)
                        freeMeshes.Add(occupied.Item2);
                    else
                        occupied.Item2.Dispose();
                    occupiedMeshes.RemoveCurrent();
                }

            if (freeMeshes.empty)
                freeMeshes.Add(new MeshBuffers(
                    graphics.dispatcher,
                    new MeshData(
                        (byte)MeshLoadFlags.Default,
                        new Vector3f[1],
                        null,
                        null,
                        null,
                        new uint[1],
                        new MeshData.CustomStreamDataArray<Vector4f>(new Vector4f[1], "COLOR", "COLOR0"),
                        new MeshData.CustomStreamDataArray<Vector4f>(new Vector4f[1], "TEXCOORD", "TEXCOORD0")
                        )
                    ));

            return freeMeshes.RemoveAt(0);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            sdf.Dispose();
            col.Dispose();
            foreach (MeshBuffers mesh in freeMeshes)
                mesh.Dispose();
            foreach (WeakReferencedMesh occupied in occupiedMeshes)
                occupied.Item2.Dispose();
            occupiedMeshes.Clear();
        }

        public string FitText(string text, float unscaledMaxWidth, bool keepExplicitLineBreaks = false)
        {
            if (keepExplicitLineBreaks)
                return string.Join('\n', text.Split('\r', '\n').Select(l => FitText(l, unscaledMaxWidth, false)));;

            LinkedList<string> cutLines = new LinkedList<string>();
            string[] split = text.Split(new char[] { ' ', '\r', '\n', '\t', '\b' }, System.StringSplitOptions.RemoveEmptyEntries);
            int cursor = 0;
            while (cursor < split.Length)
            {
                int numWordsInThisLine = split.Length - cursor;
                TextMesh tmp = null;
                string line;
                do
                {
                    System.Text.StringBuilder txt = new System.Text.StringBuilder(text.Length + 1);
                    for (int j = cursor; j < cursor + numWordsInThisLine; j++)
                        txt.Append(split[j] + " ");
                    txt.Remove(txt.Length - 1, 1);
                    UpdateText(ref tmp, line = txt.ToString(), LayoutInfo.TOP_LEFT);
                    numWordsInThisLine--;
                } while (numWordsInThisLine > 0 && tmp.geo.geometryBounds.width > unscaledMaxWidth);
                cutLines.Add(line);
                cursor += numWordsInThisLine + 1;
            }
            System.Text.StringBuilder outTxt = new System.Text.StringBuilder(text.Length + 1 + cutLines.length);
            foreach (string line in cutLines)
                outTxt.AppendLine(line.Trim());
            if (outTxt.Length > 0)
                outTxt.Remove(outTxt.Length - 1, 1);
            return outTxt.ToString();
        }

        public float FitTextToBounds(ref TextMesh mesh, string text, LayoutInfo layout, float targetWidth, float targetHeight)
        {
            UpdateText(ref mesh, new(this, text, layout));
            float bestScale = GetScale(mesh.geo.textBounds);
            float minDivider = bestScale;

            TextMesh bestFitMesh = mesh;

            // phase 1: find an upper bound for "maxDivider"
            float maxDivider = bestScale;
            while (true)
            {
                maxDivider *= 2;
                string brokenText = FitText(text, targetWidth / maxDivider, true);
                UpdateText(ref mesh, new(this, brokenText, layout));
                float nextScale = GetScale(mesh.geo.textBounds);
                if (nextScale > bestScale)
                {
                    bestScale = nextScale;
                    bestFitMesh = mesh;
                }
                else
                    break;
            }

            // phase 2: binary search for best divider
            float attemptedDivider = (minDivider + maxDivider) * 0.5f;
            float step = (maxDivider - minDivider) * 0.5f;
            float lastScale = bestScale;

            // We know that we are done when the step direction changes,
            // but the broken text stays the same.
            string lastBrokenText = mesh.geo.text;
            bool lastStepPositive = true;

            while (true)
            {
                string brokenText = FitText(text, targetWidth / attemptedDivider, true);
                UpdateText(ref mesh, new(this, brokenText, layout));
                float nextScale = GetScale(mesh.geo.textBounds);
                if (nextScale > bestScale)
                {
                    bestScale = nextScale;
                    bestFitMesh = mesh;
                }

                bool stepPositive = nextScale < lastScale;
                // in edge cases, the step may become too small to run into the logical exit condition
                if (step < 0.001f || stepPositive ^ lastStepPositive && brokenText == lastBrokenText)
                    break;

                attemptedDivider += stepPositive ? step : -step;
                step *= 0.5f;
                lastScale = nextScale;
                lastBrokenText = brokenText;
                lastStepPositive = stepPositive;
            }

            mesh = bestFitMesh;
            return bestScale;

            float GetScale(Bounds2f bounds) => Min(targetWidth / bounds.width, targetHeight / bounds.height);
        }
    }
}
