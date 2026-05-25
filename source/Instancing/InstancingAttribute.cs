using ChaosFramework.Collections;
using System;
using System.Linq;
using ChaosUtil.Primitives;

namespace ChaosFramework.Graphics.OpenGl.Instancing
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public abstract class InstancingAttribute : Attribute
    {
        static bool FilterArgForHashCode(object arg)
            => arg != null && (arg.GetType().IsPrimitive || arg is string);

        public MatrixInstancer informer { get; protected set; }
        public int maxInstances { get; internal set; }
        public InstancingManager creator { get; internal set; }

        public T Context<T>() => GenericCreator<T>().context;
        public InstancingManager<T> GenericCreator<T>() => (InstancingManager<T>)creator;

        // TODO: RENAME
        internal object[] creationParameters;

        [ChaosAnalyzers.ClassIntegrity.ExplicitConstructor(false)]
        public InstancingAttribute() { }

        public InstancingAttribute(int maxInstances, params object[] creationParameters)
            : this()
        {
            this.maxInstances = maxInstances;
            this.creationParameters = creationParameters;
        }

        public object GetCreationParam(int i) => creationParameters[i];
        public abstract void SetDrawCalls();
        public abstract void Initialize(Graphics graphics, int maxInstances, params object[] parameters);

        public virtual void Reset()
            => informer?.Reset();

        public virtual void Dispose()
            => informer?.Dispose();

        public override bool Equals(object other)
            => Equals(other as InstancingAttribute);

        public bool Equals(InstancingAttribute other)
        {
            if (other?.GetType() != GetType())
                return false;

            if ((creationParameters == null || creationParameters.Length == 0)
              ^ (other.creationParameters == null || other.creationParameters.Length == 0))
                return false;

            if (creationParameters != null && other.creationParameters != null)
            {
                if (other.creationParameters.Length != creationParameters.Length)
                    return false;

                if (!other.creationParameters.CompareValueEqualityRecursive(creationParameters))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
            => creationParameters != null
            ? ChaosUtil.Primitives.HashCode.Combine(GetType(), ChaosUtil.Primitives.HashCode.Combine(creationParameters.Where(FilterArgForHashCode)))
            : GetType().GetHashCode();

        public static bool operator ==(InstancingAttribute a, InstancingAttribute b)
            => a?.Equals(b) ?? (object)b == null;

        public static bool operator !=(InstancingAttribute a, InstancingAttribute b)
            => !(a?.Equals(b) ?? (object)b == null);
    }
}
