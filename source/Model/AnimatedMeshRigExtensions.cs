using ChaosFramework.Math;
using ChaosFramework.Shapes.Rigging;

namespace ChaosFramework.Graphics.OpenGl.Model
{
    public static class AnimatedMeshRigExtensions
    {
        const string BONE_TRANSFORMS_HANDLE = "boneTransforms";

        public static void SetData(this Rig @this, ChaosShader.Shader effect, AnimatedMeshData mesh)
            => effect.SetValue(BONE_TRANSFORMS_HANDLE, @this.GetBoneTransforms(mesh, mesh.groupNames.length));

        public static Matrix[] GetBoneTransforms(this Rig @this, AnimatedMeshData mesh)
        {
            Matrix[] boneTransforms = new Matrix[mesh.groupNames.length];
            @this.GetBoneTransforms(boneTransforms, mesh);
            return boneTransforms;
        }

        public static Matrix[] GetBoneTransforms(this Rig @this, AnimatedMeshData mesh, int maxLength)
        {
            Matrix[] boneTransforms = new Matrix[maxLength];
            @this.GetBoneTransforms(boneTransforms, mesh);
            for (int i = mesh.groupNames.length; i < maxLength; i++)
                boneTransforms[i] = Matrix.IDENTITY;

            return boneTransforms;
        }

        static void GetBoneTransforms(this Rig @this, Matrix[] boneTransforms, AnimatedMeshData mesh)
        {
            @this.ComputeTransforms();
            int i = 0;
            foreach (string bone in mesh.groupNames)
            {
                Rig.Bone targetBone = @this.root.GetBoneByName(bone);
                if (targetBone != null)
                    boneTransforms[i] = Matrix.Transpose(targetBone.transform);
                i++;
            }
        }
    }
}
