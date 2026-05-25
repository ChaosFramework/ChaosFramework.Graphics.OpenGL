using ChaosFramework.Collections;
using System.Text;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    using CompilationErrors;

    partial class CodeBlock
    {
        internal class ParserState
        {
            public SyntaxKind state = SyntaxKind.Undetermined;

            public StringBuilder currentSyntaxKind = new StringBuilder();
            public StringBuilder currentIdentifier = new StringBuilder();
            public StringBuilder currentValue = new StringBuilder();
            public StringBuilder currentSemantic = new StringBuilder();

            public Modifier currentModifier;
            public Modifier tmpModifier;

            public void Reset()
            {
                currentSyntaxKind.Clear();
                currentIdentifier.Clear();
                currentValue.Clear();
                currentSemantic.Clear();
                currentModifier = default(Modifier);
                tmpModifier = default(Modifier);
                state = SyntaxKind.Undetermined;
            }
        }

        internal enum SyntaxKind
        {
            Undetermined,
            Identifier,
            VariableAssignment,
            VariableSemantic,
            FunctionSignature,
            Pass,
        }

        void Parse(ref ParseCursor i)
        {
            ParserState parserState = new ParserState();
            while (++i)
            {
                switch (parserState.state)
                {
                    case SyntaxKind.Undetermined:
                        DetermineNextSyntaxKind(ref i, parserState);
                        break;

                    case SyntaxKind.Identifier:
                        DetermineIdentifierResult(ref i, parserState);
                        break;

                    case SyntaxKind.VariableAssignment:
                        HandleVariableAssignment(ref i, parserState);
                        break;

                    case SyntaxKind.VariableSemantic:
                        if (i == ';')
                        {
                            variables.last.semantic = parserState.currentSemantic.ToString().Trim();
                            parserState.Reset();
                        }
                        else
                            parserState.currentSemantic.Append(i.currentChar);
                        break;

                    case SyntaxKind.FunctionSignature:
                        methods.Add(Method.Parse(ref i, parserState));
                        parserState.Reset();
                        break;

                    case SyntaxKind.Pass:
                        AddPass(Pass.Parse(ref i, parserState.currentIdentifier.ToString().Trim()));
                        parserState.Reset();
                        break;
                }
            }
        }

        void DetermineNextSyntaxKind(ref ParseCursor i, ParserState parserState)
        {
            if (char.IsWhiteSpace(i))
            {
                if (parserState.currentSyntaxKind.Length > 0)
                {
                    parserState.state = SyntaxKind.Identifier;
                    string potentialKeyword = parserState.currentSyntaxKind.ToString().Trim();
                    switch (potentialKeyword)
                    {
                        case "import":
                            AddImport(ref i, parserState, imports);
                            break;
                        case "expand":
                            AddImport(ref i, parserState, expands);
                            break;
                        default:
                            if (modifiers.TryGetValue(potentialKeyword, out parserState.tmpModifier))
                            {
                                parserState.currentModifier = parserState.tmpModifier;
                                parserState.currentSyntaxKind.Clear();
                                parserState.state = SyntaxKind.Undetermined;
                            }
                            break;
                    }
                }
            }
            else if (i == '#')
            {
                AddDefine(Define.Parse(ref i));
                parserState.Reset();
            }
            else
                parserState.currentSyntaxKind.Append(i.currentChar);
        }

        void AddImport(ref ParseCursor i, ParserState parserState, LinkedList<string> imports)
        {
            StringBuilder importBuilder = new StringBuilder();

            bool needBlank = false;
            while (i && i++)
                switch (i)
                {
                    case '\n':
                    case '\r':
                    case '\t':
                    case ' ':
                        if (needBlank)
                        {
                            importBuilder.Append(' ');
                            needBlank = false;
                        }
                        break;
                    case ';':
                        goto loopbreak;
                    default:
                        importBuilder.Append(i.currentChar);
                        needBlank = true;
                        break;
                }
            loopbreak:

            if (!i)
                throw new SyntaxError("unexpected EOF", i);

            imports.Add(importBuilder.ToString());

            parserState.Reset();
        }

        void DetermineIdentifierResult(ref ParseCursor i, ParserState parserState)
        {
            switch (i)
            {
                case ';':
                    variables.Add(new Variable(parserState));
                    parserState.Reset();
                    break;

                case ',':
                    variables.Add(new Variable(parserState));
                    parserState.currentIdentifier.Clear();
                    break;

                case ':':
                case '=':
                    variables.Add(new Variable(parserState));
                    parserState.currentIdentifier.Clear();
                    parserState.state = i == '=' ? SyntaxKind.VariableAssignment : SyntaxKind.VariableSemantic;
                    break;

                case '(':
                    parserState.state = SyntaxKind.FunctionSignature;
                    return; // skip currentModifier reset

                case '{':
                    if (parserState.currentSyntaxKind.ToString().Trim().ToLower() == "pass")
                        parserState.state = SyntaxKind.Pass;
                    else
                        throw new SyntaxError("Unexpected token '{'.", i);
                    break;

                default:
                    parserState.currentIdentifier.Append(i.currentChar);
                    return; // skip currentModifier reset
            }

            parserState.currentModifier = default(Modifier);
        }

        void HandleVariableAssignment(ref ParseCursor i, ParserState parserState)
        {
            switch (i)
            {
                case ':':
                case ';':
                    variables.last.value = parserState.currentValue.ToString().Trim();
                    parserState.currentValue.Clear();
                    if (i == ':')
                        parserState.state = SyntaxKind.VariableSemantic;
                    else
                        parserState.Reset();
                    break;

                case '{':
                    for (; i < i.code.Length; i++)
                    {
                        parserState.currentValue.Append(i.currentChar);
                        if (i == '}')
                            break;
                    }
                    break;

                default:
                    parserState.currentValue.Append(i.currentChar);
                    break;
            }
        }
    }
}
