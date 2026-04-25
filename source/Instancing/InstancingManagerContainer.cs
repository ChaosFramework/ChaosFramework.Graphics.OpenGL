using System;
using System.Collections;
using ChaosFramework.Collections;
using ChaosFramework.Core;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.Instancing
{
    public class InstancingManagerContainer<Context> : Disposable, SysCol.IEnumerable<InstancingManager<Context>>
    {
        public delegate bool TypeVerifier(Type t);

        readonly SysCol.Dictionary<Type, InstancingManager<Context>> managers
            = new SysCol.Dictionary<Type, InstancingManager<Context>>();

        internal readonly SysCol.Dictionary<InstancingAttribute, InstancingAttribute> classToRuntimeInstancers
            = new SysCol.Dictionary<InstancingAttribute, InstancingAttribute>();

        internal readonly LinkedList<InstancingAttribute> allRuntimeInstancers = new LinkedList<InstancingAttribute>();
        internal readonly Context context;
        internal readonly Graphics graphics;

        public InstancingManagerContainer(Context context, Graphics graphics)
        {
            this.context = context;
            this.graphics = graphics;
        }

        public InstancingManager<Context> this[Type t] => managers[t];

        public bool TryGetValue(Type renderableType, out InstancingManager<Context> result)
            => managers.TryGetValue(renderableType, out result);

        public void CreateInstancers(params Type[] targetTypes)
        {
            foreach (Tuple<Type, InstancingAttribute[]> renderableInfo in InstancingManager<Context>.allTypeInstancers)
                if (!managers.ContainsKey(renderableInfo.Item1))
                    foreach (Type t in targetTypes)
                        if (t.IsAssignableFrom(renderableInfo.Item1))
                        {
                            CreateManager(renderableInfo);
                            break;
                        }
        }

        public void CreateInstancers(params System.Reflection.Assembly[] targetAssemblies)
        {
            foreach (Tuple<Type, InstancingAttribute[]> renderableInfo in InstancingManager<Context>.allTypeInstancers)
                if (!managers.ContainsKey(renderableInfo.Item1) && Array.IndexOf(targetAssemblies, renderableInfo.Item1.Assembly) >= 0)
                    CreateManager(renderableInfo);
        }

        public void CreateInstancers(TypeVerifier verifyType)
        {
            foreach (Tuple<Type, InstancingAttribute[]> renderableInfo in InstancingManager<Context>.allTypeInstancers)
                if (!managers.ContainsKey(renderableInfo.Item1) && verifyType(renderableInfo.Item1))
                    CreateManager(renderableInfo);
        }

        void CreateManager(Tuple<Type, InstancingAttribute[]> renderableInfo)
        {
            InstancingManager<Context> mgr = GetOrCreateManager(renderableInfo.Item1);
            foreach (InstancingAttribute attr in renderableInfo.Item2)
                mgr.GetOrCreateAttr(attr);
        }

        public InstancingManager<Context> GetOrCreateManager(Type renderableType)
        {
            InstancingManager<Context> typeInstancer;
            if (!managers.TryGetValue(renderableType, out typeInstancer))
            {
                typeInstancer = (InstancingManager<Context>)Activator.CreateInstance(typeof(InstancingManager<Context>), this);
                typeInstancer.instancers = new InstancingAttribute[0];
                managers[renderableType] = typeInstancer;
            }

            return typeInstancer;
        }

        public void SetDrawCalls(LinkedList<Action> resetInstancerLayer, LinkedList<Action> fillInstancerLayer)
        {
            foreach (InstancingAttribute instancer in allRuntimeInstancers)
                instancer.SetDrawCalls();

            resetInstancerLayer.Add(ResetInstancers);
            fillInstancerLayer.Add(FillInstancers);
        }

        void ResetInstancers()
        {
            foreach (InstancingAttribute instancer in allRuntimeInstancers)
                instancer.Reset();
        }

        void FillInstancers()
        {
            ChaosUtil.Debug.MeasurementLog.StartMeasure(
                nameof(Instancable.GiveMeInstances),
                new ChaosUtil.Debug.MeasurementLog.CustomAttribute("context", typeof(Context).ToString())
                );

            foreach (SysCol.KeyValuePair<Type, InstancingManager<Context>> manager in managers)
            {
                ChaosUtil.Debug.MeasurementLog.StartMeasure(
                    nameof(Instancable.GiveMeInstances),
                    new ChaosUtil.Debug.MeasurementLog.CustomAttribute("type", manager.Key.ToString())
                    );
                foreach (Instancable instance in manager.Value.instancedThings)
                    if (instance.NeedsInstancedDraw())
                    {
                        ChaosUtil.Debug.MeasurementLog.StartMeasure(
                            nameof(Instancable.GiveMeInstances),
                            new ChaosUtil.Debug.MeasurementLog.CustomAttribute("instance", instance.ToString())
                            );
                        instance.GiveMeInstances(manager.Value.instancers);
                        ChaosUtil.Debug.MeasurementLog.EndMeasure();
                    }
                ChaosUtil.Debug.MeasurementLog.EndMeasure();
            }

            ChaosUtil.Debug.MeasurementLog.EndMeasure();
        }

        public SysCol.IEnumerable<InstancingAttribute> EnumerateInstancers()
        {
            foreach(InstancingAttribute attr in allRuntimeInstancers)
                yield return attr;
        }

        SysCol.IEnumerator<InstancingManager<Context>> SysCol.IEnumerable<InstancingManager<Context>>.GetEnumerator()
            => managers.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((SysCol.IEnumerable<InstancingManager<Context>>)this).GetEnumerator();

        protected override void DoDispose()
        {
            base.DoDispose();
            foreach (InstancingManager<Context> instancer in this)
                instancer.Dispose();
            managers.Clear();
            foreach (InstancingAttribute instancer in classToRuntimeInstancers.Values)
                instancer.Dispose();
            classToRuntimeInstancers.Clear();
        }
    }
}
