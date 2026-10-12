using System;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;

namespace XenoKit.Engine.Collision
{
    public class HitboxVisual : EngineObject, IDisposable
    {
        private enum ShapeType
        {
            None,
            Sphere,
            Capsule,
            Box,
            Cylinder
        }

        private readonly Color fillColor;
        private Color wireColor;
        public Color WireColor { get => wireColor; set => wireColor = value; }
        private Shapes.Cylinder cylinder;
        private float cylinderHeight;
        private float cylinderRadius;
        private readonly Cube boxWireframe;
        private Sphere sphere;
        private Capsule capsule;
        private Vector3 boxPosition;
        private float sphereRadius;
        private Vector3 capsuleStart;
        private Vector3 capsuleEnd;
        private float capsuleRadius;
        private ShapeType shapeType;
        //The sphere, capsule or cylinder to draw, and its offset from the world that Draw gets.
        private GeometricPrimitive primitive;
        private Matrix primitiveLocal;

        public HitboxVisual(Color fillColor, Color wireColor)
        {
            this.fillColor = fillColor;
            this.wireColor = wireColor;
            boxWireframe = new Cube(Vector3.Zero, new Vector3(-1f), new Vector3(1f), 0f, wireColor, true, false, true);
        }

        public void Clear()
        {
            shapeType = ShapeType.None;
        }

        public void SetSphere(Vector3 position, float radius)
        {
            if (!IsFinite(position) || !IsFinite(radius) || radius <= 0f)
                return;

            if (sphere == null || !AreClose(sphereRadius, radius))
            {
                sphere?.Dispose();
                sphere = new Sphere(radius * 2f, true, 8);
                sphereRadius = radius;
            }

            shapeType = ShapeType.Sphere;
            primitive = sphere;
            primitiveLocal = Matrix.CreateTranslation(position);
        }

        public void SetCapsule(Vector3 start, Vector3 end, float radius)
        {
            if (!IsFinite(start) || !IsFinite(end) || !IsFinite(radius) || radius <= 0f)
                return;

            if (Vector3.DistanceSquared(start, end) <= 0.0000001f)
            {
                SetSphere(start, radius);
                return;
            }

            if (capsule == null ||
                !AreClose(capsuleStart, start) ||
                !AreClose(capsuleEnd, end) ||
                !AreClose(capsuleRadius, radius))
            {
                capsule?.Dispose();
                capsule = new Capsule(start, end, radius, true);
                capsuleStart = start;
                capsuleEnd = end;
                capsuleRadius = radius;
            }

            shapeType = ShapeType.Capsule;
            primitive = capsule;
            primitiveLocal = capsule.LocalTransform;
        }

        public void SetBox(Vector3 position, Vector3 halfExtents)
        {
            if (!IsFinite(position) || !IsFinite(halfExtents))
                return;

            halfExtents = new Vector3(
                Math.Abs(halfExtents.X),
                Math.Abs(halfExtents.Y),
                Math.Abs(halfExtents.Z));

            if (halfExtents.X <= 0f && halfExtents.Y <= 0f && halfExtents.Z <= 0f)
                return;

            boxWireframe.SetBounds(-halfExtents, halfExtents, 0f, true);
            boxPosition = position;
            shapeType = ShapeType.Box;
        }

        public void SetShape(BEPUphysics.CollisionShapes.ConvexShapes.ConvexShape shape)
        {
            switch (shape)
            {
                case BEPUphysics.CollisionShapes.ConvexShapes.BoxShape box:
                    SetBox(Vector3.Zero, new Vector3(box.HalfWidth, box.HalfHeight, box.HalfLength));
                    break;
                case BEPUphysics.CollisionShapes.ConvexShapes.SphereShape sphereShape:
                    SetSphere(Vector3.Zero, sphereShape.Radius);
                    break;
                case BEPUphysics.CollisionShapes.ConvexShapes.CapsuleShape capsuleShape:
                    SetCapsule(new Vector3(0, -capsuleShape.Length * 0.5f, 0), new Vector3(0, capsuleShape.Length * 0.5f, 0), capsuleShape.Radius);
                    break;
                case BEPUphysics.CollisionShapes.ConvexShapes.CylinderShape cylinderShape:
                    if (cylinder == null || !AreClose(cylinderHeight, cylinderShape.Height) || !AreClose(cylinderRadius, cylinderShape.Radius))
                    {
                        cylinder?.Dispose();
                        cylinder = new Shapes.Cylinder(cylinderShape.Height, cylinderShape.Radius);
                        cylinderHeight = cylinderShape.Height;
                        cylinderRadius = cylinderShape.Radius;
                    }
                    shapeType = ShapeType.Cylinder;
                    primitive = cylinder;
                    primitiveLocal = Matrix.Identity;
                    break;
                default:
                    Clear();
                    break;
            }
        }

        public void Draw(Matrix world)
        {
            if (shapeType == ShapeType.None)
                return;

            bool isBox = shapeType == ShapeType.Box;
            Matrix shapeWorld = isBox ? world : primitiveLocal * world;

            if (isBox)
                boxWireframe.SetPosition(boxPosition);
            else if (fillColor.A != 0)
                primitive.Draw(shapeWorld, Camera.ViewMatrix, Camera.ProjectionMatrix, fillColor);

            foreach (Matrix projection in RenderSystem.GetLineProjections(Camera.ProjectionMatrix))
            {
                if (isBox)
                    boxWireframe.Draw(world, projection);
                else
                    primitive.DrawWireframe(shapeWorld, Camera.ViewMatrix, projection, wireColor);
            }
        }

        public void Dispose()
        {
            sphere?.Dispose();
            sphere = null;
            capsule?.Dispose();
            capsule = null;
            boxWireframe.Dispose();
            cylinder?.Dispose();
            cylinder = null;
            shapeType = ShapeType.None;
        }

        private static bool AreClose(float first, float second)
        {
            return Math.Abs(first - second) <= 0.000001f;
        }

        private static bool AreClose(Vector3 first, Vector3 second)
        {
            return Vector3.DistanceSquared(first, second) <= 0.0000001f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
        }
    }
}
