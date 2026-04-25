namespace ChaosFramework.Graphics.OpenGl
{
    public interface Transparent
    {
        void PrepareVertices();

        /// <summary>
        ///     If the used shader is different from <see cref="TransparencyRenderer.maskEffect" />,
        ///     this is expected to call <see cref="TransparencyRenderer.SetMaskRenderingValues(ChaosShader.Shader)"/> to function properly.
        /// </summary>
        void DrawMask(TransparencyRenderer renderer);

        void DrawTransparent(TransparencyRenderer renderer);
    }
}
