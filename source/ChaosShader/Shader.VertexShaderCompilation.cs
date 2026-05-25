using ChaosFramework.Collections;
using System;
using System.Text;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    using CompilationErrors;

    using SemanticTransition = SysCol.Dictionary<string, int>;

    partial class Shader
    {
        static readonly string[] intrinsicVertexIn = new[] { "gl_InstanceID" };

        string BuildVertexShaderMain(Pass pass, out SemanticTransition vs_to_fs_semanticLocations)
        {
            int version = pass.shaderModel ?? defaultShaderModel;
            vs_to_fs_semanticLocations = new SemanticTransition();

            StringBuilder inBuilder = new StringBuilder();
            StringBuilder outBuilder = new StringBuilder();
            StringBuilder callBuilder = new StringBuilder();
            StringBuilder baseBuilder = new StringBuilder();

            LinkedList<Method> shaderFuncs = new LinkedList<Method>();
            LinkedList<ShaderParam> inSemantics = new LinkedList<ShaderParam>();
            LinkedList<string> parsedFuncs = new LinkedList<string>();
            foreach (string iterating in pass.vertexShaderFuncs)
            {
                string vertexShaderFunc = ProcessDefines(iterating);
                Method meth;
                int openParenthesis = vertexShaderFunc.IndexOf('(');
                if (openParenthesis >= 0)
                {
                    string macro = vertexShaderFunc.Substring(0, openParenthesis).Trim();
                    switch (macro)
                    {
                        case "PASS":
                            int closeParenthesis = vertexShaderFunc.LastIndexOf(')');
                            if (closeParenthesis < 0)
                                throw new CompilationError(
                                    $"')' expected for macro {macro} in vertex shader declaration of pass {pass.name}."
                                    );

                            string args = vertexShaderFunc.Substring(openParenthesis + 1, closeParenthesis - openParenthesis - 1);
                            string[] argsSplit = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (argsSplit.Length < 2 || argsSplit.Length > 3)
                                throw new CompilationError(
                                    $"Invalid number of arguments for macro {macro} in vertex shader declaration of pass {pass.name}.\n"
                                  + $"Expected (type inout) or (type in out)."
                                    );

                            string typeArg = argsSplit[0];
                            string arg1 = argsSplit[1];
                            string arg2 = argsSplit.Length > 2 ? argsSplit[2] : arg1;
                            AddMethod(meth = new Method(
                                "void",
                                $"_VS_MACRO_pass_{arg1}_to_{arg2}",
                                $"{typeArg} _in : {arg1}, out {typeArg} _out : {arg2}",
                                "_out = _in;"
                                ));
                            vertexShaderFunc = meth.name;
                            break;

                        default:
                            throw new CompilationError($"Invalid macro {macro} in vertex shader declaration of pass {pass.name}.");
                    }
                }
                meth = GetMethod(vertexShaderFunc);

                if (meth == null)
                    throw new CompilationError($"Vertex Shader \"{vertexShaderFunc}\" not found.");

                foreach (ShaderParam param in meth.parameters)
                    if (param.isIn)
                        inSemantics.AddUnique(param);

                parsedFuncs.Add(vertexShaderFunc);
            }

            inSemantics.Sort<ShaderParam>();
            pass.semanticMapping = SemanticMapping.GetMapping(inSemantics);

            int currentOffset = 0;
            int vs_to_fs_paramLocation = 0;
            SysCol.HashSet<string> knownStreams = new SysCol.HashSet<string>();
            foreach (string vertexShaderFunc in parsedFuncs)
            {
                int numArgs = 0;
                Method meth = GetMethod(vertexShaderFunc);
                if (meth == null)
                    throw new CompilationError($"Vertex Shader '{vertexShaderFunc}' not found");

                shaderFuncs.Add(meth);

                callBuilder.Append("\t");
                callBuilder.Append(vertexShaderFunc);
                callBuilder.Append("(");
                foreach (ShaderParam param in meth.parameters)
                {
                    if (param.isIn)
                    {
                        if (Array.IndexOf(intrinsicVertexIn, param.semantic) == -1 && knownStreams.Add(param.semantic))
                        {
                            inBuilder.AppendLine(
                                $"layout(location = {pass.semanticMapping.mapping[param.semantic]}) in {param.type} {param.semantic};"
                                );

                            currentOffset += GetNumAttribs(param.type);
                        }
                        callBuilder.Append(param.semantic + ", ");
                    }

                    if (param.isOut)
                    {
                        if (param.semantic == "POSITION" || param.semantic == "POSITION0")
                            param.semantic = "gl_Position";

                        if (param.semantic != "gl_Position")
                        {
                            if (version >= (int)VERSION_REQ.VS_OUT_LOCATION)
                                outBuilder.Append($"layout(location = {vs_to_fs_paramLocation}) ");

                            outBuilder.AppendLine($"out {param.type} fs_{param.semantic};");
                            vs_to_fs_semanticLocations[param.semantic] = vs_to_fs_paramLocation++;
                            callBuilder.Append("fs_");
                        }
                        callBuilder.Append(param.semantic + ", ");
                    }
                    numArgs++;
                }

                if (numArgs > 0)
                    callBuilder.Remove(callBuilder.Length - 2, 2);

                callBuilder.Append(");\n");
            }

            string inBuilderString = inBuilder.ToString().Trim();
            if (inBuilderString.Length > 0)
                baseBuilder.Append(inBuilderString);

            if (outBuilder.Length > 0)
            {
                baseBuilder.AppendLine();
                baseBuilder.AppendLine(outBuilder.ToString());
            }

            AdvancedLinkedList<Method> methodsClone = new AdvancedLinkedList<Method>();
            methodsClone.Add(methods);
            foreach (Method v in shaderFuncs)
                v.WriteReferenceTree(methodsClone, baseBuilder);

            return ProcessDefines(baseBuilder.ToString() + "\nvoid main() {\n" + callBuilder.ToString() + "}");
        }
    }
}
