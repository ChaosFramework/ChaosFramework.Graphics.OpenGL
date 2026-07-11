using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Math.Vectors;
using OpenTK.Graphics.OpenGL;

namespace ChaosFramework.Graphics.OpenGl
{
    public static class Sprite
    {
        public static string textureHandle = "tex";
        public static Vector3f[] verts = new Vector3f[] {
            new Vector3f(-1, 1, 0),
            new Vector3f(1, 1, 0),
            new Vector3f(1, -1, 0),
            new Vector3f(-1, -1, 0)
        };

        public static void DrawFullscreen(Graphics graphics)
        {
            GL.BindVertexArray(graphics.emptyVAO);
            Graphics.ThrowErrors();
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            Graphics.ThrowErrors();
        }

        public static void DrawFullscreen(Graphics graphics, Texture tex)
        {
            graphics.shaders.spriteEffect.SetValue("tex", tex);
            graphics.shaders.spriteEffect.BeginPass("Screen");
            DrawFullscreen(graphics);
            graphics.shaders.spriteEffect.EndPass();
        }

        public static void DrawPositionOnly(Graphics graphics)
        {
            GL.BindVertexArray(graphics.emptyVAO);
            Graphics.ThrowErrors();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            Graphics.ThrowErrors();
        }

        public static void DrawPositionInstanced(Graphics graphics, Shader effect, MatrixInstancer instancer, string pass)
        {
            if (instancer.numInstances == 0)
                return;

            Shader.SemanticMapping mapping = effect.BeginPass(pass);
            GL.BindVertexArray(graphics.emptyVAO);
            Graphics.ThrowErrors();
            instancer.Bind(mapping);
            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, instancer.numInstances);
            Graphics.ThrowErrors();
            instancer.Unbind(mapping);
            effect.EndPass();
        }

        public static void DrawPositionTextured(Graphics graphics)
        {
            GL.BindVertexArray(graphics.emptyVAO);
            Graphics.ThrowErrors();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            Graphics.ThrowErrors();
        }

        public static void DrawPositionTextured(Graphics graphics, Shader shader, string pass = "Sprite")
        {
            shader.BeginPass(pass);
            GL.BindVertexArray(graphics.emptyVAO);
            Graphics.ThrowErrors();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            Graphics.ThrowErrors();
            shader.EndPass();
        }

        public static void DrawSpriteMesh(Graphics graphics, Shader effect, string pass)
            => graphics.meshes.Load("$Sprite", graphics).content.Draw(effect, pass);

        public static void DrawSpriteMeshInstanced(Graphics graphics, Shader effect, MatrixInstancer instancer, string pass)
            => graphics.meshes.Load("$Sprite", graphics).content.DrawInstanced(effect, pass, instancer);

        public static uint[] CreateQuadIndices(int numQuads)
        {
            uint[] indices = new uint[numQuads * 6];
            for (uint i = 0; i < numQuads; i++)
            {
                indices[i * 6 + 0] = i * 4;
                indices[i * 6 + 1] = i * 4 + 1;
                indices[i * 6 + 2] = i * 4 + 2;

                indices[i * 6 + 3] = i * 4 + 1;
                indices[i * 6 + 4] = i * 4 + 3;
                indices[i * 6 + 5] = i * 4 + 2;
            }

            return indices;
        }
    }
}
