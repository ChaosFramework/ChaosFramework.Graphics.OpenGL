using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosUtil.Reflection;
using System;
using System.Linq;

namespace ChaosFramework.Graphics.OpenGl.Instancing
{
    public interface InstanceManaged { }

    public interface Instancable : InstanceManaged
    {
        bool NeedsInstancedDraw();
        void GiveMeInstances(InstancingAttribute[] instancers);
    }

    public abstract class InstancingManager
        : Disposable
    { }

    public class InstancingManager<Context> : InstancingManager
    {
        static internal Tuple<Type, InstancingAttribute[]>[] allTypeInstancers
            = AssemblyManager.EnumerateRelevantAssemblies()
                .SelectMany(ass => ass.GetTypes())
                .Where(t => typeof(InstanceManaged).IsAssignableFrom(t))
                .Select(t => new Tuple<Type, InstancingAttribute[]>(t, t.GetAttributes<InstancingAttribute>(true)))
                .Where(tuple => tuple.Item2.NotEmpty())
                .ToArray();

        public readonly InstancingManagerContainer<Context> parent;

        public LinkedList<Instancable> instancedThings = new LinkedList<Instancable>();
        public InstancingAttribute[] instancers;

        public Context context => parent.context;

        public InstancingManager(InstancingManagerContainer<Context> parent)
        {
            this.parent = parent;
        }

        public InstancingAttribute GetOrCreateAttr(InstancingAttribute buildTimeAttr)
        {
            InstancingAttribute runtimeAttr;
            if (!parent.classToRuntimeInstancers.TryGetValue(buildTimeAttr, out runtimeAttr))
            {
                runtimeAttr = (InstancingAttribute)Activator.CreateInstance(buildTimeAttr.GetType());
                runtimeAttr.creationParameters = buildTimeAttr.creationParameters;
                runtimeAttr.maxInstances = buildTimeAttr.maxInstances;
                runtimeAttr.creator = this;
                runtimeAttr.Initialize(parent.graphics, buildTimeAttr.maxInstances, buildTimeAttr.creationParameters);
                parent.allRuntimeInstancers.Add(runtimeAttr);
                parent.classToRuntimeInstancers[buildTimeAttr] = runtimeAttr;

                Array.Resize(ref instancers, instancers.Length + 1);
                instancers[instancers.Length - 1] = runtimeAttr;
            }
            else
            {
                if (Array.IndexOf(instancers, runtimeAttr) < 0)
                {
                    Array.Resize(ref instancers, instancers.Length + 1);
                    instancers[instancers.Length - 1] = runtimeAttr;
                }
            }

            return runtimeAttr;
        }
    }
}
