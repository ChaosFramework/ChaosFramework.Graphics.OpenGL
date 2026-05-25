using ChaosFramework.Graphics.OpenGl.ChaosShader;
using ChaosFramework.Core;
using ChaosFramework.IO.Containers;
using ChaosFramework.IO.Streams;

namespace ChaosFramework.Graphics.OpenGl.AssetContainers
{
    public class ShaderCodeContainer
        : AssetContainer<CodeBlock>
    {
        public ShaderCodeContainer(StreamSource streamSource)
            : base(streamSource, false)
        { }

        protected override void DisposeItem(CodeBlock obj)
            => obj?.Dispose();

        protected override CodeBlock LoadFromStream(Key key, System.IO.Stream resource, CancellationToken cancel)
        {
            byte[] bytes = new byte[resource.Length];
            resource.Read(bytes, 0, bytes.Length);
            return new CodeBlock(this, System.Text.Encoding.ASCII.GetString(bytes));
        }

        public override Entry Load(string key, Disposable monitor1, Disposable[] monitors)
            => Entry.Mock((_, __) => (CodeBlock)base.Load(key, monitor1, monitors).content.Clone(), _ => { });
    }
}
