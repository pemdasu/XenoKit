using Microsoft.Xna.Framework;
using XenoKit.Engine;
using Xv2CoreLib;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Particle
{
    public static class ParticleBillboard
    {
        public static Matrix4x4 CreateWorld(ParticleEmissionBase particle, ParticleBillboardType billboardType, bool velocityOriented, float rotationAmount, bool randomDirection, float renderDepth)
        {
            Matrix4x4 world;

            if (billboardType == ParticleBillboardType.Camera)
            {
                Matrix4x4 attachBone = particle.GetParticleAttachmentBone();
                float angle = randomDirection ? -rotationAmount : rotationAmount;
                Matrix4x4 worldTranslation = particle.Transform * Matrix4x4.CreateScale(particle.ParticleSystem.Scale) * attachBone;

                if (velocityOriented)
                {
                    SimdVector3 velocity = SimdVector3.TransformNormal(particle.Velocity, attachBone);
                    Matrix4x4 billboard = CreateVelocityRotation(velocity, particle.Camera.ViewMatrix);

                    world = billboard *
                            Matrix4x4.CreateScale(particle.ParticleSystem.Scale);
                    world.Translation = worldTranslation.Translation;
                    if (particle is ParticlePlane plane)
                        world.Translation += billboard.GetUp() * plane.GetHeight() * particle.ParticleSystem.Scale;
                }
                else
                {
                    Matrix4x4 billboard = Matrix4x4.CreateFromAxisAngle(MathHelpers.Up, MathHelper.Pi) *
                            Matrix4x4.CreateFromAxisAngle(MathHelpers.Forward, MathHelper.ToRadians(-angle)) *
                            MathHelpers.Invert(particle.Camera.ViewMatrix);
                    billboard.Translation = SimdVector3.Zero;

                    world = billboard *
                            Matrix4x4.CreateScale(particle.ParticleSystem.Scale);
                    world.Translation = worldTranslation.Translation;
                }
            }
            else if (billboardType == ParticleBillboardType.Front)
            {
                Matrix4x4 attachBone = particle.GetParticleAttachmentBone();
                float angle = randomDirection ? -rotationAmount : rotationAmount;
                Matrix4x4 baseWorld = particle.Transform * Matrix4x4.CreateScale(particle.ParticleSystem.Scale) * attachBone;
                Matrix4x4 billboard = Matrix4x4.CreateFromAxisAngle(MathHelpers.Forward, MathHelper.ToRadians(-angle)) *
                        Matrix4x4.CreateBillboard(baseWorld.Translation, attachBone.Translation, MathHelpers.Up, SimdVector3.Zero);
                billboard.Translation = SimdVector3.Zero;

                world = billboard *
                        Matrix4x4.CreateScale(particle.ParticleSystem.Scale);
                world.Translation = baseWorld.Translation;
            }
            else
            {
                world = particle.GetParticleRotationAxisWorld(false);
            }

            return ApplyRenderDepth(particle, world, renderDepth);
        }

        internal static Matrix4x4 CreateVelocityRotation(SimdVector3 velocity, Matrix4x4 viewMatrix)
        {
            SimdVector3 cameraBack = SimdVector3.Normalize(new SimdVector3(viewMatrix.M13, viewMatrix.M23, viewMatrix.M33));
            SimdVector3 cameraFront = -cameraBack;
            SimdVector3 particleUp = velocity.LengthSquared() > 0f ? SimdVector3.Normalize(velocity) : SimdVector3.Zero;

            // The length stays along the 3D direction, so a streak that points at the camera
            // is foreshortened. Only the width axis faces the camera. When the direction is
            // parallel to the view, the width is zero and the streak is not visible.
            SimdVector3 right = SimdVector3.Cross(particleUp, cameraFront);
            SimdVector3 front = cameraFront;
            if (right.LengthSquared() < 0.000001f)
            {
                right = SimdVector3.Zero;
            }
            else
            {
                right = SimdVector3.Normalize(right);
                front = SimdVector3.Cross(right, particleUp);
            }

            return new Matrix4x4(
                right.X, right.Y, right.Z, 0f,
                particleUp.X, particleUp.Y, particleUp.Z, 0f,
                front.X, front.Y, front.Z, 0f,
                0f, 0f, 0f, 1f);
        }

        public static Matrix4x4 ApplyRenderDepth(EngineObject owner, Matrix4x4 world, float renderDepth)
        {
            return world * Matrix4x4.CreateTranslation(owner.Camera.TransformRelativeToCamera(world.Translation, renderDepth));
        }
    }
}
