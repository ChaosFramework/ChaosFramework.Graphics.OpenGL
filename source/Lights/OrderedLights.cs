using SysCol = System.Collections.Generic;
using ChaosFramework.Collections;
using Type = System.Type;
using ChaosFramework.Core;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    internal partial class OrderedLights
        : Disposable
    {
        readonly SysCol.Dictionary<Type, AdvancedLinkedList<LightWithScreenCoverage>> lightsByType
            = new SysCol.Dictionary<Type, AdvancedLinkedList<LightWithScreenCoverage>>();

        readonly LightSet guide;
        readonly Camera camera;

        public OrderedLights(LightSet guide, Camera camera)
        {
            this.guide = guide;
            this.camera = camera;
            guide.Added += Add;
            guide.Removed += Remove;

            guide.Fill(Add);
        }

        void Add(Light l)
        {
            Type lightType = l.GetType();
            AdvancedLinkedList<LightWithScreenCoverage> lights;
            if (!lightsByType.TryGetValue(lightType, out lights))
                lightsByType[lightType] = lights = new AdvancedLinkedList<LightWithScreenCoverage>();

            lights.AddSorted(new LightWithScreenCoverage(this, l));
        }

        void Remove(Light l)
        {
            if (l.alive)
            {
                AdvancedLinkedList<LightWithScreenCoverage> lights;
                if (!lightsByType.TryGetValue(l.GetType(), out lights))
                    return;

                foreach (LightWithScreenCoverage existing in lights)
                    if (existing.light == l)
                    {
                        lights.RemoveCurrent();
                        break;
                    }
            }
        }

        public void Update()
        {
            foreach (AdvancedLinkedList<LightWithScreenCoverage> list in lightsByType.Values)
                foreach (LightWithScreenCoverage l in list)
                    l.Update();

            foreach (AdvancedLinkedList<LightWithScreenCoverage> l in lightsByType.Values)
                l.Sort<LightWithScreenCoverage>();
        }

        public SysCol.IEnumerable<Light> EnumerateLights(Type t)
        {
            AdvancedLinkedList<LightWithScreenCoverage> list;
            if (lightsByType.TryGetValue(t, out list))
            {
                foreach (LightWithScreenCoverage l in list)
                {
                    if (!l.light.alive)
                    {
                        guide.Remove(l.light);
                        list.RemoveCurrent();
                    }
                    else
                        yield return l.light;
                }
            }
        }

        public SysCol.IEnumerable<Type> EnumerateTypes()
            => lightsByType.Keys;

        protected override void DoDispose()
        {
            base.DoDispose();
            guide.Added -= Add;
            guide.Removed -= Remove;
        }
    }
}
