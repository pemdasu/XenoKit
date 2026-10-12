using System;
using Microsoft.Xna.Framework;

namespace XenoKit.Engine.Shapes
{
    public sealed class Cylinder : GeometricPrimitive
    {
        public Cylinder(float height, float radius)
        {
            alwaysVisible = true;
            const int segments = 16;
            for (int ring = 0; ring < 2; ring++)
            {
                float positionY = (ring - 0.5f) * height;
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * MathHelper.TwoPi / segments;
                    Vector3 normal = new Vector3((float)Math.Cos(angle), 0, (float)Math.Sin(angle));
                    AddVertex(new Vector3(normal.X * radius, positionY, normal.Z * radius), normal);
                }
            }
            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                AddIndex(segment); AddIndex(next); AddIndex(segment + segments);
                AddIndex(next); AddIndex(next + segments); AddIndex(segment + segments);
                AddWireframeLine(segment, next);
                AddWireframeLine(segment + segments, next + segments);
                AddWireframeLine(segment, segment + segments);
            }
            InitializePrimitive(GraphicsDevice);
        }
    }
}
