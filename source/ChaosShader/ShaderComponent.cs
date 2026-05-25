using System.Text;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    public abstract class ShaderComponent : Core.Disposable
    {
        /* every ShaderComponent type may have a static Parse method that
         * - takes any number of arguments needed
         * - must only throw ParserError exceptions if any
         * - must take a parsing cursor as input and modify it so it points to the end of the parsed ShaderComponent afterwards
         * - must return the parsed component or throw an exception (as specified above)
         */

        public abstract ShaderComponent Clone();
        public abstract void WriteCode(StringBuilder builder, bool addSemantics = true);
    }
}
