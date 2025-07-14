using Xv2CoreLib.EMD;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight;
using static Xv2CoreLib.EMD.EMD_TextureSamplerDef;
using Xv2CoreLib.EMB_CLASS;
using System.Windows;

namespace XenoKit.ViewModel.EMD
{
    public class EmdTextureViewModel : ObservableObject
    {
        private EMB_File embFile;
        private EMD_File emdFile;
        private EMD_TextureSamplerDef texture;
        private object submeshContext; //EMD_Submesh or EMG_Submesh

        public byte I_00
        {
            get
            {
                return texture.I_00;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.I_00), texture, texture.I_00, value, "TextureSampler I_00"), UndoGroup.EMD);
                texture.I_00 = value;
                RaisePropertyChanged(() => I_00);
            }
        }
        public byte EmbIndex
        {
            get
            {
                return texture.EmbIndex;
            }
            set
            {
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.EmbIndex), texture, texture.EmbIndex, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler EmbIndex", UndoGroup.EMD);

                texture.EmbIndex = value;

                RaisePropertyChanged(() => EmbIndex);
                emdFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
            }
        }
        public AddressMode AddressModeU
        {
            get
            {
                return texture.AddressModeU;
            }
            set
            {
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.AddressModeU), texture, texture.AddressModeU, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler AddressModeU", UndoGroup.EMD);
                texture.AddressModeU = value;

                RaisePropertyChanged(() => AddressModeU);
                emdFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
            }
        }
        public AddressMode AddressModeV
        {
            get
            {
                return texture.AddressModeV;
            }
            set
            {
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.AddressModeV), texture, texture.AddressModeV, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler AddressModeV", UndoGroup.EMD);
                texture.AddressModeV = value;

                RaisePropertyChanged(() => AddressModeV);
                emdFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
            }
        }
        public Filtering FilteringMin
        {
            get
            {
                return texture.FilteringMin;
            }
            set
            {
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.FilteringMin), texture, texture.FilteringMin, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler FilteringMin", UndoGroup.EMD);
                texture.FilteringMin = value;

                RaisePropertyChanged(() => FilteringMin);
                emdFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
            }
        }
        public Filtering FilteringMag
        {
            get
            {
                return texture.FilteringMag;
            }
            set
            {
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.FilteringMag), texture, texture.FilteringMag, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler FilteringMag", UndoGroup.EMD);
                texture.FilteringMag = value;

                RaisePropertyChanged(() => FilteringMag);
                emdFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
            }
        }
        public float ScaleU
        {
            get
            {
                return texture.ScaleU;
            }
            set
            {
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.ScaleU), texture, texture.ScaleU, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler ScaleU", UndoGroup.EMD);

                texture.ScaleU = value;

                RaisePropertyChanged(() => ScaleU);
                emdFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
            }
        }
        public float ScaleV
        {
            get
            {
                return texture.ScaleV;
            }
            set
            {
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.ScaleV), texture, texture.ScaleV, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler ScaleV", UndoGroup.EMD);
                texture.ScaleV = value;

                RaisePropertyChanged(() => ScaleV);
                emdFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
            }
        }

        public EmbEntry SelectedEmbEntry
        {
            get => embFile != null ? embFile.GetEntry(texture.EmbIndex) : null;
            set
            {
                if(embFile != null)
                {
                    EmbIndex = (byte)embFile.Entry.IndexOf(value);
                }
            }
        }
        public Visibility TextureSelectorVisibility => embFile != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TextureIndexVisibility => embFile == null ? Visibility.Visible : Visibility.Collapsed;

        public EmdTextureViewModel(EMD_TextureSamplerDef texture, object submeshContext, EMD_File emdFile, EMB_File embFile)
        {
            this.texture = texture;
            this.emdFile = emdFile;
            this.submeshContext = submeshContext;
            this.embFile = embFile;
        }

        public void UpdateProperties()
        {
            RaisePropertyChanged(() => I_00);
            RaisePropertyChanged(() => EmbIndex);
            RaisePropertyChanged(() => AddressModeU);
            RaisePropertyChanged(() => AddressModeV);
            RaisePropertyChanged(() => FilteringMin);
            RaisePropertyChanged(() => FilteringMag);
            RaisePropertyChanged(() => ScaleU);
            RaisePropertyChanged(() => ScaleV);
        }

    }
}
