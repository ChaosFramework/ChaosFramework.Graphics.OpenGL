using ChaosUtil.Serialization.Text;
using System.Text;
using Type = System.Type;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    using CompilationErrors;

    public sealed class Variable : Signature
    {
        public string value { get; internal set; }
        public string semantic { get; internal set; }
        public int arraySize { get; private set; }
        public CodeBlock.Modifier modifier { get; private set; }

        protected override bool needsDispose => false;

        public Variable(CodeBlock.Modifier modifier, string type, string name, string value, string semantic) : base(type, name)
        {
            this.modifier = modifier;
            string[] nameSplit = name.Trim().Split('[', ']');
            if (nameSplit.Length == 2)
            {
                arraySize = int.Parse(nameSplit[1].Substring(0, nameSplit[1].Length - 1));
                name = nameSplit[0];
            }
            this.value = value;
            this.semantic = semantic;
        }

        internal Variable(CodeBlock.ParserState parserState)
            : this(parserState.currentModifier,
                   parserState.currentSyntaxKind.ToString().Trim(),
                   parserState.currentIdentifier.ToString().Trim(),
                   null,
                   null
                  )
        { }

        public override void WriteCode(StringBuilder builder, bool addSemantics = true)
        {
            if (modifier.value != null)
                builder.Append($"{modifier.value} ");

            builder.Append($"{type} {name}");

            if (value != null && modifier.value == "const")
                builder.Append($" = {value}");

            if (semantic != null && addSemantics)
                builder.Append($" : {semantic};\n");
            else
                builder.Append(";\n");
        }

        public override ShaderComponent Clone()
            => new Variable(modifier, type, name, value, semantic);

        public Type GetFieldType()
        {
            Type t;
            if (Shader.typeByName.TryGetValue(type, out t))
                return t;

            throw new CompilationError($"Unknown type '{type}'.");
        }

        public object GetInitialValue()
        {
            string workingSet = value;
            if (value.StartsWith("vec"))
            {
                string[] split = value.Split('(', ')');
                if (split.Length < 2)
                    throw new CompilationError($"Invalid variable initialization. Could not parse '{value}' to '{type}' '{name}'.");

                workingSet = split[1];
            }

            object[] result = new object[] { workingSet, null };
            if ((bool)Parse.GetParser(GetFieldType()).DynamicInvoke(result))
                return result[1];
            else
                throw new CompilationError($"Invalid variable initialization. Could not parse '{value}' to '{type}' '{name}'.");
        }
    }
}
