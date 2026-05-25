namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    public class TranspiledPass
    {
        public readonly Shader shader;
        public readonly Pass pass;
        public readonly string vertexShaderCode;
        public readonly string fragmentShaderCode;

        public TranspiledPass(Shader shader, Pass pass, string vertexShaderCode, string fragmentShaderCode)
        {
            this.shader = shader;
            this.pass = pass;
            this.vertexShaderCode = vertexShaderCode;
            this.fragmentShaderCode = fragmentShaderCode;
        }
    }
}
