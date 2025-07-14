using Microsoft.Xna.Framework;

namespace XenoKit.Engine.Collision
{
    public struct AabbNode
    {
        public BoundingBox LeftAABB;
        public BoundingBox RightAABB;
        public int LeftIndex;
        public int RightIndex;
    }
}