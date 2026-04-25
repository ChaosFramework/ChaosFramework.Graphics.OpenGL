using ChaosFramework.Graphics.OpenGl.Model;
using ChaosFramework.Graphics.Text;

namespace ChaosFramework.Graphics.OpenGl.Text
{
    using ChaosShader;

    public class TextMesh
    {
        readonly Font font;
        internal readonly Mesh mesh;
        public readonly TextGeometry geo;

        internal TextMesh(Font font, MeshBuffers buffers, TextGeometry geo)
        {
            this.font = font;
            this.geo = geo;
            mesh = new Mesh(geo.meshData, buffers);
        }

        ~TextMesh()
        {
            lock (font.bufferedGeos)
            {
                font.bufferedGeos.Remove(geo.args);
            }
        }

        public void DrawText(Shader shader, string pass)
        {
            if (mesh != null && geo.numPrintedChars != 0)
                mesh.Draw(shader, pass);
        }
    }
}
