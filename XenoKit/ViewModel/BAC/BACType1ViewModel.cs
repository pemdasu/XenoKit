using GalaSoft.MvvmLight;
using System;
using XenoKit.Engine;
using XenoKit.Engine.Collision;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using BacValues = Xv2CoreLib.ValuesDictionary.BAC;
using static Xv2CoreLib.BAC.BAC_Type1;

namespace XenoKit.ViewModel.BAC
{
    public class BACType1ViewModel : ObservableObject, IDisposable
    {
        private BAC_Type1 bacType;

        public byte BdmType
        {
            get
            {
                return (byte)bacType.bdmFile;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.bdmFile), bacType, bacType.bdmFile, (BAC_Type1.BdmType)value, "Hitbox BdmType"));
                bacType.bdmFile = (BAC_Type1.BdmType)value;
                RaisePropertyChanged(() => BdmType);
                bacType.RefreshType();
            }
        }
        public ushort BdmEntryID
        {
            get
            {
                return bacType.BdmEntryID;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.BdmEntryID), bacType, bacType.BdmEntryID, value, "Hitbox BdmEntryID"));
                bacType.BdmEntryID = value;
                RaisePropertyChanged(() => BdmEntryID);
            }
        }

        //HitboxFlags (damage properties)
        public ushort ImpactType
        {
            get
            {
                return (ushort)((ushort)bacType.HitboxFlags & 0xF000);
            }
            set
            {
                ushort newFlags = (ushort)(((ushort)bacType.HitboxFlags & 0x0FFF) | value);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox ImpactType"));
                bacType.HitboxFlags = (HitboxFlagsEnum)newFlags;
                UpdateHitboxProperties();
            }
        }
        public ushort DamageType
        {
            get
            {
                return (ushort)((ushort)bacType.HitboxFlags & 0x0F00);
            }
            set
            {
                ushort newFlags = (ushort)(((ushort)bacType.HitboxFlags & 0xF0FF) | value);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox DamageType"));
                bacType.HitboxFlags = (HitboxFlagsEnum)newFlags;
                UpdateHitboxProperties();
            }
        }
        public ushort HitboxSpawnSource
        {
            get
            {
                return (ushort)((ushort)bacType.HitboxFlags & 0x00F0);
            }
            set
            {
                ushort newFlags = (ushort)(((ushort)bacType.HitboxFlags & 0xFF0F) | value);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox SpawnSource"));
                bacType.HitboxFlags = (HitboxFlagsEnum)newFlags;
                UpdateHitboxProperties();
            }
        }
        

        public string ImpactTypeLabel => BacValues.BdmImpactType.TryGetValue(ImpactType, out string name) ? name : $"Unknown ({ImpactType})";
        public string DamageTypeLabel => BacValues.BdmDamageType.TryGetValue(DamageType, out string name) ? name : $"Unknown ({DamageType})";
        public string HitboxSpawnSourceLabel => BacValues.BdmSpawnSource.TryGetValue(HitboxSpawnSource, out string name) ? name : $"Unknown ({HitboxSpawnSource})";

        //HitboxFlags (bools, first flag)
        public bool HitboxFlagA_Unk1
        {
            get
            {
                return ((ushort)bacType.HitboxFlags & 0x0001) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0001 : 0x0000);
                ushort newFlags = (ushort)(((ushort)bacType.HitboxFlags & 0xFFFE) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox FlagsUnk1"));
                bacType.HitboxFlags = (HitboxFlagsEnum)newFlags;
                UpdateHitboxProperties();
                RaisePropertyChanged(() => HitboxFlagA_Unk1);
            }
        }
        public bool HitboxFlagA_Unk2
        {
            get
            {
                return ((ushort)bacType.HitboxFlags & 0x0002) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0002 : 0x0000);
                ushort newFlags = (ushort)(((ushort)bacType.HitboxFlags & 0xFFFD) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox FlagsUnk2"));
                bacType.HitboxFlags = (HitboxFlagsEnum)newFlags;
                UpdateHitboxProperties();
                RaisePropertyChanged(() => HitboxFlagA_Unk2);
            }
        }
        public bool HitboxFlagA_Unk3
        {
            get
            {
                return ((ushort)bacType.HitboxFlags & 0x0004) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0004 : 0x0000);
                ushort newFlags = (ushort)(((ushort)bacType.HitboxFlags & 0xFFFB) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox FlagsUnk3"));
                bacType.HitboxFlags = (HitboxFlagsEnum)newFlags;
                UpdateHitboxProperties();
                RaisePropertyChanged(() => HitboxFlagA_Unk3);
            }
        }
        public bool HitboxFlagA_Unk4
        {
            get
            {
                return ((ushort)bacType.HitboxFlags & 0x0008) != 0;
            }
            set
            {
                ushort result = (ushort)((value) ? 0x0008 : 0x0000);
                ushort newFlags = (ushort)(((ushort)bacType.HitboxFlags & 0xFFF7) | result);

                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox FlagsUnk4"));
                bacType.HitboxFlags = (HitboxFlagsEnum)newFlags;
                UpdateHitboxProperties();
                RaisePropertyChanged(() => HitboxFlagA_Unk4);
            }
        }

        public byte BoundingBoxType
        {
            get => (byte)bacType.BoundingBoxType;
            set
            {
                var shape = (BoundingBoxTypeEnum)value;
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.BoundingBoxType), bacType, bacType.BoundingBoxType, shape, "Hitbox Shape"));
                bacType.BoundingBoxType = shape;
            }
        }
        public string BoundingBoxTypeLabel => BacValues.BoundingBoxType.TryGetValue(BoundingBoxType, out string name) ? name : $"Unknown ({BoundingBoxType})";
        private HitShapeType ShapeType => HitShapes.GetShapeType(BoundingBoxType);
        public bool UsesRadius => ShapeType == HitShapeType.Sphere || ShapeType == HitShapeType.Capsule;
        public bool IsCapsule => ShapeType == HitShapeType.Capsule;
        public bool IsBox => ShapeType == HitShapeType.Box;

        //Damage
        public ushort Damage
        {
            get
            {
                return bacType.Damage;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.Damage), bacType, bacType.Damage, value, "Hitbox Damage"));
                bacType.Damage = value;
                RaisePropertyChanged(() => Damage);
            }
        }
        public ushort DamageWhenBlocked
        {
            get
            {
                return bacType.DamageWhenBlocked;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.DamageWhenBlocked), bacType, bacType.DamageWhenBlocked, value, "Hitbox DamageWhenBlocked"));
                bacType.DamageWhenBlocked = value;
                RaisePropertyChanged(() => DamageWhenBlocked);
            }
        }
        public ushort StaminaBlockedConsume
        {
            get
            {
                return bacType.StaminaTakenWhenBlocked;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.StaminaTakenWhenBlocked), bacType, bacType.StaminaTakenWhenBlocked, value, "Hitbox StaminaBlocked"));
                bacType.StaminaTakenWhenBlocked = value;
                RaisePropertyChanged(() => StaminaBlockedConsume);
            }
        }

        //Unknown integer. Not a flag.
        public ushort I_20
        {
            get
            {
                return bacType.I_20;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.I_20), bacType, bacType.I_20, value, "Hitbox I_20"));
                bacType.I_20 = value;
                RaisePropertyChanged(() => I_20);
            }
        }
        public BoneLinks BoneLink
        {
            get
            {
                return bacType.BoneLink;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.BoneLink), bacType, bacType.BoneLink, value, "Hitbox BoneLink"), UndoGroup.Action, "BoneLink");
                bacType.BoneLink = value;
                RaisePropertyChanged(() => BoneLink);
                UndoManager.Instance.ForceEventCall(UndoGroup.Action, "BoneLink");
            }
        }

        public float Size
        {
            get
            {
                return bacType.Size;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.Size), bacType, bacType.Size, value, "Hitbox Size"));
                bacType.Size = value;
                RaisePropertyChanged(() => Size);
            }
        }

        //BoundingBox
        public float PositionX
        {
            get
            {
                return bacType.PositionX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.PositionX), bacType, bacType.PositionX, value, "Hitbox PositionX"));
                bacType.PositionX = value;
                RaisePropertyChanged(() => PositionX);
            }
        }
        public float PositionY
        {
            get
            {
                return bacType.PositionY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.PositionY), bacType, bacType.PositionY, value, "Hitbox PositionY"));
                bacType.PositionY = value;
                RaisePropertyChanged(() => PositionY);
            }
        }
        public float PositionZ
        {
            get
            {
                return bacType.PositionZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.PositionZ), bacType, bacType.PositionZ, value, "Hitbox PositionZ"));
                bacType.PositionZ = value;
                RaisePropertyChanged(() => PositionZ);
            }
        }
        public float MaxX
        {
            get
            {
                return bacType.MaxX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.MaxX), bacType, bacType.MaxX, value, "Hitbox MaxX"));
                bacType.MaxX = value;
                RaisePropertyChanged(() => MaxX);
            }
        }
        public float MaxY
        {
            get
            {
                return bacType.MaxY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.MaxY), bacType, bacType.MaxY, value, "Hitbox MaxY"));
                bacType.MaxY = value;
                RaisePropertyChanged(() => MaxY);
            }
        }
        public float MaxZ
        {
            get
            {
                return bacType.MaxZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.MaxZ), bacType, bacType.MaxZ, value, "Hitbox MaxZ"));
                bacType.MaxZ = value;
                RaisePropertyChanged(() => MaxZ);
            }
        }
        public float MinX
        {
            get
            {
                return bacType.MinX;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.MinX), bacType, bacType.MinX, value, "Hitbox MinX"));
                bacType.MinX = value;
                RaisePropertyChanged(() => MinX);
            }
        }
        public float MinY
        {
            get
            {
                return bacType.MinY;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.MinY), bacType, bacType.MinY, value, "Hitbox MinY"));
                bacType.MinY = value;
                RaisePropertyChanged(() => MinY);
            }
        }
        public float MinZ
        {
            get
            {
                return bacType.MinZ;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.MinZ), bacType, bacType.MinZ, value, "Hitbox MinZ"));
                bacType.MinZ = value;
                RaisePropertyChanged(() => MinZ);
            }
        }


        public BACType1ViewModel(BAC_Type1 _bacType)
        {
            bacType = _bacType;
            bacType.PropertyChanged += BacType_PropertyChanged;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
            bacType.PropertyChanged -= BacType_PropertyChanged;
        }

        private void Instance_UndoOrRedoCalled(object sender, System.EventArgs e)
        {
            UpdateProperties();
        }

        private void BacType_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(bacType.BoundingBoxType)) UpdateShapeProperties();
        }

        private void UpdateShapeProperties()
        {
            RaisePropertyChanged(() => BoundingBoxTypeLabel);
            RaisePropertyChanged(() => UsesRadius);
            RaisePropertyChanged(() => IsCapsule);
            RaisePropertyChanged(() => IsBox);
        }

        private void UpdateHitboxProperties()
        {
            RaisePropertyChanged(() => ImpactType);
            RaisePropertyChanged(() => DamageType);
            RaisePropertyChanged(() => HitboxSpawnSource);
            RaisePropertyChanged(() => ImpactTypeLabel);
            RaisePropertyChanged(() => DamageTypeLabel);
            RaisePropertyChanged(() => HitboxSpawnSourceLabel);
        }

        private void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => BdmType);
            RaisePropertyChanged(() => BdmEntryID);
            UpdateHitboxProperties();
            RaisePropertyChanged(() => HitboxFlagA_Unk1);
            RaisePropertyChanged(() => HitboxFlagA_Unk2);
            RaisePropertyChanged(() => HitboxFlagA_Unk3);
            RaisePropertyChanged(() => HitboxFlagA_Unk4);
            RaisePropertyChanged(() => BoundingBoxType);
            RaisePropertyChanged(() => Damage);
            RaisePropertyChanged(() => DamageWhenBlocked);
            RaisePropertyChanged(() => StaminaBlockedConsume);
            RaisePropertyChanged(() => I_20);
            RaisePropertyChanged(() => BoneLink);
            RaisePropertyChanged(() => Size);
            RaisePropertyChanged(() => PositionX);
            RaisePropertyChanged(() => PositionY);
            RaisePropertyChanged(() => PositionZ);
            RaisePropertyChanged(() => MaxX);
            RaisePropertyChanged(() => MaxY);
            RaisePropertyChanged(() => MaxZ);
            RaisePropertyChanged(() => MinX);
            RaisePropertyChanged(() => MinY);
            RaisePropertyChanged(() => MinZ);
            UpdateShapeProperties();
            UpdateBacPlayer();
        }

        private void UpdateBacPlayer()
        {
            SceneManager.InvokeBacDataChangedEvent();
        }

        //Flags
        private bool GetHitboxFlags(ushort mask)
        {
            return ((ushort)bacType.HitboxFlags & mask) != 0;
        }


        public void SetHitboxFlags(ushort mask, bool state)
        {
            ushort result = (ushort)((state) ? mask : 0x0000);
            ushort newFlags = (ushort)((bacType.I_18_b & (0xFFFF - mask)) | result);

            UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.HitboxFlags), bacType, bacType.HitboxFlags, newFlags, "Hitbox HitboxFlags"));
            bacType.HitboxFlags = (HitboxFlagsEnum)newFlags;
            UpdateHitboxProperties();
        }
   
        public bool GetHitboxProperties1(ushort mask)
        {
            return (bacType.I_20 & mask) != 0;
        }

        public void SetHitboxProperties1(ushort mask, bool state)
        {
            ushort result = (ushort)((state) ? mask : 0x0000);
            ushort newFlags = (ushort)((bacType.I_20 & (0xFFFF - mask)) | result);

            UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Type1>(nameof(bacType.I_20), bacType, bacType.I_20, newFlags, "Hitbox PropertyFlags 1"));
            bacType.I_20 = newFlags;
        }

    }
}
