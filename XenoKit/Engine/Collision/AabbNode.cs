using Microsoft.Xna.Framework;

namespace XenoKit.Engine.Collision
{
    public struct AabbNode : IBvhNode
    {
        public BoundingBox LeftAABB;
        public BoundingBox RightAABB;
        public int LeftIndex;
        public int RightIndex;

        public BoundingBox GetAABB()
        {
            Vector3 min = new Vector3(float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity);

            if (LeftIndex != 0)
            {
                Vector3.Min(ref min, ref LeftAABB.Min, out min);
                Vector3.Max(ref max, ref LeftAABB.Max, out max);
            }

            if (RightIndex != 0)
            {
                Vector3.Min(ref min, ref RightAABB.Min, out min);
                Vector3.Max(ref max, ref RightAABB.Max, out max);
            }

            return new BoundingBox(min, max);
        }

        public Vector3 GetAABBCenter()
        {
            var aabb = GetAABB();
            return (aabb.Min + aabb.Max) * 0.5f;
        }

        public override string ToString()
        {
            bool isLeftLeaf = LeftIndex < 0;
            bool isRightLeaf = RightIndex < 0;
            int leftIdx = isLeftLeaf ? -(LeftIndex + 1) : LeftIndex - 1;
            int rightIdx = isRightLeaf ? -(RightIndex + 1) : RightIndex - 1;
            return $"LEFT: {leftIdx} (isLeaf: {isLeftLeaf}), RIGHT: {rightIdx} (isLeaf: {isRightLeaf})";
        }
    }
}