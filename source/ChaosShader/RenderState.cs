using ChaosUtil.Reflection;
using ChaosUtil.Serialization.Text;
using System;
using System.Reflection;
using System.Text;

namespace ChaosFramework.Graphics.OpenGl.ChaosShader
{
    using CompilationErrors;

    public sealed class RenderState : ShaderComponent
    {
        public string func;
        public string args;

        GlStateTracker.RenderStateChange change;

        protected override bool needsDispose => false;

        public RenderState(string func, string args)
        {
            this.func = func;
            this.args = args;
        }

        public void GetActions(Graphics graphics, out Action setAction, out Action unsetAction)
        {
            string[] argsSplit = args.Split(',');
            foreach (Type t in AssemblyManager.SubTypesOf(typeof(GlStateTracker.RenderStateChange)))
                if (t.Name == func)
                {
                    Type interfaceType = null;
                    foreach (Type tmp in t.GetInterfaces())
                        if (typeof(GlStateTracker.RenderStateChange).IsAssignableFrom(tmp) && tmp.IsGenericType)
                            interfaceType = tmp;

                    Type argType = interfaceType.GetGenericArguments()[0];
                    object arg;
                    Type genericTupleType;

                    if (argType.IsGenericType &&
                        ((genericTupleType = argType.GetGenericTypeDefinition()) == typeof(Tuple<>)
                      || genericTupleType == typeof(Tuple<,>)
                      || genericTupleType == typeof(Tuple<,,>)
                      || genericTupleType == typeof(Tuple<,,,>)
                      || genericTupleType == typeof(Tuple<,,,,>)
                      || genericTupleType == typeof(Tuple<,,,,,>)
                      || genericTupleType == typeof(Tuple<,,,,,,>)
                      || genericTupleType == typeof(Tuple<,,,,,,,>)
                        )
                       )
                    {
                        Type[] @params = argType.GetGenericArguments();
                        object[] tupleArgs = new object[@params.Length];
                        if (@params.Length != argsSplit.Length)
                            throw new CompilationError(
                                $"Incorrect number of arguments for {func}. Expected {@params.Length}, but invocation was 'func ({args})'"
                                );

                        for (int i = 0; i < @params.Length; i++)
                        {
                            object[] parseArgs = new object[] { argsSplit[i].Trim(), null };
                            Delegate @delegate;
                            if (Parse.TryGetParser(@params[i], out @delegate) && (bool)@delegate.DynamicInvoke(parseArgs))
                                tupleArgs[i] = parseArgs[1];
                            else
                                throw new CompilationError($"Invalid argument '{parseArgs[0]}' in {func}({args})");
                        }
                        arg = Activator.CreateInstance(argType, tupleArgs);
                    }
                    else
                    {
                        object[] parseArgs = new object[] { argsSplit[0].Trim(), null };
                        Delegate a;
                        if (Parse.TryGetParser(argType, out a) && (bool)a.DynamicInvoke(parseArgs))
                            arg = parseArgs[1];
                        else
                            throw new CompilationError($"Invalid argument '{parseArgs[0]}' in {func}({args})");
                    }

                    PropertyInfo currentValueProperty = t.GetProperty(nameof(GlStateTracker.RenderStateChange<Type>.currentValue));
                    PropertyInfo oldValueProperty = t.GetProperty(nameof(GlStateTracker.RenderStateChange<Type>.oldValue));
                    change = (GlStateTracker.RenderStateChange)Activator.CreateInstance(t);
                    currentValueProperty.SetValue(change, arg, null);
                    setAction = () =>
                    {
                        change.Query();
                        change.SetState();
                    };
                    unsetAction = change.ResetSate;
                    return;
                }

            throw new CompilationError($"No State action called {func} found.");
        }

        public override void WriteCode(StringBuilder str, bool untransformed = true)
            => str.Append($"\t\t{func}(" + args + ");\n");

        public override ShaderComponent Clone()
            => new RenderState(func, args);

        public override bool Equals(object obj)
        {
            RenderState state = obj as RenderState;
            if (state == null)
                return false;

            if (func != state.func) return false;
            if (args != state.args) return false;

            return true;
        }
        public override int GetHashCode()
            => func.GetHashCode();
    }
}
