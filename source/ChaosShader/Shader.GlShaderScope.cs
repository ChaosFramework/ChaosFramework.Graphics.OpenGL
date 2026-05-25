using OpenTK.Graphics.OpenGL;
using System;
using System.Text;
using System.Linq;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    using CompilationErrors;

    partial class Shader
    {
        class GlShaderScope : IDisposable
        {
            string error;
            string code;
            bool failed;
            public readonly int shader;
            public GlShaderScope(StringBuilder allErrors, ShaderType kind, string code)
            {
                this.code = code;

                shader = GL.CreateShader(kind);
                GL.ShaderSource(shader, code);
                Graphics.ThrowErrors();
                GL.CompileShader(shader);
                Graphics.ThrowErrors();
                int compileStatus;
                error = GL.GetShaderInfoLog(shader);
                GL.GetShader(shader, ShaderParameter.CompileStatus, out compileStatus);
                Graphics.ThrowErrors();
                failed = compileStatus == 0;
#if IGNORE_SHADER_WARNINGS
                if (failed)
#endif
                    if (error != "")
                        allErrors.Append($"{kind} Warning:\n\n{error}");
            }

            public GlShaderScope AssertSuccess(TranspiledPass tp)
            {
                if (failed)
                {
                    CompilationError e = new CompilationError($"Could not compile Pixel Shader in Pass '{tp.pass.name}'.\n{error}");
                    e.Data.Add("Transpiled Code", string.Join("\r\n",
                                    code
                                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select((x, i) => $"({i}) {x}")
                                    ));
                    throw e;
                }

                return this;
            }

            void IDisposable.Dispose()
            {
                GL.DeleteShader(shader);
                Graphics.ThrowErrors();
            }
        }
    }
}
