using Exception = System.Exception;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader.CompilationErrors
{
    public class ParserError : CompilationError
    {
        readonly ParseCursor location;

        internal ParserError(string exceptionString, ParseCursor location, Exception innerException = null)
            : base($"{exceptionString} \n\n[{location.line}]: {location.currentLine}", innerException)
        {
            this.location = location;
        }
    }
}
