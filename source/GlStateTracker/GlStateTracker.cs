using ChaosFramework.Core;
using ChaosFramework.Math;
using OpenTK.Graphics.OpenGL;
using GlStencilOp = OpenTK.Graphics.OpenGL.StencilOp;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl
{
    using SetEnable = System.Tuple<EnableCap, bool>;
    using SetStencilFunc = System.Tuple<StencilFunction, int, int>;
    using SetStencilOps = System.Tuple<GlStencilOp, GlStencilOp, GlStencilOp>;

    public partial class GlStateTracker
    {
        [ChaosUtil.Reflection.AssemblyManager.ListSubTypes]
        public interface RenderStateChange
        {
            void SetState();
            void ResetSate();
            void Query();
        }

        public interface RenderStateChange<Value>
            : RenderStateChange
        {
            Value oldValue { get; set; }
            Value currentValue { get; set; }
        }

        readonly Graphics graphics;
        readonly SysCol.Stack<Scope> currentScopes;
        readonly SysCol.Dictionary<FramebufferTarget, Framebuffer> boundFrameBuffers;

        public GlStateTracker(Graphics graphics)
        {
            this.graphics = graphics;
            boundFrameBuffers = new SysCol.Dictionary<FramebufferTarget, Framebuffer>();
            currentScopes = new SysCol.Stack<Scope>();

            GL.FrontFace(FrontFaceDirection.Cw);
            Graphics.ThrowErrors();

            SetRenderState<CullFace, CullFaceMode>(CullFaceMode.Back);
            SetRenderState<DepthMask, bool>(true);
            SetRenderState<DepthFunc, DepthFunction>(DepthFunction.Lequal);
            SetRenderState<Enable, SetEnable>(new SetEnable(EnableCap.DepthTest, false));
            SetRenderState<Enable, SetEnable>(new SetEnable(EnableCap.Blend, false));
            SetRenderState<Enable, SetEnable>(new SetEnable(EnableCap.CullFace, true));
            GL.ClampColor(ClampColorTarget.ClampReadColor, ClampColorMode.False);
            Graphics.ThrowErrors();
        }

        public Disposable Start()
            => new Scope(this);

        public Framebuffer BindFramebuffer(FramebufferTarget target, Framebuffer newBuffer)
        {
            GL.Viewport(newBuffer?.viewport ?? new Bounds2i(graphics.viewportOffset, graphics.viewportOffset + graphics.size));
            Graphics.ThrowErrors();

            Framebuffer output;
            if (!boundFrameBuffers.TryGetValue(target, out output))
                output = null;
            else if (output == newBuffer)
                return output;

            boundFrameBuffers[target] = newBuffer;
            GL.BindFramebuffer(target, newBuffer == null ? 0 : newBuffer.frameBufferName);
            Graphics.ThrowErrors();
            return output;
        }

        public Framebuffer GetFramebuffer(FramebufferTarget target)
        {
            Framebuffer output;
            if (boundFrameBuffers.TryGetValue(target, out output))
                return output;

            return null;
        }

        public Bounds2i FullViewPort()
            => GetFramebuffer(FramebufferTarget.Framebuffer)?.viewport
            ?? new Bounds2i(graphics.viewportOffset, graphics.viewportOffset + graphics.size);

        public void SetRenderState<ChangeLogType, StateType>(StateType value)
            where ChangeLogType : struct, RenderStateChange<StateType>
        {
            ChangeLogType newState = default(ChangeLogType);
            newState.currentValue = value;
            newState.Query();
            newState.SetState();
            if (currentScopes.Count > 0)
                currentScopes.Peek().changeLog.Add(newState);
        }

        public void SetEnable(EnableCap cap, bool enabled)
            => SetRenderState<Enable, SetEnable>(new SetEnable(cap, enabled));

        public void SetStencilFunc(StencilFunction func, int @ref, int mask)
            => SetRenderState<StencilFunc, SetStencilFunc>(new SetStencilFunc(func, @ref, mask));

        public void SetStencilOp(GlStencilOp stencilFail, GlStencilOp stencilPassDepthFail, GlStencilOp stencilPassDepthPass)
            => SetRenderState<StencilOp, SetStencilOps>(new SetStencilOps(stencilFail, stencilPassDepthFail, stencilPassDepthPass));
    }
}
