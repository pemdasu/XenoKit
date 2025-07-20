using Microsoft.Xna.Framework;
using XenoKit.Engine.Vfx.Particle;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EMP_NEW;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Pool
{
    public class ObjectPoolManager : EngineObject
    {
        public readonly PoolInstance<ParticleNodeBase> ParticleNodeBasePool;
        public readonly PoolInstance<Vfx.Particle.ParticleEmitter> ParticleEmitterPool;
        public readonly PoolInstance<ParticlePlane> ParticlePlanePool;
        public readonly PoolInstance<ParticleMesh> ParticleMeshPool;

        public ObjectPoolManager()
        {
            //Pool size for base node can be reduced when ShapeDraw, Cone Extrude and Mesh are added, as only Null will use the pool at that point
            ParticleNodeBasePool = new PoolInstance<ParticleNodeBase>(1000);
            ParticleEmitterPool = new PoolInstance<Vfx.Particle.ParticleEmitter>(500);
            ParticlePlanePool = new PoolInstance<ParticlePlane>(5000);
            ParticleMeshPool = new PoolInstance<ParticleMesh>(500);
        }

        public override void DelayedUpdate()
        {
            ParticleNodeBasePool.DelayedUpdate();
            ParticleEmitterPool.DelayedUpdate();
            ParticlePlanePool.DelayedUpdate();
        }


        #region Particle Methods

        public ParticleNodeBase GetParticleNodeBase(ref Matrix4x4 emitPoint, ref SimdVector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            ParticleNodeBase newNode = ViewportInstance.ObjectPoolManager.ParticleNodeBasePool.GetObject();
            newNode.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            newNode.Reclaim();
            return newNode;
        }

        public Vfx.Particle.ParticleEmitter GetParticleEmitter(ref Matrix4x4 emitPoint, ref SimdVector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            Vfx.Particle.ParticleEmitter newNode = ViewportInstance.ObjectPoolManager.ParticleEmitterPool.GetObject();
            newNode.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            newNode.Reclaim();
            return newNode;
        }

        public ParticlePlane GetParticlePlane(ref Matrix4x4 emitPoint, ref SimdVector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            ParticlePlane newNode = ViewportInstance.ObjectPoolManager.ParticlePlanePool.GetObject();
            newNode.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            newNode.Reclaim();
            return newNode;
        }

        public ParticleMesh GetParticleMesh(ref Matrix4x4 emitPoint, ref SimdVector3 velocity, ParticleSystem system, ParticleNode node, EffectPart effectPart, object effect)
        {
            ParticleMesh newNode = ViewportInstance.ObjectPoolManager.ParticleMeshPool.GetObject();
            newNode.Initialize(emitPoint, velocity, system, node, effectPart, effect);
            newNode.Reclaim();
            return newNode;
        }
        #endregion
    }
}