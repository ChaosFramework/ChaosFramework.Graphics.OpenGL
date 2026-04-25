using ChaosFramework.IO.Containers;
using ChaosFramework.IO.Streams;
using System.IO;

namespace ChaosFramework.Graphics.OpenGl.AssetContainers
{
    public class MaterialContainer
        : AssetContainer<Material>
    {
        readonly TextureContainer source;
        readonly Graphics graphics;

        public MaterialContainer(StreamSource streamSource, Graphics graphics, TextureContainer source, bool monitoring = true)
            : base(streamSource, monitoring)
        {
            this.graphics = graphics;
            this.source = source;
        }

        protected override void DisposeItem(Material obj)
            => obj?.Dispose();

        protected override Material LoadFromStream(Key key, Stream resource, CancellationToken cancel)
            => new Material(source, resource, key.key);
    }
}
