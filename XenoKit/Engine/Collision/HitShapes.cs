using System;
using System.Numerics;

namespace XenoKit.Engine.Collision
{
    public enum HitShapeType
    {
        None,
        Sphere,
        Capsule,
        Box
    }

    public static class HitShapes
    {
        public static HitShapeType GetShapeType(int shapeValue)
        {
            switch (shapeValue & 0xF)
            {
                case 0:
                case 3:
                    return HitShapeType.Sphere;
                case 1:
                case 4:
                    return HitShapeType.Capsule;
                case 2:
                case 5:
                    return HitShapeType.Box;
                default:
                    return HitShapeType.None;
            }
        }

        //Shape values 3, 4 and 5 ignore the character size scale.
        public static bool UsesSizeScale(int shapeValue)
        {
            return (shapeValue & 0xF) <= 2;
        }

        public static bool SphereIntersectsSphere(Vector3 firstCenter, float firstRadius, Vector3 secondCenter, float secondRadius)
        {
            float radiusSum = firstRadius + secondRadius;
            return Vector3.DistanceSquared(firstCenter, secondCenter) <= radiusSum * radiusSum;
        }

        public static Vector3 ClosestPointOnSegment(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector3 segment = end - start;
            float lengthSquared = segment.LengthSquared();

            if (lengthSquared <= 0f)
                return start;

            float amount = Math.Max(0f, Math.Min(1f, Vector3.Dot(point - start, segment) / lengthSquared));
            return start + segment * amount;
        }

        public static bool CapsuleIntersectsSphere(Vector3 start, Vector3 end, float capsuleRadius, Vector3 sphereCenter, float sphereRadius)
        {
            return SphereIntersectsSphere(ClosestPointOnSegment(start, end, sphereCenter), capsuleRadius, sphereCenter, sphereRadius);
        }

        public static bool BoxIntersectsSphere(Matrix4x4 boxWorld, Vector3 halfExtents, Vector3 sphereCenter, float sphereRadius)
        {
            if (!Matrix4x4.Invert(boxWorld, out Matrix4x4 inverseBox))
                return false;

            Vector3 localCenter = Vector3.Transform(sphereCenter, inverseBox);
            Vector3 closest = Vector3.Clamp(localCenter, -halfExtents, halfExtents);
            closest = Vector3.Transform(closest, boxWorld);
            return Vector3.DistanceSquared(closest, sphereCenter) <= sphereRadius * sphereRadius;
        }
    }
}
