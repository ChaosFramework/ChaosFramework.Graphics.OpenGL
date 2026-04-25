using ChaosFramework.Collections;
using System.Text;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    using CompilationErrors;

    using SemanticTransition = SysCol.Dictionary<string, int>;

    partial class Shader
    {
        string BuildFragmentShaderMain(Pass pass, SemanticTransition vsToFsLayoutLocations)
        {
            int version = pass.shaderModel ?? defaultShaderModel;

            StringBuilder outBuilder = new StringBuilder();
            StringBuilder callBuilder = new StringBuilder();
            StringBuilder baseBuilder = new StringBuilder();
            LinkedList<Method> shaderFuncs = new LinkedList<Method>();
            SysCol.Dictionary<string, string> inSemantics = new SysCol.Dictionary<string, string>();
            SysCol.Dictionary<string, string> outSemantics = new SysCol.Dictionary<string, string>();

            foreach (string fragmentShaderFunc in pass.fragmentShaderFuncs)
            {
                Method meth = GetMethod(fragmentShaderFunc);
                if (meth == null)
                    throw new CompilationError($"Method '{fragmentShaderFunc}' not found");

                shaderFuncs.Add(meth);
                callBuilder.Append("\t");
                callBuilder.Append(fragmentShaderFunc);
                callBuilder.Append("(");

                int numArgs = 0;
                foreach (ShaderParam param in meth.parameters)
                {
                    if (param.isIn)
                    {
                        string paramType;
                        if (inSemantics.TryGetValue(param.semantic, out paramType))
                            if (paramType != param.type)
                                throw new CompilationError($"Incompatible redeclaration of in semantic {param.semantic}.");

                        inSemantics[param.semantic] = param.type;
                        callBuilder.Append($"fs_{param.semantic}, ");
                    }
                    else if (param.isOut && param.semantic != "gl_FragDepth")
                    {
                        string paramType;
                        if (outSemantics.TryGetValue(param.semantic, out paramType))
                            if (paramType != param.type)
                                throw new CompilationError($"Incompatible redeclaration of out semantic {param.semantic}.");

                        outSemantics[param.semantic] = param.type;

                        if (param.semantic.StartsWith("COLOR"))
                        {
                            int streamOffset = int.Parse(param.semantic.Substring("COLOR".Length));
                            outBuilder.Append($"layout(location = ");
                            outBuilder.Append(streamOffset);
                            outBuilder.Append(") out ");
                            outBuilder.Append(param.type);
                            outBuilder.Append(" ");
                            outBuilder.Append(param.semantic);
                            outBuilder.Append("; ");
                        }
                        else
                            callBuilder.Append("fs_");

                        callBuilder.Append(param.semantic);
                        callBuilder.Append(", ");
                    }

                    ++numArgs;
                }

                if (numArgs > 0)
                    callBuilder.Remove(callBuilder.Length - 2, 2);

                callBuilder.AppendLine(");");
            }

            string inBuilderString = BuildFsInSemantics(vsToFsLayoutLocations, inSemantics, version);
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

            return ProcessDefines($"{baseBuilder}\nvoid main() {{\n{callBuilder}}}");
        }

        string BuildFsInSemantics(SemanticTransition vsToFsLayoutLocations, SysCol.Dictionary<string, string> inSemantics, int version)
        {
            StringBuilder inBuilder = new StringBuilder();
            foreach (SysCol.KeyValuePair<string, string> inSemantic in inSemantics)
            {
                int semanticLocation;
                if (vsToFsLayoutLocations.TryGetValue(inSemantic.Key, out semanticLocation))
                {
                    if (version >= (int)VERSION_REQ.VS_OUT_LOCATION)
                        inBuilder.Append($"layout(location = {semanticLocation}) ");

                    inBuilder.Append("in ");
                }
                inBuilder.AppendLine($"{inSemantic.Value} fs_{inSemantic.Key};");
            }
            return inBuilder.ToString().TrimEnd();
        }
    }
}
