using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BPE;

namespace XenoKit.Engine.Scripting.BAC
{
    public sealed class BpePreviewController : ObservableObject
    {
        private BPE_Entry entry;
        private int frame;
        private BacScreenEffectEvaluator evaluator;
        private Actor actor;
        private BacScreenEffectInstance[] effects = Array.Empty<BacScreenEffectInstance>();
        private readonly Action<ushort> removeEffect = index => { };
        private readonly Action clearBodyOutline;
        public BacScreenEffectState State { get; } = new BacScreenEffectState();
        public int Duration => entry?.I_04 ?? 0;
        public BpePreviewController()
        {
            clearBodyOutline = () => actor?.ClearBodyOutlineValues();
        }
        public int Frame
        {
            get => frame;
            set
            {
                frame = Math.Max(0, Math.Min(value, Duration));
                RaisePropertyChanged(nameof(Frame));
                Evaluate();
            }
        }

        public void SetEntry(BPE_Entry value)
        {
            Clear();
            entry = value;
            effects = value == null ? Array.Empty<BacScreenEffectInstance>() : new[] { new BacScreenEffectInstance(value, 0, 0) };
            frame = 0;
            RaisePropertyChanged(nameof(Duration));
            RaisePropertyChanged(nameof(Frame));
        }

        public void Update()
        {
            if (entry == null) return;
            if (Viewport.Instance.IsPlaying)
            {
                if (frame < Duration) frame++;
                else if (SceneManager.Loop || entry.I_00 == 1) frame = 0;
                else Viewport.Instance.IsPlaying = false;
                RaisePropertyChanged(nameof(Frame));
            }
            Evaluate();
        }

        public void Refresh()
        {
            RaisePropertyChanged(nameof(Duration));
            Frame = frame;
        }

        private void Evaluate()
        {
            if (entry == null || !Xv2CoreLib.Resource.App.SettingsManager.settings.XenoKit_BpeSimulation)
            {
                Clear();
                return;
            }
            Actor currentActor = SceneManager.Actors[0];
            if (evaluator == null || actor != currentActor)
            {
                actor?.ClearBodyOutlineValues();
                actor = currentActor;
                evaluator = new BacScreenEffectEvaluator(actor);
            }
            evaluator.Update(effects, State, frame, removeEffect, clearBodyOutline);
        }

        public void Clear()
        {
            State.Clear();
            actor?.ClearBodyOutlineValues();
        }
    }
}
