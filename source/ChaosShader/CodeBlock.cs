using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Collections;
using System.Linq;
using System.Text;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    using CompilationErrors;

    static class AddToFrontOrBackExtension
    {
        internal static void AddToFrontOrBack<T>(this LinkedList<T> list, T value, bool front)
        {
            if (front)
                list.Insert(0, value);
            else
                list.Add(value);
        }
    }

    public partial class CodeBlock : ShaderComponent
    {
        public struct Modifier
        {
            public string value;
        }

        internal static SysCol.Dictionary<string, Modifier> modifiers = new SysCol.Dictionary<string, Modifier>();
        static CodeBlock()
        {
            modifiers["const"] = new Modifier { value = "const" };
            modifiers["in"] = new Modifier { value = "in" };
        }

        protected override bool needsDispose => false;

        internal AdvancedLinkedList<Method> methods = new AdvancedLinkedList<Method>();
        internal AdvancedLinkedList<Define> defines = new AdvancedLinkedList<Define>();
        internal AdvancedLinkedList<Variable> variables = new AdvancedLinkedList<Variable>();
        internal AdvancedLinkedList<Pass> passes = new AdvancedLinkedList<Pass>();
        internal readonly ShaderCodeContainer importSource;
        LinkedList<string> imports = new LinkedList<string>(), expands = new LinkedList<string>();
        bool resolved = false;

        public CodeBlock(ShaderCodeContainer importSource)
        {
            this.importSource = importSource;
        }

        public CodeBlock(ShaderCodeContainer importSource, string unprocessedCode)
                : this(importSource)
        {
            ParseCursor i = new ParseCursor(Optimization.KillComments(unprocessedCode));
            try
            {
                Parse(ref i);
            }
            catch (CompilationError)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                throw new ParserError("Unknown error while parsing shader code.", i, ex);
            }
        }

        public void ResolveImports()
        {
            if (resolved)
                return;

            string[] importClone = imports.ToArray();

            foreach (string expand in expands)
                Expand(GetAndResolve(expand));

            foreach (string import in importClone)
            {
                LinkedList<ShaderComponent> reversedShaderComponents = new LinkedList<ShaderComponent>();
                foreach (ShaderComponent c in GetAndResolve(import))
                    reversedShaderComponents.Insert(0, c);

                Expand(reversedShaderComponents, true);
            }

            resolved = true;
        }

        SysCol.IEnumerable<ShaderComponent> GetAndResolve(string importStatement)
        {
            string file = importStatement;
            SysCol.HashSet<string> allowedSignatureNames = new SysCol.HashSet<string>();

            int startOfFrom = importStatement.IndexOf(" from ");
            bool simpleImport = startOfFrom < 0;

            bool importDefines = simpleImport;
            bool importVariables = simpleImport;
            bool importMethods = simpleImport;
            bool importPasses = simpleImport;

            if (!simpleImport)
            {
                file = importStatement.Substring(startOfFrom + " from ".Length).Trim();
                string[] filterEntries = importStatement
                                        .Substring(0, startOfFrom)
                                        .Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                                        ;

                foreach (string importFilter in filterEntries)
                {
                    switch (importFilter.Trim())
                    {
                        case "*": importPasses = importMethods = importVariables = importDefines = true; break;
                        case "*Passes": importPasses = true; break;
                        case "*Methods": importMethods = true; break;
                        case "*Variables": importVariables = true; break;
                        case "*Defines": importDefines = true; break;
                        default:
                            allowedSignatureNames.Add(importFilter.Trim());
                            break;
                    }
                }
            }

            CodeBlock importedCode = importSource?.Load(file, this);
            if (importedCode == null)
                throw new System.IO.FileNotFoundException("Could not find import '" + file + "'");

            importedCode.ResolveImports();

            bool importIndividual = allowedSignatureNames.Count > 0;
            if (importDefines)
                foreach (ShaderComponent sig in importedCode.GetDefines())
                    yield return sig;
            else if (importIndividual)
                foreach (Signature sig in importedCode.GetDefines())
                    if (allowedSignatureNames.Remove(sig.type))
                        yield return sig;

            if (importMethods)
                foreach (ShaderComponent sig in importedCode.GetMethods())
                    yield return sig;
            else if (importIndividual)
                foreach (Signature sig in importedCode.GetMethods())
                    if (allowedSignatureNames.Remove(sig.name))
                        yield return sig;

            if (importVariables)
                foreach (ShaderComponent sig in importedCode.GetVariables())
                    yield return sig;
            else if (importIndividual)
                foreach (Signature sig in importedCode.GetVariables())
                    if (allowedSignatureNames.Remove(sig.name.Split('[')[0])) // import array variables without needing to know length
                        yield return sig;

            if (importPasses)
                foreach (ShaderComponent sig in importedCode.passes)
                    yield return sig;
            else if (importIndividual)
                foreach (Pass sig in importedCode.passes)
                    if (allowedSignatureNames.Remove(sig.name))
                        yield return sig;

            if (allowedSignatureNames.Count > 0)
            {
                StringBuilder error = new StringBuilder("The following signatures could not be imported:\n");
                foreach (string sig in allowedSignatureNames)
                {
                    error.Append("\t");
                    error.AppendLine(sig);
                }
                throw new CompilationError(error.ToString());
            }
        }

        internal void Clear()
        {
            methods.Clear();
            passes.Clear();
            variables.Clear();
            defines.Clear();
            imports.Clear();
        }

        public void Expand(CodeBlock codeBlock)
        {
            foreach (Define d in codeBlock.defines)
                AddDefine((Define)d.Clone());

            foreach (Method m in codeBlock.methods)
                AddMethod((Method)m.Clone());

            foreach (Variable v in codeBlock.variables)
                AddVariable((Variable)v.Clone());

            foreach (Pass p in codeBlock.passes)
                AddPass((Pass)p.Clone());
        }

        public void Expand(SysCol.IEnumerable<ShaderComponent> signatures, bool front = false)
        {
            foreach (ShaderComponent signature in signatures)
                Expand(signature, front);
        }

        public void Expand(ShaderComponent signature, bool front = false)
        {
            System.Type t = signature.GetType();
            if (t == typeof(Variable))
                AddVariable((Variable)signature, front);
            else if (t == typeof(Method))
                AddMethod((Method)signature, front);
            else if (t == typeof(Define))
                AddDefine((Define)signature, front);
            else if (t == typeof(Pass))
                AddPass((Pass)signature, front);
            else
                throw new CompilationError($"Unknown Signature Type: {t.FullName}");
        }

        public override void WriteCode(StringBuilder str, bool addSemantics = true)
        {
            foreach (Define v in defines)
                v.WriteCode(str, addSemantics);

            foreach (Variable v in variables)
                v.WriteCode(str, addSemantics);

            foreach (Method v in methods)
                v.WriteCode(str, addSemantics);

            foreach (Pass p in passes)
                p.WriteCode(str);
        }

        internal string ProcessDefines(string code)
        {
            foreach (Define define in defines)
                if (code.Contains(define.type))
                    code = code.Replace(define.type, ProcessDefines(define.name));

            return code;
        }

        public override ShaderComponent Clone()
        {
            CodeBlock output = new CodeBlock(importSource);
            CloneTo(output);
            return output;
        }

        protected internal void CloneTo(CodeBlock output)
        {
            foreach (Method m in methods)
                output.AddMethod((Method)m.Clone());

            foreach (Variable v in variables)
                output.AddVariable((Variable)v.Clone());

            foreach (Define v in defines)
                output.AddDefine((Define)v.Clone());

            foreach (Pass p in passes)
                output.AddPass((Pass)p.Clone());

            foreach (string import in imports)
                output.imports.Add(import);

            foreach (string expand in expands)
                output.imports.Add(expand);

        }

        public Pass[] GetPasses() => passes.ToArray();
        public Variable[] GetVariables() => variables.ToArray();
        public Method[] GetMethods() => methods.ToArray();
        public Define[] GetDefines() => defines.ToArray();
        public string[] GetImports() => imports.ToArray();
        public string[] GetExpands() => expands.ToArray();

        public Method GetMethod(string name)
            => methods.LastOrDefault(m => m.name == name); // last because we want to override like this

        public Variable GetVariable(string name)
            => variables.SingleOrDefault(v => v.name == name); // variables may only occur once

        public void AddMethod(Method method, bool front = false)
        {
            if (method == null)
                return;

            foreach (Method m in methods)
                if (m.name == method.name && m.type == method.type && m.parameters.Length == method.parameters.Length)
                {
                    for (int i = 0; i < m.parameters.Length; i++)
                        if (m.parameters[i].type != method.parameters[i].type)
                            goto signature_mismatch;

                    if (!front)
                    {
                        m.body = method.body;
                        m.parameters = (ShaderParam[])method.parameters.Clone();
                    }

                    return;
                signature_mismatch:;
                }

            methods.AddToFrontOrBack(method, front);
        }

        public void AddPass(Pass pass, bool front = false)
        {
            foreach (Pass p in passes)
                if (pass.name == p.name)
                {
                    if (!front)
                    {
                        p.SetShaders(pass.vertexShader, pass.fragmentShader);
                        p.shaderModel = pass.shaderModel;
                        p.renderStates = new LinkedList<RenderState>(pass.renderStates);
                    }

                    return;
                }

            passes.AddToFrontOrBack(pass, front);
        }

        public void AddDefine(Define define, bool front = false)
        {
            foreach (Define d in defines)
                if (d.type == define.type)
                {
                    if (!front)
                        d.name = define.name;

                    return;
                }

            defines.AddToFrontOrBack(define, front);
        }

        public void AddVariable(Variable variable, bool front = false)
        {
            foreach (Variable v in variables)
                if (v.name == variable.name && v.type == variable.type)
                {
                    if (!front)
                        v.value = variable.value;

                    return;
                }

            variables.AddToFrontOrBack(variable, front);
        }
    }
}
