using ChaosFramework.Graphics.OpenGl.AssetContainers;
using ChaosFramework.Graphics.OpenGl.Instancing;
using ChaosFramework.Math;
using ChaosFramework.Shapes.Rigging;
using static ChaosFramework.Math.Constants;

namespace ChaosFramework.Graphics.OpenGl
{
    public abstract class RigRenderer : InstancingAttribute
    {
        Graphics graphics;

        ShaderContainer.Entry shader;
        MeshContainer.Entry mesh;

        public RigRenderer() { }

        public RigRenderer(int maxInstances)
            : base(maxInstances, ChaosUtil.Primitives.Array<object>.empty)
        { }

        public override void Initialize(Graphics graphics, int maxInstances, params object[] parameters)
        {
            this.graphics = graphics;
            mesh = graphics.meshes.Load("$Bone", informer);
            shader = graphics.shaders.instancedNormalMap;
            informer = new MatrixInstancer(graphics, new string[0], maxInstances);
        }

        public void AddRig(Rig rig, Matrix baseTransform, Matrix boneTransform)
        {
            Matrix outOfBlenderSpace = Matrix.RotationX(PI_HALF);
            foreach (Rig.Bone bone in rig.root.EnumerateBones())
                informer.AddInstance(
                    outOfBlenderSpace
                    * boneTransform
                    * Matrix.Scaling(bone.length)
                    * bone.GetBoneTransform()
                    * baseTransform
                    );
        }

        public void DrawInstances(Camera view, string pass)
        {
            if (informer.numInstances == 0)
                return;

            view.SetValues(shader, Matrix.IDENTITY, Matrix.IDENTITY);
            graphics.defaultMaterial.content.SetValues(shader);
            mesh.content.DrawInstanced(shader, pass, informer);
        }
    }
}
