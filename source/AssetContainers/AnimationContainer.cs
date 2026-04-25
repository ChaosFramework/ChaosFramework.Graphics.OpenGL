using ChaosFramework.IO.Containers;
using ChaosFramework.IO.Streams;
using SysCol = System.Collections.Generic;
using ChaosFramework.Shapes.Rigging;

namespace ChaosFramework.Graphics.OpenGl.AssetContainers
{
    public class AnimationContainer
        : AssetContainer<SysCol.Dictionary<string, Animation>>
    {
        public AnimationContainer(StreamSource streamSource, bool monitoring)
            : base(streamSource, monitoring)
        { }

        protected override void DisposeItem(SysCol.Dictionary<string, Animation> obj) { }

        protected override SysCol.Dictionary<string, Animation> LoadFromStream(Key key, System.IO.Stream resource, CancellationToken cancel)
            => Animation.FromStream(resource);
    }
}
