using Exception = System.Exception;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader.CompilationErrors
{
    public class CompilationError : Exception
    {
        public CompilationError(string message = null, Exception innerException = null)
            : base(message ?? "Failed to compile.", innerException)
        { }
    }
}
