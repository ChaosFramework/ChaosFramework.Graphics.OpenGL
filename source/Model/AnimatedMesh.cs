using ChaosFramework.Shapes;
using ChaosFramework.IO;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Primitives;
using System.IO;
using ChaosFramework.Collections.Immutable;
using static System.Text.Encoding;

namespace ChaosFramework.Graphics.OpenGl.Model
{
    public class AnimatedMeshData : MeshData
    {
        public const string BONE_INDICES_SEMANTIC = "BONE_INDICES";
        public const string BONE_WEIGHTS_SEMANTIC = "BONE_WEIGHTS";

        public static AnimatedMeshData FromStream(Stream srcStream)
        {
            using (BinaryReader rd = new BinaryReader(srcStream))
            {
                MeshData data = MeshData.FromStream(rd);
                Vector4f[] boneWeights = new Vector4f[data.vertexCount];
                Vector4f[] boneIndices = new Vector4f[data.vertexCount];
                if (!data.flags.HasFlag(MeshLoadFlags.Animated))
                    return new AnimatedMeshData(data, boneIndices, boneWeights, Array<string>.empty);

                for (int i = 0; i < data.vertexCount; i++)
                {
                    boneIndices[i] = new Vector4f(rd.Read<float>(), rd.Read<float>(), rd.Read<float>(), rd.Read<float>());
                    boneWeights[i] = new Vector4f(rd.Read<float>(), rd.Read<float>(), rd.Read<float>(), rd.Read<float>());
                }

                short numNames = rd.Read<short>();
                string[] groupNames = new string[numNames];
                for (int i = 0; i < numNames; i++)
                    groupNames[i] = UTF8.GetString(rd.ReadBytes(rd.Read<short>()));

                return new AnimatedMeshData(data, boneIndices, boneWeights, groupNames);
            }
        }

        public ImmutableArray<Vector4f> boneIndices;
        public ImmutableArray<Vector4f> boneWeights;
        public ImmutableArray<string> groupNames;

        public AnimatedMeshData(
            MeshData data,
            ImmutableArray<Vector4f> boneIndices,
            ImmutableArray<Vector4f> boneWeights,
            ImmutableArray<string> groupNames
            ) : this(data.pos, data.nor, data.tan, data.tex, data.ind, boneIndices, boneWeights, groupNames)
        { }

        public AnimatedMeshData(
            ImmutableArray<Vector3f> pos,
            ImmutableArray<Vector3f> nor,
            ImmutableArray<Vector4f> tan,
            ImmutableArray<ImmutableArray<Vector2f>> tex,
            ImmutableArray<uint> inds,
            ImmutableArray<Vector4f> boneIndices,
            ImmutableArray<Vector4f> boneWeights,
            ImmutableArray<string> groupNames
            ) : base(
                  MeshLoadFlags.Animated,
                  pos,
                  nor,
                  tan,
                  tex,
                  inds,
                  new[]
                  {
                      new CustomStreamDataArray<Vector4f>(boneIndices, BONE_INDICES_SEMANTIC),
                      new CustomStreamDataArray<Vector4f>(boneWeights, BONE_WEIGHTS_SEMANTIC)
                  }
                  )
        {
            this.boneIndices = boneIndices;
            this.boneWeights = boneWeights;
            this.groupNames = groupNames;
        }
    }
}
