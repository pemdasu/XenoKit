using Microsoft.Xna.Framework;
using System.Collections.Generic;
using XenoKit.Engine.Vertex;

namespace XenoKit.Engine.Collision
{
    public readonly struct TriangleBvhNode : IBvhNode
    {
        public readonly Vector3 v0;
        public readonly Vector3 v1;
        public readonly Vector3 v2;

        private readonly BoundingBox aabb;
        private readonly Vector3 center;

        public TriangleBvhNode(VertexPositionNormalTextureBlend v0, VertexPositionNormalTextureBlend v1, VertexPositionNormalTextureBlend v2)
        {
            this.v0 = v0.Position;
            this.v1 = v1.Position;
            this.v2 = v2.Position;

            Vector3 min = new Vector3(float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity);
            min = Vector3.Min(min, v0.Position);
            min = Vector3.Min(min, v1.Position);
            min = Vector3.Min(min, v2.Position);
            max = Vector3.Max(max, v0.Position);
            max = Vector3.Max(max, v1.Position);
            max = Vector3.Max(max, v2.Position);

            aabb = new BoundingBox(min, max);
            center = (min + max) * 0.5f;
        }

        public BoundingBox GetAABB() => aabb;

        public Vector3 GetAABBCenter() => center;
    
        public static TriangleBvhNode[] GetNodes(IList<VertexPositionNormalTextureBlend> vertices, int[] indices)
        {
            TriangleBvhNode[] nodes = new TriangleBvhNode[indices.Length / 3];

            int idx = 0;
            for (int i = 0; i < indices.Length; i += 3)
            {
                nodes[idx] = new TriangleBvhNode(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]);
                idx++;
            }

            return nodes;
        }
    }

}
