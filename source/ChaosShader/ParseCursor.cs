using System.Text;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    using CompilationErrors;

    internal struct ParseCursor
    {
        public static ParseCursor operator ++(ParseCursor @this)
        {
            @this.Increment();
            return @this;
        }

        public static implicit operator char(ParseCursor c)
            => c.currentChar;

        public static implicit operator bool(ParseCursor c)
            => c.cursor < c.code.Length;

        public readonly string code;

        int cursor;
        int lineNumber;
        int lineStart;
        char _currentChar;

        public int line => lineNumber;
        public int column => cursor - lineStart;
        public char currentChar => _currentChar;

        public string currentLine
        {
            get
            {
                int end = code.IndexOf('\n', lineStart);
                return code.Substring(lineStart, end < 0 ? code.Length : end - lineStart);
            }
        }

        public string lineDisplayString => $"[{line}] {currentLine}";

        public ParseCursor(string code)
        {
            this.code = code;
            _currentChar = '\0';
            cursor = -1;
            lineNumber = 0;
            lineStart = 0;
        }

        public int GetColumn(int cursor)
            => cursor - lineStart;

        public void ReadToBraceEnd(int openBraces, StringBuilder bldr)
        {
            int bracesToClose = openBraces;
            while (this)
            {
                Increment();
                if (this == '}')
                {
                    bracesToClose--;
                    if (bracesToClose == 0)
                        return;
                }
                else if (this == '{')
                    bracesToClose++;

                bldr?.Append(currentChar);
            }

            throw new SyntaxError("'}' expected", this);
        }

        void Increment()
        {
            cursor++;
            _currentChar = this ? code[cursor] : '\0';
            switch (currentChar)
            {
                case '\n':
                    lineNumber++;
                    lineStart = cursor;
                    break;

                case '\r':
                    lineStart = cursor;
                    Increment();
                    break;

                default:
                    break;
            }
        }

    }
}
