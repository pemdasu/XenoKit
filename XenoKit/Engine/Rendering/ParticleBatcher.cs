using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Vfx.Particle;
using Xv2CoreLib.EMP_NEW;

namespace XenoKit.Engine.Rendering
{
    public class ParticleBatcher : EngineObject
    {
        private readonly List<ParticleBatch> _batches = new List<ParticleBatch>();

        private int _totalBatchedParticles = 0;
        public int NumBatches => _batches.Count;
        public int NumTotalBatched => _totalBatchedParticles;

        public ParticleBatcher()
        {

        }

        public void SlowUpdate()
        {
            for (int i = _batches.Count - 1; i >= 0; i--)
            {
                if (_batches[i].MaxBatchSinceLastSlowUpdate > 0)
                {
                    _batches[i].SlowUpdate();
                }
                else
                {
                    _batches[i].Destroy();
                    _batches.RemoveAt(i);
                }
            }

        }

        public override void Update()
        {
            for (int i = 0; i < _batches.Count; i++)
            {
                _batches[i].Update();
            }
        }

        public void Draw(int lowRez)
        {
            if(lowRez == 0)
                _totalBatchedParticles = 0;

            for (int i = 0; i < _batches.Count; i++)
            {
                if (_batches[i].LowRezMode == lowRez)
                {
                    _totalBatchedParticles += _batches[i].NumBatch;
                    _batches[i].Draw();
                }
            }
        }

        public ParticleBatch GetBatch(ParticleSystem particleSystem, ParticleNode particleNode, bool noGlare)
        {
            EMP_File empFile = particleSystem.EmpFile;
            uint renderGroupKey = particleNode.EmissionNode.Texture.RenderGroupKey;
            int insertIndex = _batches.Count;

            //Instances of the same EMP share one batch per key, and those batches draw in ascending key order.
            //Batches of different EMP files keep the order they were created in.
            for (int i = 0; i < _batches.Count; i++)
            {
                ParticleBatch batch = _batches[i];

                if (!ReferenceEquals(batch.EmpFile, empFile))
                    continue;

                if (batch.RenderGroupKey > renderGroupKey && insertIndex == _batches.Count)
                    insertIndex = i;

                if (batch.RenderGroupKey == renderGroupKey && batch.NoGlare == noGlare)
                {
                    //A miss means the file was edited after the batch was built.
                    if (!batch.HasMember(particleNode))
                    {
                        batch.LoadMembers();

                        if (!batch.HasMember(particleNode))
                            throw new InvalidOperationException($"ParticleBatcher: node \"{particleNode.Name}\" is not in render group {renderGroupKey} of its EMP file.");
                    }

                    return batch;
                }
            }

            ParticleBatch newBatch = new ParticleBatch(empFile, renderGroupKey, noGlare, particleNode);
            _batches.Insert(insertIndex, newBatch);
            return newBatch;
        }
    }
}
