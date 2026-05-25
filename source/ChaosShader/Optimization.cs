using ChaosFramework.Collections;
using System.Text;
using static ChaosFramework.Math.Clamping;
using Regex = System.Text.RegularExpressions.Regex;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    public static class Optimization
    {
        enum CommentParserState
        {
            Parsing,
            LineComment,
            InnerComment,
        }

        public static string Optimize(CodeBlock fx)
        {
            string code = "";
            do
            {
                StringBuilder str = new StringBuilder();
                fx.WriteCode(str);

                // spaces so split spits out more entries even if searched signature is at beginning or end
                code = " " + str.ToString() + " ";
            } while (KillUnneededSignatures(code, fx.variables) ||
                     KillUnneededSignatures(code, fx.methods));

            return code.Trim();
        }

        public static bool KillUnneededSignatures<T>(string code, LinkedList<T> lst)
            where T : Signature
        {
            bool hasKilledSomething = false;
            foreach (T obj in lst)
            {
                string name = obj.name;
                if (name.Contains("[")) // array declarations are weird
                {
                    int open = 0, close = 0;
                    for (; open < name.Length && name[open] != '['; open++) ;
                    for (close = open; close < name.Length && name[close] != ']'; close++) ;
                    name = name.Remove(open, close - open + 1);
                }

                if (SplitText(code, name, false).Length == 2) // the signature exists exactly once (at its declaration), so kill it
                {
                    lst.Remove(obj);
                    hasKilledSomething = true;
                }
            }

            return hasKilledSomething;
        }

        internal static string[] SplitText(string text, string separator, bool removeBlankEntries = true)
        {
            string[] split = Regex.Split(text, separator);

            if (!removeBlankEntries)
                return split;

            LinkedList<string> splitLst = new LinkedList<string>();
            foreach (string str in split)
                if (Regex.Replace(str, "\\s", "") != "")
                    splitLst.Add(str);

            return splitLst.ToArray();
        }

        public static string KillComments(string code)
        {
            code += " ";
            CommentParserState state = CommentParserState.Parsing;
            char[] newCode = new char[code.Length];
            int newCodeCursor = 0;
            for (int i = 0; i < code.Length; i++)
            {
                switch (state)
                {
                    case CommentParserState.Parsing:
                        if (code[i] == '/')
                        {
                            if (code[i + 1] == '/')
                                state = CommentParserState.LineComment;
                            else if (code[i + 1] == '*')
                                state = CommentParserState.InnerComment;
                            else
                            {
                                newCode[newCodeCursor++] = code[i];
                                i--;
                            }
                            i++;
                        }
                        else
                            newCode[newCodeCursor++] = code[i];
                        break;

                    case CommentParserState.LineComment:
                        if (code[i] == '\n')
                        {
                            state = CommentParserState.Parsing;
                            newCode[newCodeCursor++] = code[i];
                        }
                        break;

                    case CommentParserState.InnerComment:
                        if (code[i] == '*' && code[i + 1] == '/')
                        {
                            state = CommentParserState.Parsing;
                            i++;
                        }
                        break;
                }
            }

            return new string(newCode, 0, Clamp(0, newCode.Length, newCodeCursor - 1));
        }
    }
}
