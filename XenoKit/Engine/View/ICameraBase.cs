using Microsoft.Xna.Framework;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector2 = System.Numerics.Vector2;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.View
{
    public interface ICameraBase
    {
        Matrix4x4 ViewMatrix { get; }
        Matrix4x4 ProjectionMatrix { get; }
        Matrix4x4 ViewProjectionMatrix { get; }
        CameraState CameraState { get; }
        BoundingFrustum Frustum { get; }

        SimdVector2 ProjectToScreenPosition(SimdVector3 worldPos);

        float DistanceFromCamera(SimdVector3 worldPos);

        SimdVector3 TransformRelativeToCamera(SimdVector3 position, float distanceModifier);

        void SetReflectionView(bool reflection);

        void RecalculateMatrices();
    }
}
