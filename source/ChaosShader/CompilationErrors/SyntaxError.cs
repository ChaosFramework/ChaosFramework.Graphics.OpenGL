namespace ChaosFramework.Graphics.OpenGl.ChaosShader.CompilationErrors
{
    public class SyntaxError : CompilationError
    {
        readonly ParseCursor location;

        internal SyntaxError(string exceptionString, ParseCursor location)
            : base($"{exceptionString}\n\n{location.lineDisplayString}")
        {
            this.location = location;
        }
    }
}
