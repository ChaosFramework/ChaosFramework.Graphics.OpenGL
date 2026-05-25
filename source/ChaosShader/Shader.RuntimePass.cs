using System;
using ChaosFramework.Collections;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    partial class Shader
    {
        class RuntimePass
        {
            public LinkedList<Tuple<Action, Action>> stateActions;
            public int programHandle;
            public SemanticMapping semanticMapping;
        }
    }
}
