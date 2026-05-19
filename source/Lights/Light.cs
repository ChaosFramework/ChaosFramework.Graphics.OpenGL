using ChaosFramework.Collections;
using ChaosFramework.Core;
using ChaosFramework.Graphics.Colors;
using System.Linq;
using System.Reflection;
using Exception = System.Exception;
using SysCol = System.Collections.Generic;
using Type = System.Type;

namespace ChaosFramework.Graphics.OpenGl.Lights
{
    public abstract class Light : Disposable
    {
        static Light()
        {
            SysCol.HashSet<string> resources = new SysCol.HashSet<string>();
            foreach (PropertyInfo resource in typeof(Properties.Resources).GetProperties(BindingFlags.NonPublic | BindingFlags.Static))
                if (resource.Name.StartsWith("FX_") && resource.Name.ToLower().Contains("light"))
                    resources.Add(resource.Name);

            using (new AccessScope<SysCol.HashSet<string>>(resources))
            {
                SysCol.IEnumerable<Exception> missingShaderExceptions
                    = Assembly.GetAssembly(typeof(Light)).GetTypes().Where(IsMissingShader).Select(CreateMissingShaderException);
                if (!missingShaderExceptions.Empty())
                    throw new System.AggregateException(
                        "Missing light shaders.",
                        missingShaderExceptions.ToArray()
                        );
            }
        }

        static bool IsMissingShader(Type lightType)
            => !lightType.IsAbstract
            && lightType.IsSubclassOf(typeof(Light))
            && !AccessScope<SysCol.HashSet<string>>.current.Contains($"FX_{lightType.Name}");

        static Exception CreateMissingShaderException(Type lightType)
            => new Exception($"{lightType.Name} has no matching shader resource.");

        public Rgba color;
        public bool masked = false;

        protected internal Rgba premultipliedColor
            => new Rgba(color.rgb * color.a, color.a);

        public abstract bool CheckVisible(Camera view);

        public abstract float EstimatedScreenCoverage(Camera view);

        public virtual void Update() { }
    }
}
