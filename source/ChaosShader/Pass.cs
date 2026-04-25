using ChaosFramework.Collections;
using System.Text;
using Regex = System.Text.RegularExpressions.Regex;
using RegexOptions = System.Text.RegularExpressions.RegexOptions;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    using CompilationErrors;

    public sealed class Pass : ShaderComponent
    {
        private enum Parsing
        {
            Nothing,
            Symbol,
            ShaderStageAssignmentValue,
            StateSetterParameters,
        }

        static readonly Regex symbolRegex = new Regex("[a-z_][a-z0-9_]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static string ValidateAndTrimSymbol(ParseCursor location, StringBuilder currentSymbolBuilder)
        {
            string currentSymbol = currentSymbolBuilder.ToString().Trim();
            if (currentSymbol.Length == 0)
                throw new SyntaxError("Expected symbol", location);

            if (!symbolRegex.IsMatch(currentSymbol))
                throw new SyntaxError($"Invalid symbol '{currentSymbol}'", location);

            return currentSymbol;
        }

        static void AssignShaderStage(
            ParseCursor i,
            ref string shaderStageImplementation,
            StringBuilder currentSymbol,
            StringBuilder currentValue,
            out Parsing state
            )
        {
            if (shaderStageImplementation != null)
                throw new SyntaxError($"Illegal reassignment {currentSymbol}.", i);
            else
            {
                shaderStageImplementation = currentValue.ToString();
                currentSymbol.Clear();
                currentValue.Clear();
                state = Parsing.Nothing;
            }
        }

        internal static Pass Parse(ref ParseCursor i, string identifier)
        {
            int braceCount = 1;
            LinkedList<RenderState> renderStates = new LinkedList<RenderState>();
            string vertexShader = null;
            string fragmentShader = null;

            StringBuilder currentSymbolBuilder = new StringBuilder();
            StringBuilder currentValue = new StringBuilder();
            string currentSymbol = string.Empty;
            ParseCursor symbolStart = i;

            Parsing state = Parsing.Nothing;
            while (i++)
            {
                if (i == '}')
                {
                    if (--braceCount == 0)
                        if (state != Parsing.Nothing)
                            throw new SyntaxError("Premature end of pass.", i);
                        else
                            break;
                }

                switch (state)
                {
                    case Parsing.Nothing:
                        if (!char.IsWhiteSpace(i))
                        {
                            symbolStart = i;
                            goto case Parsing.Symbol;
                        }
                        break;

                    case Parsing.Symbol:
                        switch (i)
                        {
                            case '(':
                                currentSymbol = ValidateAndTrimSymbol(symbolStart, currentSymbolBuilder);
                                state = Parsing.StateSetterParameters;
                                break;

                            case '=':
                                currentSymbol = ValidateAndTrimSymbol(symbolStart, currentSymbolBuilder);
                                state = Parsing.ShaderStageAssignmentValue;
                                break;

                            case ';':
                                throw new SyntaxError("Unexpected end of statement.", i);

                            default:
                                currentSymbolBuilder.Append(i.currentChar);
                                break;
                        }
                        break;

                    case Parsing.StateSetterParameters:
                        if (i == ')')
                        {
                            renderStates.Add(new RenderState(currentSymbol, currentValue.ToString()));
                            currentSymbolBuilder.Clear();
                            currentValue.Clear();
                            state = Parsing.Nothing;

                            while (++i && char.IsWhiteSpace(i))
                                ; // skip white spaces

                            if (i != ';')
                                throw new SyntaxError("';' expected.", i);
                        }
                        else
                            currentValue.Append(i.currentChar);
                        break;

                    case Parsing.ShaderStageAssignmentValue:
                        for (; i && i != ';'; ++i)
                            currentValue.Append(i.currentChar);

                        switch (currentSymbol)
                        {
                            case "FragmentShader":
                                AssignShaderStage(i, ref fragmentShader, currentSymbolBuilder, currentValue, out state);
                                break;

                            case "VertexShader":
                                AssignShaderStage(i, ref vertexShader, currentSymbolBuilder, currentValue, out state);
                                break;

                            default:
                                throw new SyntaxError($"Unknown shader stage '{currentValue}'.", i);
                        }
                        break;
                }
            }

            return new Pass(identifier, fragmentShader, vertexShader, renderStates, null);
        }

        protected override bool needsDispose => false;

        public string name;
        public string[] vertexShaderFuncs;
        public string[] fragmentShaderFuncs;
        public Shader.SemanticMapping semanticMapping;
        public int? shaderModel;

        internal LinkedList<RenderState> renderStates = new LinkedList<RenderState>();

        public string vertexShader { get; private set; }
        public string fragmentShader { get; private set; }


        public Pass(string name, string fragmentShader, string vertexShader, LinkedList<RenderState> renderStates, int? shaderModel)
        {
            this.name = name;
            this.vertexShader = vertexShader;
            this.fragmentShader = fragmentShader;
            this.renderStates = renderStates;
            this.shaderModel = shaderModel;
            SplitShaders();
        }

        public void SetShaders(string newVertexShader, string newFragmentShader)
        {
            vertexShader = newVertexShader;
            fragmentShader = newFragmentShader;
            SplitShaders();
        }

        void SplitShaders()
        {
            vertexShaderFuncs = vertexShader.Split(',');
            for (int i = 0; i < vertexShaderFuncs.Length; i++)
                vertexShaderFuncs[i] = vertexShaderFuncs[i].Trim();

            fragmentShaderFuncs = fragmentShader.Split(',');
            for (int i = 0; i < fragmentShaderFuncs.Length; i++)
                fragmentShaderFuncs[i] = fragmentShaderFuncs[i].Trim();
        }

        public override void WriteCode(StringBuilder str, bool untransformed = true)
        {
            str.Append("\tPass " + name + " {\n");
            foreach (RenderState renderState in renderStates)
                renderState.WriteCode(str);

            str.Append("\t\tFragmentShader = " + fragmentShader + ";\n");
            str.Append("\t\tVertexShader = " + vertexShader + ";\n");
            str.Append("\t}\n");
        }

        public override ShaderComponent Clone()
        {
            LinkedList<RenderState> stateClones = new LinkedList<RenderState>();
            foreach (RenderState x in renderStates)
                stateClones.Add((RenderState)x.Clone());

            return new Pass(name, fragmentShader, vertexShader, stateClones, shaderModel);
        }

        public override bool Equals(object obj)
        {
            Pass p = obj as Pass;
            if (p == null)
                return false;

            if (p.name != name
             || p.fragmentShader != fragmentShader
             || p.vertexShader != vertexShader
             || p.renderStates.length != renderStates.length
                )
                return false;

            foreach (RenderState s1 in renderStates)
            {
                foreach (RenderState s2 in p.renderStates)
                    if (s1.Equals(s2))
                        goto match_found;
                return false;
            match_found:;
            }
            return true;
        }

        public override int GetHashCode()
            => name.GetHashCode();

        public override string ToString()
            => $"{GetType()}{{ {name} }}";
    }
}
