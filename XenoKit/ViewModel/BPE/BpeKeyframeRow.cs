using System;
using System.Collections;
using System.Collections.Generic;
using Xv2CoreLib.BPE;

namespace XenoKit.ViewModel.BPE
{
    /// <summary>
    /// One row of the post effect list: a keyframe of a layer. The row lasts until the next keyframe of the same layer, or until the layer ends.
    /// A layer with no keyframes gets one row with no keyframe, so that you can select and remove it.
    /// </summary>
    public sealed class BpeKeyframeRow : BpeViewModelBase
    {
        public static IReadOnlyDictionary<int, string> TypeNames { get; } = new Dictionary<int, string>
        {
            { 0, "Blur" }, { 1, "White shine" }, { 2, "Color filter" }, { 3, "Type 3" }, { 4, "Disorientation" },
            { 5, "Type 5" }, { 6, "Zoom" }, { 7, "Type 7" }, { 8, "Hue" }, { 9, "Body outline" }, { 10, "Fog" }
        };

        private const string FrameProperty = "I_00";

        public BPE_Entry Entry { get; }
        public BPE_SubEntry Layer { get; }
        /// <summary>Keyframes of the layer, or null when the layer type has no keyframe list.</summary>
        public IList Keyframes { get; }
        /// <summary>The keyframe of this row, or null for a layer with no keyframes.</summary>
        public object Keyframe { get; }

        public string Type
        {
            get
            {
                string name = TypeNames.TryGetValue((int)Layer.BpeType, out string typeName) ? typeName : $"Unknown ({(int)Layer.BpeType})";
                string summary = BpeKeyframeViewModels.GetSummary(Keyframe);
                return summary != null ? $"{name} ({summary})" : name;
            }
        }

        public int Start
        {
            get => Keyframe != null ? GetFrame(Keyframe) : Layer.I_04;
            set
            {
                if (Keyframe == null)
                {
                    Set(Layer, nameof(BPE_SubEntry.I_04), (ushort)Math.Max(0, Math.Min(ushort.MaxValue, value)), "Layer Start");
                    return;
                }

                int index = Keyframes.IndexOf(Keyframe);
                int minimum = index > 0 ? GetFrame(Keyframes[index - 1]) : 0;
                int maximum = index + 1 < Keyframes.Count ? GetFrame(Keyframes[index + 1]) : int.MaxValue;
                Set(Keyframe, FrameProperty, Math.Max(minimum, Math.Min(maximum, value)), "Keyframe Start");
            }
        }

        public int Duration
        {
            get => Math.Max(0, GetEnd() - Start);
            set
            {
                int end = Start + Math.Max(0, value);
                int index = Keyframe != null ? Keyframes.IndexOf(Keyframe) : -1;

                if (index >= 0 && index + 1 < Keyframes.Count)
                {
                    //When the end moves, the next keyframe moves too. It does not move past the keyframe after it.
                    object next = Keyframes[index + 1];
                    int maximum = index + 2 < Keyframes.Count ? GetFrame(Keyframes[index + 2]) : int.MaxValue;
                    Set(next, FrameProperty, Math.Min(maximum, end), "Keyframe Duration");
                }
                else
                {
                    Set(Layer, nameof(BPE_SubEntry.I_06), (ushort)Math.Min(ushort.MaxValue, end), "Layer End");
                }
            }
        }

        public BpeKeyframeRow(BPE_Entry entry, BPE_SubEntry layer, IList keyframes, object keyframe, BPE_File file, Action changed) : base(file, changed)
        {
            Entry = entry;
            Layer = layer;
            Keyframes = keyframes;
            Keyframe = keyframe;
        }

        private int GetEnd()
        {
            int index = Keyframe != null ? Keyframes.IndexOf(Keyframe) : -1;

            if (index >= 0 && index + 1 < Keyframes.Count)
                return GetFrame(Keyframes[index + 1]);

            return Layer.I_06 >= Layer.I_04 ? Layer.I_06 : Entry.I_04;
        }

        public static int GetFrame(object keyframe) => (int)keyframe.GetType().GetProperty(FrameProperty).GetValue(keyframe);

        public static void SetFrame(object keyframe, int frame) => keyframe.GetType().GetProperty(FrameProperty).SetValue(keyframe, frame);

        /// <summary>
        /// Keyframe list of a layer, found by its type number. Null for layer types with no keyframe list.
        /// </summary>
        public static IList GetKeyframes(BPE_SubEntry layer)
        {
            return typeof(BPE_SubEntry).GetProperty("Type" + (int)layer.BpeType)?.GetValue(layer) as IList;
        }
    }
}
