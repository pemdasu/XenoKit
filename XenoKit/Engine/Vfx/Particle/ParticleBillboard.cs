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
                    Matrix4x4 baseWorld = particle.Transform * attachBone;
                    Matrix4x4 billboard = CreateVelocityRotation(baseWorld, particle.Camera.ViewMatrix);

                    world = billboard *
                            Matrix4x4.CreateScale(particle.ParticleSystem.Scale);
                    world.Translation = worldTranslation.Translation;
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

        internal static Matrix4x4 CreateVelocityRotation(Matrix4x4 particleWorld, Matrix4x4 viewMatrix)
        {
            SimdVector3 cameraBack = SimdVector3.Normalize(new SimdVector3(viewMatrix.M13, viewMatrix.M23, viewMatrix.M33));
            SimdVector3 particleUp = particleWorld.GetUp();
            SimdVector3 screenUp = particleUp - SimdVector3.Dot(particleUp, cameraBack) * cameraBack;
            if (screenUp.LengthSquared() < 0.000001f)
                screenUp = new SimdVector3(viewMatrix.M12, viewMatrix.M22, viewMatrix.M32);
            screenUp = SimdVector3.Normalize(screenUp);
            SimdVector3 screenRight = SimdVector3.Normalize(SimdVector3.Cross(screenUp, cameraBack));

            return new Matrix4x4(
                screenRight.X, screenRight.Y, screenRight.Z, 0f,
                screenUp.X, screenUp.Y, screenUp.Z, 0f,
                cameraBack.X, cameraBack.Y, cameraBack.Z, 0f,
                0f, 0f, 0f, 1f);
        }

        public static Matrix4x4 ApplyRenderDepth(EngineObject owner, Matrix4x4 world, float renderDepth)
        {
            return world * Matrix4x4.CreateTranslation(owner.Camera.TransformRelativeToCamera(world.Translation, renderDepth));
        }
    }
}
