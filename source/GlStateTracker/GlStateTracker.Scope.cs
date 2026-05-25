using ChaosFramework.Collections;
using ChaosFramework.Core;

namespace ChaosFramework.Graphics.OpenGl
{
    partial class GlStateTracker
    {
        class Scope : Disposable
        {
            internal readonly AdvancedLinkedList<RenderStateChange> changeLog
                = new AdvancedLinkedList<RenderStateChange>();

            readonly GlStateTracker parent;

            public Scope(GlStateTracker parent)
            {
                this.parent = parent;

                if (parent.currentScopes.Count > short.MaxValue - 2)
                    throw new System.InvalidOperationException(
                        $"Not more than {short.MaxValue - 1} trackers allowed."
                        + "Make sure you close your trackers when you don't need them anymore."
                        );

                parent.currentScopes.Push(this);
            }

            internal void ResetRenderStates()
            {
                changeLog.SetEnumerator(changeLog.length - 1, -changeLog.length);
                foreach (RenderStateChange stat in changeLog)
                    stat.ResetSate();

                changeLog.Clear();
            }

            protected override void DoDispose()
            {
                base.DoDispose();
                ResetRenderStates();
                Scope popped = parent.currentScopes.Pop();
                System.Diagnostics.Debug.Assert(popped == this);
            }
        }
    }
}
