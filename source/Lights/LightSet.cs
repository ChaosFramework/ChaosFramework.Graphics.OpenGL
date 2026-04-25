using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public class LightSet
    {
        public delegate void LightAction(Light l);

        readonly SysCol.HashSet<Light> lights = new SysCol.HashSet<Light>();

        public event LightAction Added;
        public event LightAction Removed;

        public void Add(Light l)
        {
            if (lights.Add(l))
                Added?.Invoke(l);
        }

        public void Remove(Light l)
        {
            if (lights.Remove(l))
                Removed?.Invoke(l);
        }

        public void Fill(LightAction add)
        {
            foreach (Light l in lights)
                add(l);
        }
    }
}
