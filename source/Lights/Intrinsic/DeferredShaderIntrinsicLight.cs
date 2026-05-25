using System;
using System.Linq;

namespace ChaosFramework.Graphics.OpenGl.Lights.Intrinsic
{

    public abstract class DeferredShaderIntrinsicLights<Light>
        : DeferredShaderIntrinsicLights
        where Light : Lights.Light
    {
        public class ShadeInstanceInput<Element>
            : LightShadeInput<Light>
            where Element : struct
        {
            public delegate Element Extract(Light l);

            internal readonly Element[] data;
            internal readonly Element nullValue;
            readonly Extract extract;

            internal override Array untypedData => data;

            public ShadeInstanceInput(string type, string semantic, int maxLights, Extract extract, Element nullValue)
                : base(type, semantic)
            {
                this.extract = extract;
                this.data = new Element[maxLights];
                this.nullValue = nullValue;
            }

            internal override void SetValue(Light l, int i)
            {
                data[i] = extract(l);
            }

            internal override void Reset()
            {
                for (int i = 0; i < untypedData.Length; i++)
                    data[i] = nullValue;
            }
        }

        int boundLights = 0;
        private LightShadeInput<Light>[] instanceShadeInputs;

        protected DeferredShaderIntrinsicLights(ushort maxLights)
            : base(maxLights)
        {
            instanceShadeInputs = allShadeInputs.OfType<LightShadeInput<Light>>().ToArray();
        }

        public override sealed bool AddLight(DeferredShader s, Lights.Light l)
            => AddLight(s, l as Light);

        protected virtual bool Suitable(DeferredShader s, Light l)
            => true;

        public bool AddLight(DeferredShader s, Light l)
        {
            if (l == null || boundLights >= maxLights || !Suitable(s, l))
                return false;
            else
            {
                foreach (LightShadeInput<Light> input in instanceShadeInputs)
                    input.SetValue(l, boundLights);

                boundLights++;
                return true;
            }
        }

        public override void Clear()
        {
            boundLights = 0;
            foreach (LightShadeInput<Light> input in instanceShadeInputs)
                input.Reset();
        }
    }
}
