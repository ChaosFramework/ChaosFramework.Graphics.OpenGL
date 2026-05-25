using ChaosFramework.Core;
using ChaosFramework.IO.Containers;
using ChaosFramework.IO.Streams;

namespace ChaosFramework.Graphics.OpenGl.AssetContainers
{
    public class TextureContainer : AssetContainer<Texture>
    {
        static Texture CreateDefault(Dispatcher dispatcher)
        {
            using (System.IO.Stream str = new System.IO.MemoryStream(Properties.Resources.Tex_PlaceHolder))
                return Texture.FromStream(dispatcher, str);
        }

        // TODO: add more options, such as minimal and maximum texture bounds to be applied for technical limitations
        public float textureScale { get; private set; } = 1.0f;

        readonly Dispatcher dispatcher;

        public TextureContainer(
            StreamSource streamSource,
            Dispatcher dispatcher,
            bool monitoring = true,
            bool backgroundLoading = false,
            float textureScale = 1.0f
            )
            : base(
                  streamSource,
                  monitoring,
                  backgroundLoading,
                  _ => CreateDefault(dispatcher)
                  )
        {
            this.dispatcher = dispatcher;
            this.textureScale = textureScale;
        }

        public void RefreshContent(float textureScale)
        {
            this.textureScale = textureScale;
            base.RefreshContent();
        }

        protected override void DisposeItem(Texture obj)
            => obj?.Dispose();

        protected override Texture LoadFromStream(Key key, System.IO.Stream str, CancellationToken cancel)
            => Texture.FromStream(dispatcher, str, textureScale);
    }
}
