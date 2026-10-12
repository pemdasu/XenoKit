using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xv2CoreLib.EMP_NEW;
using Xv2CoreLib.EEPK;
using XenoKit.Editor;
using XenoKit.Engine.Vfx.Asset;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleSystem : VfxAsset
    {
        private static readonly ConditionalWeakTable<EMP_File, object> CheckedEmpFiles = new ConditionalWeakTable<EMP_File, object>();
        private bool IsParticleSystemDestroyed = false;
        public WeakReference Effect;
        public EMP_File EmpFile { get; set; }

        private ParticleRootNode RootNode;

        private float PreviousFrame = 0f;
        public float CurrentFrameDelta { get; private set; }
        public bool IsSimulating { get; private set; }
        public Matrix4x4 AttachmentBone { get; private set; } = Matrix4x4.Identity;

        protected override bool FinishAnimationBeforeTerminating => true;

        private bool IsDirty { get; set; }

        public ParticleSystem(Matrix4x4 startWorld, Actor actor, EffectPart effectPart, EMP_File empFile, VfxEffect effect, bool spawnedByProjectile = false) : base(startWorld, effectPart, actor, spawnedByProjectile)
        {
            Effect = new WeakReference(effect);
            EmpFile = empFile;
            EmpFile.PropertyChanged += EmpFile_PropertyChanged;
            InitializeParticleSystem();
        }

        private void EmpFile_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            lock (CheckedEmpFiles)
                CheckedEmpFiles.Remove(EmpFile);
            IsDirty = true;
        }

        private void InitializeParticleSystem()
        {
            AttachmentBone = GetAdjustedTransform();
            WarnAboutRenderGroupCollisions();
            RootNode = new ParticleRootNode(EmpFile, this, EffectPart);
            RootNode.Play();
        }

        private void WarnAboutRenderGroupCollisions()
        {
            lock (CheckedEmpFiles)
            {
                if (CheckedEmpFiles.TryGetValue(EmpFile, out _))
                    return;

                var groups = new Dictionary<uint, ParticleNode>();
                foreach (ParticleNode node in EmpFile.GetAllParticleNodes())
                {
                    if (!node.IsEmission || node.EmissionNode?.Texture == null)
                        continue;

                    ParticleTexture texture = node.EmissionNode.Texture;
                    uint groupKey = texture.RenderGroupKey;

                    if (!groups.TryGetValue(groupKey, out ParticleNode firstNode))
                    {
                        groups.Add(groupKey, node);
                        continue;
                    }

                    ParticleTexture firstTexture = firstNode.EmissionNode.Texture;
                    bool differentResources = texture.MaterialID != firstTexture.MaterialID ||
                        !ReferenceEquals(texture.MaterialRef, firstTexture.MaterialRef) ||
                        texture.TextureEntryRef.Count != firstTexture.TextureEntryRef.Count;

                    for (int index = 0; !differentResources && index < texture.TextureEntryRef.Count; index++)
                    {
                        EMP_TextureSamplerDef sampler = texture.TextureEntryRef[index]?.TextureRef;
                        EMP_TextureSamplerDef firstSampler = firstTexture.TextureEntryRef[index]?.TextureRef;
                        differentResources = sampler?.EmbIndex != firstSampler?.EmbIndex ||
                            !ReferenceEquals(sampler?.TextureRef, firstSampler?.TextureRef);
                    }

                    if (differentResources)
                        Log.Add($"EMP nodes '{firstNode.Name}' and '{node.Name}' share render group 0x{groupKey:X8} but use different materials or textures. The game can draw one with the wrong texture.", LogType.Warning);
                }

                CheckedEmpFiles.Add(EmpFile, new object());
            }
        }

        /// <summary>
        /// Release all the Particle Systems objects back into the pool.
        /// </summary>
        public void DestroyParticleSystem()
        {
            EmpFile.PropertyChanged -= EmpFile_PropertyChanged;

            if (!IsParticleSystemDestroyed)
            {
                IsParticleSystemDestroyed = true;
                RootNode.ReleaseAll();
            }
        }

        public override void Terminate()
        {
            base.Terminate();

            if (IsFinished)
            {
                DestroyParticleSystem();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            DestroyParticleSystem();
        }

        public override void Update()
        {
            UpdateParticleSystem(false);
        }

        public override void Simulate()
        {
            UpdateParticleSystem(true);
        }

        private void UpdateParticleSystem(bool simulate)
        {
            if (IsDirty)
                RestartParticleSystem();

            base.Update();

            if (!HasStarted)
                return;

            CurrentFrameDelta = simulate ? 1f : CurrentFrame - PreviousFrame;
            AttachmentBone = GetAdjustedTransform();

            IsSimulating = simulate;
            try
            {
                RootNode.Update();
            }
            finally
            {
                IsSimulating = false;
            }

            if (RootNode.State == NodeState.Expired)
            {
                IsFinished = true;
                DestroyParticleSystem();
                return;
            }

            PreviousFrame = CurrentFrame;

            if (simulate)
                CurrentFrame++;
            else if (ViewportInstance.IsPlaying)
                CurrentFrame += FrameStep;
        }

        public void RestartParticleSystem()
        {
            IsDirty = false;
            RootNode.ReleaseAll();
            PreviousFrame = 0f;
            CurrentFrameDelta = 0f;
            Initialize();
            InitializeParticleSystem();
        }

        public override void SeekNextFrame()
        {
            base.SeekNextFrame();
            Simulate();
        }

        public override void SeekPrevFrame()
        {
            //Cant really seek backwards in a particle system.
        }

    }
}
