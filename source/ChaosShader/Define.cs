using System.Text;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    using CompilationErrors;

    public class Define : Signature
    {
        internal static Define Parse(ref ParseCursor i)
        {
            ParseCursor startOfDefine = i;

            StringBuilder defineTokenBuilder = new StringBuilder();
            while (i && !char.IsWhiteSpace(i))
                defineTokenBuilder.Append(i++.currentChar);

            string defineToken = defineTokenBuilder.ToString().Trim();
            if (defineToken.ToLower() != "#define")
                throw new SyntaxError($"Unexpected token '{defineToken}'.", startOfDefine);

            for (; char.IsWhiteSpace(i); ++i)
                if (i == '\n')
                    throw new SyntaxError("Expected define name.", i);

            StringBuilder defineName = new StringBuilder();
            for (; i && !char.IsWhiteSpace(i); ++i)
                defineName.Append(i.currentChar);

            for (; char.IsWhiteSpace(i); ++i)
                if (i == '\n')
                    return new Define(defineName.ToString(), "");

            StringBuilder defineValue = new StringBuilder();
            for (; i && i != '\n'; ++i)
                defineValue.Append(i.currentChar);

            return new Define(defineName.ToString(), defineValue.ToString());
        }

        protected override bool needsDispose => false;

        public Define(string define, string value)
            : base(define, value)
        { }

        public override void WriteCode(StringBuilder builder, bool untransformed = true)
            => builder.Append("#define " + type + " " + name + "\n");

        public override ShaderComponent Clone()
            => new Define(type, name);
    }
}
