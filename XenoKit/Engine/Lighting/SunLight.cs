using System;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Stage;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine.Lighting
{
    public class SunLight : EngineObject
    {
        public SimdVector3 Direction { get; private set; }

        private Matrix4x4 LightViewMatrix { get; set; }
        private Matrix4x4 LightProjectionMatrix { get; set; }
        public Matrix4x4 LightViewProjectionMatrix { get; private set; }
        public Matrix4x4 LightViewProjectionBiasMatrix { get; private set; }

        public BoundingFrustum LightFrustum { get; private set; }

        private readonly Matrix4x4 BiasMatrix = new Matrix4x4(
                                                0.5f, 0.0f, 0.0f, 0.0f,
                                                0.0f, -0.5f, 0.0f, 0.0f,
                                                0.0f, 0.0f, 1.0f, 0.0f,
                                                0.5f, 0.5f, 0.0f, 1.0f
                                              );

        public SunLight()
        {
            Xv2Stage.CurrentSpmChanged += Xv2Stage_CurrentSpmChanged;
            LightFrustum = new BoundingFrustum(Matrix.Identity);
        }

        private void Xv2Stage_CurrentSpmChanged(object sender, EventArgs e)
        {
            UpdateLight();
        }

        public override void Update()
        {
            //UpdateLight();
        }

        private void UpdateLight()
        {
            Direction = new SimdVector3(ViewportInstance.CurrentStage.CurrentSpm.ShadowDirX, ViewportInstance.CurrentStage.CurrentSpm.ShadowDirY, ViewportInstance.CurrentStage.CurrentSpm.ShadowDirZ);
            //LightViewMatrix = Matrix.CreateLookAt(position, position + direction, Vector3.Up);
            LightViewMatrix = CreateDirectionalLightView(Direction, SimdVector3.Zero, 100f);

            float width = 500;
            float height = 500;
            float nearPlane = 0.5f;
            float farPlane = 500;

            LightProjectionMatrix = Matrix4x4.CreateOrthographic(width, height, nearPlane, farPlane);

            LightViewProjectionMatrix = LightViewMatrix * LightProjectionMatrix;
            //LightViewProjectionMatrix = CreateLightViewProjectionMatrix(Direction, CameraBase.Frustum);
            //LightViewProjectionMatrix = CreateLightViewProjectionMatrix();
            LightViewProjectionBiasMatrix = LightViewProjectionMatrix * BiasMatrix;

            LightFrustum.Matrix = LightProjectionMatrix;
        }

        private Matrix CreateLightViewProjectionMatrix()
        {
            // Matrix with that will rotate in points the direction of the light
            Matrix lightRotation = Matrix.CreateLookAt(Vector3.Zero,
                                                       -Direction,
                                                       Vector3.Up);

            // Get the corners of the frustum
            Vector3[] frustumCorners = Camera.Frustum.GetCorners();

            // Transform the positions of the corners into the direction of the light
            for (int i = 0; i < frustumCorners.Length; i++)
            {
                frustumCorners[i] = Vector3.Transform(frustumCorners[i], lightRotation);
            }

            // Find the smallest box around the points
            BoundingBox lightBox = BoundingBox.CreateFromPoints(frustumCorners);

            Vector3 boxSize = lightBox.Max - lightBox.Min;
            Vector3 halfBoxSize = boxSize * 0.5f;

            // The position of the light should be in the center of the back
            // pannel of the box. 
            Vector3 lightPosition = lightBox.Min + halfBoxSize;
            lightPosition.Z = lightBox.Min.Z;

            // We need the position back in world coordinates so we transform 
            // the light position by the inverse of the lights rotation
            lightPosition = Vector3.Transform(lightPosition,
                                              Matrix.Invert(lightRotation));

            // Create the view matrix for the light
            Matrix lightView = Matrix.CreateLookAt(lightPosition,
                                                   lightPosition + Direction,
                                                   Vector3.Up);

            // Create the projection matrix for the light
            // The projection is orthographic since we are using a directional light
            Matrix lightProjection = Matrix.CreateOrthographic(boxSize.X, boxSize.Y,
                                                               -boxSize.Z, boxSize.Z);

            return lightView * lightProjection;
        }

        public static Matrix4x4 CreateDirectionalLightView(SimdVector3 lightDirection, SimdVector3 sceneCenter, float sceneRadius)
        {
            lightDirection = SimdVector3.Normalize(lightDirection);

            // Create a light position sufficently far away, in the opposite direction
            SimdVector3 lightPosition = sceneCenter - lightDirection * sceneRadius * 2f;

            // Up vector - must not be parallel to light direction to avoid artifacts
            SimdVector3 up = MathHelpers.Up;
            if (SimdVector3.Dot(up, lightDirection) > 0.99f) // If too parallel, pick another
                up = MathHelpers.Right;

            return Matrix4x4.CreateLookAt(lightPosition, sceneCenter, up);
        }

    }
}
