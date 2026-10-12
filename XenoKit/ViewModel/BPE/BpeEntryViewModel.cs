using System;
using System.Collections.Generic;
using Xv2CoreLib.BPE;

namespace XenoKit.ViewModel.BPE
{
    public sealed class BpeEntryViewModel : BpeViewModelBase
    {
        public static IReadOnlyDictionary<int, string> PlaybackChoices { get; } = new Dictionary<int, string> { { 0, "Once" }, { 1, "Loop" } };

        private readonly BPE_Entry entry;

        public int Id
        {
            get => int.TryParse(entry.Index, out int id) ? id : 0;
            set => Set(entry, nameof(BPE_Entry.Index), value.ToString(), "ID");
        }

        public ushort Duration
        {
            get => entry.I_04;
            set => Set(entry, nameof(BPE_Entry.I_04), value, "Duration");
        }

        public int Playback
        {
            get => entry.I_00;
            set => Set(entry, nameof(BPE_Entry.I_00), value, "Playback");
        }

        public string PlaybackText => PlaybackChoices.TryGetValue(entry.I_00, out string text) ? text : $"Unknown ({entry.I_00})";

        public IReadOnlyList<BpeRawField> Unknown { get; }

        public BpeEntryViewModel(BPE_Entry entry, BPE_File file, Action changed) : base(file, changed)
        {
            this.entry = entry;
            Unknown = Raw(entry, nameof(BPE_Entry.I_06), nameof(BPE_Entry.I_08));
        }
    }
}
