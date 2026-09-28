using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BSA;
using Xv2CoreLib.BPE;
using Xv2CoreLib.Resource.App;

namespace XenoKit.Engine.Scripting.BSA
{
    // The BSA tab uses ProjectileInstance so movement, hitboxes, and effects follow the same transform path as BAC Type9 projectiles.
    public class BsaEffectPreviewController
    {
        private ProjectileInstance projectile;
        private BSA_Entry entry;
        private BSA_File bsaFile;
        private Move move;
        private int duration;
        private bool isActive;
        private int playRequestId;
        private readonly Dictionary<ushort, BacScreenEffectInstance> activeScreenEffects = new Dictionary<ushort, BacScreenEffectInstance>();
        private readonly BacScreenEffectState screenEffectState = new BacScreenEffectState();
        private readonly Action<ushort> removeScreenEffect;
        private readonly Action clearBodyOutlineValues;
        private BacScreenEffectEvaluator screenEffectEvaluator;

        public int CurrentFrame => projectile != null ? (int)Math.Floor(projectile.CurrentFrame) : 0;
        public int Duration => duration;
        internal BacScreenEffectState ScreenEffectState => isActive ? screenEffectState : null;

        public static BsaEffectPreviewController Instance { get; } = new BsaEffectPreviewController();

        private BsaEffectPreviewController()
        {
            removeScreenEffect = RemoveScreenEffect;
            clearBodyOutlineValues = ClearBodyOutlineValues;
        }

        public async void Play(BSA_Entry bsaEntry, Move selectedMove, BSA_File selectedBsaFile)
        {
            Stop();
            int requestId = ++playRequestId;

            if (bsaEntry == null || selectedMove == null || selectedBsaFile?.BSA_Entries?.Contains(bsaEntry) != true)
                return;

            if (!SettingsManager.Instance.Settings.XenoKit_ProjectileSimulation)
                return;

            await SceneManager.AsyncEnsureActorIsSet(0);

            if (requestId != playRequestId ||
                !SceneManager.IsOnTab(EditorTabs.Projectile) ||
                SceneManager.Actors[0] == null ||
                !ReferenceEquals(Files.Instance.SelectedItem?.SelectedBsaFile?.File, selectedBsaFile) ||
                selectedBsaFile.BSA_Entries?.Contains(bsaEntry) != true)
                return;

            entry = bsaEntry;
            bsaFile = selectedBsaFile;
            move = selectedMove;
            projectile = ProjectileInstance.CreatePreview(SceneManager.Actors[0], move, entry, bsaFile, Matrix4x4.Identity);
            screenEffectEvaluator = new BacScreenEffectEvaluator(SceneManager.Actors[0]);
            SceneManager.Actors[1]?.ResetState();
            duration = projectile.EndFrame;
            isActive = true;
        }

        public void Stop()
        {
            playRequestId++;
            isActive = false;
            projectile?.Dispose();
            projectile = null;
            entry = null;
            bsaFile = null;
            move = null;
            duration = 0;
            ClearScreenEffects();
            screenEffectEvaluator = null;
            Viewport.Instance?.VfxManager?.StopEffects();
            SceneManager.Actors[1]?.ResetState();
        }

        public void Update()
        {
            if (!isActive)
                return;

            if (entry == null || bsaFile == null || move == null || !SceneManager.IsOnTab(EditorTabs.Projectile))
            {
                Stop();
                return;
            }

            bool isPlaying = Viewport.Instance.IsPlaying;
            projectile?.Update(isPlaying ? (SceneManager.Actors[0]?.ActiveTimeScale ?? 1f) : 0f);
            UpdateScreenEffects();

            if (!isPlaying)
                return;

            if (projectile == null || projectile.IsFinished)
            {
                Viewport.Instance?.VfxManager?.StopEffects();
                projectile?.Dispose();
                ClearScreenEffects();
                SceneManager.Actors[1]?.ResetState();
                projectile = ProjectileInstance.CreatePreview(SceneManager.Actors[0], move, entry, bsaFile, Matrix4x4.Identity);
            }
        }

        public void SeekNextFrame()
        {
            if (!isActive)
                return;

            if (CurrentFrame < duration)
                Seek(CurrentFrame + 1);
            else
                Seek(0);
        }

        public void SeekPrevFrame()
        {
            if (!isActive)
                return;

            if (CurrentFrame > 0)
                Seek(CurrentFrame - 1);
            else
                Seek(duration);
        }

        public void Seek(int frame)
        {
            if (!isActive || entry == null || bsaFile == null || move == null || SceneManager.Actors[0] == null)
                return;

            if (Viewport.Instance.IsPlaying)
                SceneManager.Pause();

            int targetFrame = Math.Max(0, Math.Min(frame, duration));

            if (projectile != null && targetFrame >= CurrentFrame)
            {
                while (CurrentFrame < targetFrame)
                    AdvanceOneFrame(CurrentFrame + 1 >= targetFrame);

                UpdateScreenEffects();
                return;
            }

            Xv2CoreLib.Random.ResetWithCurrentSeed();
            Viewport.Instance?.VfxManager?.StopEffects();
            ClearScreenEffects();

            if (Viewport.Instance?.VfxManager != null)
                Viewport.Instance.VfxManager.ForceEffectUpdate = false;

            projectile?.Dispose();
            SceneManager.Actors[1]?.ResetState();
            projectile = ProjectileInstance.CreatePreview(SceneManager.Actors[0], move, entry, bsaFile, Matrix4x4.Identity);

            for (int replayFrame = 0; replayFrame < targetFrame; replayFrame++)
                AdvanceOneFrame(replayFrame == targetFrame - 1);

            UpdateScreenEffects();
        }

        private void AdvanceOneFrame(bool isLastFrame)
        {
            projectile?.Update(1f, false);
            SceneManager.Actors[1]?.Simulate(true, true);

            if (Viewport.Instance?.VfxManager != null)
                Viewport.Instance.VfxManager.ForceEffectUpdate = isLastFrame;

            Viewport.Instance?.VfxManager?.Simulate();
        }

        internal void StartScreenEffect(BPE_Entry bpeEntry)
        {
            ushort bpeIndex = checked((ushort)bpeEntry.SortID);
            activeScreenEffects[bpeIndex] = new BacScreenEffectInstance(bpeEntry, 0, CurrentFrame);
        }

        private void UpdateScreenEffects()
        {
            screenEffectEvaluator?.Update(activeScreenEffects.Values, screenEffectState, CurrentFrame,
                removeScreenEffect, clearBodyOutlineValues);
        }

        private void RemoveScreenEffect(ushort bpeIndex)
        {
            activeScreenEffects.Remove(bpeIndex);
        }

        private void ClearScreenEffects()
        {
            activeScreenEffects.Clear();
            screenEffectState.Clear();
            ClearBodyOutlineValues();
        }

        private void ClearBodyOutlineValues()
        {
            Actor actor = SceneManager.Actors[0];
            if (actor == null)
                return;

            actor.ClearBodyOutlineValues();
        }

        public void Draw()
        {
            projectile?.Draw();
        }

    }
}
