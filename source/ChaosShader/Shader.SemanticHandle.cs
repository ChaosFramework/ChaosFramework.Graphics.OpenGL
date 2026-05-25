using ChaosFramework.Collections;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    partial class Shader
    {
        public class SemanticHandle
        {
            public readonly Shader fx;
            public readonly string semantic;

            internal LinkedList<string> variableNames
            {
                get
                {
                    LinkedList<string> names;
                    return fx.semanticToField.TryGetValue(semantic, out names) ? names : new LinkedList<string>();
                }
            }

            internal SemanticHandle(Shader fx, string semantic)
            {
                this.fx = fx;
                this.semantic = semantic;
            }
        }
    }
}
