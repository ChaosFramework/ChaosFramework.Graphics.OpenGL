using ChaosFramework.Shapes;
using ChaosFramework.Core;
using ChaosFramework.IO.Containers;
using ChaosFramework.IO.Streams;

namespace ChaosFramework.Graphics.OpenGl.AssetContainers
{
    using Model;

    public class MeshContainer
        : ParameterizedAssetContainer<Mesh, MeshLoadFlags>
    {
        readonly Dispatcher dispatcher;

        public override MeshLoadFlags defaultParameter => MeshLoadFlags.Default;

        public MeshContainer(StreamSource streamSource, Dispatcher dispatcher, bool monitoring = true, bool backgroundLoading = false)
            : base(streamSource, monitoring, backgroundLoading)
        {
            this.dispatcher = dispatcher;
        }

        protected override void DisposeItem(Mesh obj)
            => obj?.Dispose();

        protected override Mesh LoadFromStream(Key key, System.IO.Stream str, CancellationToken cancel)
        {
            switch (((ParameterizedKey)key).param)
            {
                case MeshLoadFlags.Default:
                    return new Mesh(dispatcher, MeshData.FromStream(str));
                case MeshLoadFlags.Animated:
                    return new Mesh(dispatcher, AnimatedMeshData.FromStream(str));
                default:
                    return null;
            }
        }

        public override void AddFactory(string key, Factory content)
            => AddFactory(new ParameterizedKey(key, defaultParameter), content);
    }
}
