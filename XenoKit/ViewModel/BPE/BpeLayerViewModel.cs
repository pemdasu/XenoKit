using System;
using System.Collections.Generic;
using Xv2CoreLib.BPE;

namespace XenoKit.ViewModel.BPE
{
    /// <summary>
    /// The layer that holds the selected keyframe. A layer applies from its start frame to its end frame. An end frame before the start frame means the layer lasts until the entry ends.
    /// </summary>
    public sealed class BpeLayerViewModel : BpeViewModelBase
    {
        private readonly BPE_SubEntry layer;

        public ushort Start
        {
            get => layer.I_04;
            set => Set(layer, nameof(BPE_SubEntry.I_04), value, "Layer Start");
        }

        public ushort End
        {
            get => layer.I_06;
            set => Set(layer, nameof(BPE_SubEntry.I_06), value, "Layer End");
        }

        public IReadOnlyList<BpeRawField> Unknown { get; }

        public BpeLayerViewModel(BPE_SubEntry layer, BPE_File file, Action changed) : base(file, changed)
        {
            this.layer = layer;
            Unknown = Raw(layer, nameof(BPE_SubEntry.I_08), nameof(BPE_SubEntry.I_10), nameof(BPE_SubEntry.F_12), nameof(BPE_SubEntry.F_16),
                nameof(BPE_SubEntry.F_20), nameof(BPE_SubEntry.F_24), nameof(BPE_SubEntry.F_28), nameof(BPE_SubEntry.F_32),
                nameof(BPE_SubEntry.I_36), nameof(BPE_SubEntry.I_40));
        }
    }
}
