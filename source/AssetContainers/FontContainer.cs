using ChaosFramework.IO.Containers;
using ChaosFramework.IO.Streams;

namespace ChaosFramework.Graphics.OpenGl.AssetContainers
{
    using Text;

    public class FontContainer
        : AssetContainer<Font>
    {
        internal readonly Graphics graphics;

        public FontContainer(StreamSource streamSource, Graphics graphics, bool monitoring, bool backgroundLoading = false)
            : base(
                  streamSource,
                  monitoring,
                  backgroundLoading,
                  backgroundLoading ? _ => new Font(graphics) : (Factory)null
                  )
        {
            this.graphics = graphics;
        }

        protected override Font LoadFromStream(Key key, System.IO.Stream resource, CancellationToken cancel)
        {
            System.IO.BinaryReader rd = new System.IO.BinaryReader(resource);
            return new Font(graphics, rd);
        }

        protected override void DisposeItem(Font obj)
            => obj?.Dispose();
    }
}
