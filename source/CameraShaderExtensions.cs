using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.OpenGl
{
    public static class CameraShaderExtensions
    {
        class FXHandles
        {
#pragma warning disable ChaosCC0102 // Handles are named exactly as the corresponding semantics -> therefore no camel case
            public ChaosShader.Shader.SemanticHandle VIEW_POSITION = null;
            public ChaosShader.Shader.SemanticHandle VIEW_DIRECTION = null;
            public ChaosShader.Shader.SemanticHandle VIEW_PROJECTION = null;
            public ChaosShader.Shader.SemanticHandle WORLD = null;
            public ChaosShader.Shader.SemanticHandle WORLDINVTRANS = null;
            public ChaosShader.Shader.SemanticHandle SCREEN_SIZE = null;
            public ChaosShader.Shader.SemanticHandle VIEW = null;
            public ChaosShader.Shader.SemanticHandle PROJECTION = null;
#pragma warning restore ChaosCC0102
        }

        static SysCol.Dictionary<ChaosShader.Shader, FXHandles> handles = new SysCol.Dictionary<ChaosShader.Shader, FXHandles>();

        public static void SetValues(this Camera camera, ChaosShader.Shader effect, Matrix transform)
            => SetValues(camera, effect, transform, Camera.GetInvTransTransform(transform));

        public static void SetValues(
            this Camera camera,
            ChaosShader.Shader effect,
            Matrix transform,
            Matrix inv_trans_transform,
            Vector4i viewPort = default(Vector4i)
            )
        {
            Graphics.ThrowErrors();
            FXHandles fxhandles;
            if (!handles.TryGetValue(effect, out fxhandles))
            {
                RegisterEffect(effect);
                fxhandles = handles[effect];
            }
            if (viewPort.z * viewPort.w > 0)
                camera.viewPort = viewPort;

            Graphics.ThrowErrors();
            if (fxhandles.VIEW_POSITION != null)
                effect.SetValue(fxhandles.VIEW_POSITION, new Vector4f(camera.Position, 0));

            if (fxhandles.VIEW_DIRECTION != null)
                effect.SetValue(fxhandles.VIEW_DIRECTION, new Vector4f(camera.Direction, 0));

            if (fxhandles.VIEW_PROJECTION != null)
                effect.SetValue(fxhandles.VIEW_PROJECTION, camera.ViewProjection);

            if (fxhandles.WORLD != null)
                effect.SetValue(fxhandles.WORLD, transform);

            if (fxhandles.VIEW != null)
                effect.SetValue(fxhandles.VIEW, camera.View);

            if (fxhandles.PROJECTION != null)
                effect.SetValue(fxhandles.PROJECTION, camera.Projection);

            if (fxhandles.WORLDINVTRANS != null)
                effect.SetValue(fxhandles.WORLDINVTRANS, inv_trans_transform);

            if (fxhandles.SCREEN_SIZE != null)
                effect.SetValue(fxhandles.SCREEN_SIZE, (Vector4f)camera.viewPort);
            Graphics.ThrowErrors();
        }

        public static void RegisterEffect(ChaosShader.Shader effect)
        {
            if (handles.ContainsKey(effect))
                return;

            FXHandles fxHandles = new FXHandles();
            foreach (System.Reflection.FieldInfo info in typeof(FXHandles).GetFields())
                if (info.FieldType == typeof(ChaosShader.Shader.SemanticHandle))
                    info.SetValue(fxHandles, effect.GetParameterBySemantic(info.Name));

            handles.Add(effect, fxHandles);
            effect.AddOnDispose(() => handles.Remove(effect));
        }
    }
}
