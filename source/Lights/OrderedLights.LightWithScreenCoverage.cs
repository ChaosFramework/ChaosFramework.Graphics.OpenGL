namespace ChaosFramework.Graphics.OpenGl.Lights
{
    partial class OrderedLights
    {
        class LightWithScreenCoverage : System.IComparable<LightWithScreenCoverage>
        {
            public readonly OrderedLights parent;
            public readonly Light light;

            float coverage = float.NegativeInfinity;

            public LightWithScreenCoverage(OrderedLights parent, Light light)
            {
                this.parent = parent;
                this.light = light;
            }

            public void Update()
            {
                light.Update();
                coverage = light.EstimatedScreenCoverage(parent.camera);
            }

            int System.IComparable<LightWithScreenCoverage>.CompareTo(LightWithScreenCoverage other)
                => coverage.CompareTo(other.coverage);
        }
    }
}
