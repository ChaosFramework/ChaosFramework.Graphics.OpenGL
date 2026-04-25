using ChaosFramework.Collections;
using System.Text;
using Regex = System.Text.RegularExpressions.Regex;
using System;
using System.Linq;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    using CompilationErrors;

    public sealed class Method : Signature
    {
        static string ParameterToStringWithSemantics(ShaderParam parameter)
            => parameter.semantic != null
            ? $"{ParameterToString(parameter)} : {parameter.semantic}"
            : ParameterToString(parameter);

        static string ParameterToString(ShaderParam parameter)
            => $"{(parameter.isOut ? (parameter.isIn ? "inout " : "out ") : "")}{parameter.type} {parameter.name}";

        internal static Method Parse(ref ParseCursor i, CodeBlock.ParserState parserState)
        {
            ParseCursor delcarationStart = i;

            StringBuilder parameters = new StringBuilder();

            int signatureI = i;

            for (; i && i != ')'; ++i)
                parameters.Append(i.currentChar);

            StringBuilder semantic = new StringBuilder();
            while (i++)
                if (i == ':')
                {
                    i++;
                    while (i && char.IsWhiteSpace(i++)) ; // skip white spaces

                    while (i && !char.IsWhiteSpace(i++))
                        semantic.Append(i.currentChar);

                    while (i && char.IsWhiteSpace(i++)) ; // skip white spaces

                    if (i != '{')
                        throw new SyntaxError("'{' expected", i);

                    break;
                }
                else if (i == '{')
                    break;

            if (!i++)
                throw new SyntaxError("unexpected EOF", i);

            StringBuilder bodyBuilder = new StringBuilder();
            i.ReadToBraceEnd(1, bodyBuilder);

            return new Method(
                parserState.currentSyntaxKind.ToString(),
                parserState.currentIdentifier.ToString(),
                parameters.ToString(),
                bodyBuilder.ToString(),
                semantic.ToString(),
                delcarationStart
                );
        }

        internal ShaderParam[] parameters = { };
        internal string body;
        internal string semantic;

        protected override bool needsDispose => false;

        public Method(string type, string name, string parameters, string body, string semantic = null)
            : this(type, name, parameters, body, semantic, default(ParseCursor))
        { }

        internal Method(string type, string name, string parameters, string body, string semantic, ParseCursor location)
            : base(type, name)
        {
            if (semantic != null && semantic.Trim() != "")
                this.semantic = semantic;

            parameters = Regex.Replace(parameters, "\\s", " ");
            try
            {
                this.body = body;
                string[] split = parameters.Split(',');

                if (split.Length == 1 && split[0] == "")
                    return;

                LinkedList<ShaderParam> paramsLst = new LinkedList<ShaderParam>();
                foreach (string parameter in split)
                {
                    string[] paramStr = parameter.Trim().Split(':');
                    paramStr[0] = paramStr[0].Trim();
                    ShaderParam param = new ShaderParam();

                    if (paramStr[0].StartsWith("inout "))
                        param.isIn = param.isOut = true;
                    else if (paramStr[0].StartsWith("in "))
                        param.isIn = !(param.isOut = false);
                    else if (paramStr[0].StartsWith("out "))
                        param.isIn = !(param.isOut = true);

                    if (param.isIn || param.isOut)
                        paramStr[0] = paramStr[0].Split(new char[] { ' ' }, 2)[1];
                    else
                        param.isIn = true;

                    string[] type_name = paramStr[0].Trim().Split(' ');
                    if (type_name.Length != 2)
                        throw new SyntaxError($"'{parameter}' is not a valid parameter declaration", location);

                    param.type = type_name[0].Trim();
                    param.name = type_name[1].Trim();
                    if (paramStr.Length > 1)
                        param.semantic = paramStr[1].Trim();

                    paramsLst.Add(param);
                }
                this.parameters = paramsLst.ToArray();
            }
            catch (Exception ex)
            {
                throw new ParserError($"Could not construct method {name}", location, ex);
            }
        }

        private Method(string type, string name)
            : base(type, name)
        { }

        public void WriteSignature(StringBuilder str, bool addSemantics)
        {
            Func<ShaderParam, string> parameterToString = addSemantics
                ? (Func<ShaderParam, string>)ParameterToStringWithSemantics
                : ParameterToString;

            str.Append($"{type} {name}(");
            str.Append(string.Join(", ", parameters.Select(parameterToString)));
            str.Append(")");
            if (semantic != null && addSemantics)
                str.Append(" : " + semantic);
        }

        public override void WriteCode(StringBuilder str, bool addSemantics)
        {
            WriteSignature(str, addSemantics);
            str.Append("\n{\n");
            str.Append(body);
            str.Append("}\n");
        }

        public override ShaderComponent Clone()
        {
            Method output = new Method(type, name);
            output.body = body;
            output.parameters = new ShaderParam[parameters.Length];
            for (int i = 0; i < output.parameters.Length; i++)
                output.parameters[i] = parameters[i];

            return output;
        }

        public void Override(Method newMethod)
        {
            if (newMethod == null)
                throw new ArgumentException("argument must not be null");

            if (parameters.Length != newMethod.parameters.Length)
                throw new InvalidOperationException(
                    $"Method {newMethod.name} does not match signature of {name}(invalid number of parameters)"
                    );

            for (int i = 0; i < parameters.Length; i++)
                if (newMethod.parameters[i].type != parameters[i].type || newMethod.parameters[i].semantic != parameters[i].semantic)
                    throw new InvalidOperationException($"Method {newMethod.name} does not match signature of {name}");

            parameters = newMethod.parameters;
            body = newMethod.body;
        }

        public bool WriteReferenceTree(AdvancedLinkedList<Method> allMethods, StringBuilder builder)
        {
            const string NON_WORD = "[\\W]";
            const string PREFIX = ".*" + NON_WORD;
            const string SUFFIX = NON_WORD + ".*";

            string invokeString = name;
            foreach (Method indirect in allMethods)
                if (System.Text.RegularExpressions.Regex.IsMatch(body, $"{PREFIX}{indirect.name}{SUFFIX}"))
                {
                    indirect.WriteReferenceTree(allMethods, builder);
                    allMethods.RemoveCurrent();
                }

            if (allMethods.Contains(this))
                WriteCode(builder, false);

            return false;
        }
    }
}
