using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BSA
{
    public class BsaEntryViewModel : ObservableObject, IDisposable
    {
        private readonly BSA_Entry entry;

        public string UserDefinedName
        {
            get => entry.UserDefinedName;
            set => SetValue(nameof(entry.UserDefinedName), entry.UserDefinedName, value, v => entry.UserDefinedName = v, "BSA Name");
        }

        public int I_00 { get => entry.I_00; set => SetValue(nameof(entry.I_00), entry.I_00, value, v => entry.I_00 = v, "BSA I_00"); }
        public byte ImpactA
        {
            get => entry.I_16_a;
            set
            {
                SetValue(nameof(entry.I_16_a), entry.I_16_a, value, v => entry.I_16_a = v, "BSA Impact A");
                RaisePropertyChanged(() => ImpactAUnknownLabel);
            }
        }
        public byte ImpactB
        {
            get => entry.I_16_b;
            set
            {
                SetValue(nameof(entry.I_16_b), entry.I_16_b, value, v => entry.I_16_b = v, "BSA Impact B");
                RaisePropertyChanged(() => ImpactBUnknownLabel);
            }
        }
        public string ImpactAUnknownLabel => Xv2CoreLib.ValuesDictionary.BSA.ImpactA.ContainsKey(ImpactA) ? null : $"Unknown ({ImpactA})";
        public string ImpactBUnknownLabel => Xv2CoreLib.ValuesDictionary.BSA.ImpactB.ContainsKey(ImpactB) ? null : $"Unknown ({ImpactB})";
        public byte I_17 { get => entry.I_17; set => SetValue(nameof(entry.I_17), entry.I_17, value, v => entry.I_17 = v, "BSA I_17"); }
        public ushort FlagsHigh { get => entry.FlagsHigh; set => SetValue(nameof(entry.FlagsHigh), entry.FlagsHigh, value, v => entry.FlagsHigh = v, "BSA I_18"); }
        public ushort I_20 { get => entry.I_20; set => SetValue(nameof(entry.I_20), entry.I_20, value, v => entry.I_20 = v, "BSA I_20"); }
        public ushort Lifetime { get => entry.Lifetime; set => SetValue(nameof(entry.Lifetime), entry.Lifetime, value, v => entry.Lifetime = v, "BSA Lifetime"); }
        public ushort LoopStartFrame { get => entry.LoopStartFrame; set => SetValue(nameof(entry.LoopStartFrame), entry.LoopStartFrame, value, v => entry.LoopStartFrame = v, "BSA Loop Start Frame"); }
        public ushort Expires { get => entry.Expires; set => SetValue(nameof(entry.Expires), entry.Expires, value, v => entry.Expires = v, "BSA Expires"); }
        public ushort ImpactProjectile { get => entry.ImpactProjectile; set => SetValue(nameof(entry.ImpactProjectile), entry.ImpactProjectile, value, v => entry.ImpactProjectile = v, "BSA Impact Projectile"); }
        public ushort ImpactEnemy { get => entry.ImpactEnemy; set => SetValue(nameof(entry.ImpactEnemy), entry.ImpactEnemy, value, v => entry.ImpactEnemy = v, "BSA Impact Enemy"); }
        public ushort ImpactGround { get => entry.ImpactGround; set => SetValue(nameof(entry.ImpactGround), entry.ImpactGround, value, v => entry.ImpactGround = v, "BSA Impact Ground"); }

        public float SpawnOffsetX { get => GetSpawnOffset(0); set => SetSpawnOffset(0, value); }
        public float SpawnOffsetY { get => GetSpawnOffset(1); set => SetSpawnOffset(1, value); }
        public float SpawnOffsetZ { get => GetSpawnOffset(2); set => SetSpawnOffset(2, value); }

        public BsaEntryViewModel(BSA_Entry entry)
        {
            this.entry = entry;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled -= UndoManager_UndoOrRedoCalled;
        }

        private void UndoManager_UndoOrRedoCalled(object sender, EventArgs e)
        {
            UpdateProperties();
        }

        private void UpdateProperties()
        {
            RaisePropertyChanged(() => UserDefinedName);
            RaisePropertyChanged(() => I_00);
            RaisePropertyChanged(() => ImpactA);
            RaisePropertyChanged(() => ImpactB);
            RaisePropertyChanged(() => ImpactAUnknownLabel);
            RaisePropertyChanged(() => ImpactBUnknownLabel);
            RaisePropertyChanged(() => I_17);
            RaisePropertyChanged(() => FlagsHigh);
            RaisePropertyChanged(() => I_20);
            RaisePropertyChanged(() => Lifetime);
            RaisePropertyChanged(() => LoopStartFrame);
            RaisePropertyChanged(() => Expires);
            RaisePropertyChanged(() => ImpactProjectile);
            RaisePropertyChanged(() => ImpactEnemy);
            RaisePropertyChanged(() => ImpactGround);
            RaisePropertyChanged(() => SpawnOffsetX);
            RaisePropertyChanged(() => SpawnOffsetY);
            RaisePropertyChanged(() => SpawnOffsetZ);
        }

        private void SetValue<TValue>(string modelProperty, TValue oldValue, TValue newValue, Action<TValue> assign, string undoName, [CallerMemberName] string viewModelProperty = null)
        {
            if (EqualityComparer<TValue>.Default.Equals(oldValue, newValue)) return;

            UndoManager.Instance.AddUndo(new UndoableProperty<BSA_Entry>(modelProperty, entry, oldValue, newValue, undoName));
            assign(newValue);
            RaisePropertyChanged(viewModelProperty);
        }

        private float GetSpawnOffset(int index)
        {
            InitSpawnOffset();
            return BitConverter.ToSingle(BitConverter.GetBytes(entry.SpawnOffsetBits[index]), 0);
        }

        private void SetSpawnOffset(int index, float value, [CallerMemberName] string viewModelProperty = null)
        {
            InitSpawnOffset();
            int bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
            if (entry.SpawnOffsetBits[index] == bits) return;

            int[] oldValue = (int[])entry.SpawnOffsetBits.Clone();
            int[] newValue = (int[])entry.SpawnOffsetBits.Clone();
            newValue[index] = bits;

            UndoManager.Instance.AddUndo(new UndoableProperty<BSA_Entry>(nameof(entry.SpawnOffsetBits), entry, oldValue, newValue, "BSA Spawn Offset"));
            entry.SpawnOffsetBits = newValue;
            RaisePropertyChanged(viewModelProperty);
        }

        private void InitSpawnOffset()
        {
            if (entry.SpawnOffsetBits == null || entry.SpawnOffsetBits.Length != 3)
                entry.SpawnOffsetBits = new int[3];
        }
    }
}
