using Microsoft.Xna.Framework;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector4 = System.Numerics.Vector4;

namespace XenoKit.Engine.Lighting
{
    public class DirLight : EngineObject
    {
        public SimdVector4 Position { get; protected set; } = new SimdVector4(-1f, 0, -1f, 0f);

        public override void Update()
        {
            Position = new SimdVector4(ViewportInstance.Camera.CameraState.Position, 1f);
        }

        public Vector4 GetLightDirection(Matrix4x4 WVP)
        {
            //Calculate light vector per actor

            //LightDir from the SPM is used by the game, but it looks... wrong here? Inverting the Z axis gets an okay result
            SimdVector4 baseDir = new SimdVector4(-0.4f, 0.0f, -0.55f, 0);
            SimdVector4 direction = SimdVector4.Transform(baseDir, WVP);
            direction = new SimdVector4(direction.X, 0, MathHelper.Clamp(direction.Z, -1f, 1f), 0f);

            return direction;
        }
    }
}
