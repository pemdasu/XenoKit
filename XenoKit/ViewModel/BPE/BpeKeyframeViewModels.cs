using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;
using Xv2CoreLib.BPE;
using Xv2CoreLib.Color;

namespace XenoKit.ViewModel.BPE
{
    public static class BpeKeyframeViewModels
    {
        /// <summary>
        /// The view model for a keyframe of any layer type, or null when there is no keyframe.
        /// </summary>
        public static BpeViewModelBase Create(object keyframe, BPE_File file, Action changed)
        {
            switch (keyframe)
            {
                case BPE_Type0 blur: return new BpeBlurViewModel(blur, file, changed);
                case BPE_Type1 whiteShine: return new BpeWhiteShineViewModel(whiteShine, file, changed);
                case BPE_Type2 type2: return new BpeType2ViewModel(type2, file, changed);
                case BPE_Type6 zoom: return new BpeZoomViewModel(zoom, file, changed);
                case BPE_Type8 hue: return new BpeHueViewModel(hue, file, changed);
                case BPE_Type9 outline: return new BpeBodyOutlineViewModel(outline, file, changed);
                case BPE_Type10 fog: return new BpeFogViewModel(fog, file, changed);
                case null: return null;
                default: return new BpeUnknownKeyframeViewModel(keyframe, file, changed);
            }
        }

        /// <summary>
        /// The main value of a keyframe for the keyframe list, or null when the type has no known value.
        /// </summary>
        public static string GetSummary(object keyframe)
        {
            switch (keyframe)
            {
                case BPE_Type0 blur: return FormatNumber(blur.F_04);
                case BPE_Type1 whiteShine: return FormatNumber(whiteShine.F_08);
                case BPE_Type2 type2: return FormatColor(type2);
                case BPE_Type6 zoom: return FormatNumber(zoom.F_40);
                case BPE_Type8 hue: return $"{FormatColor(hue)}, {FormatNumber(hue.F_24)}";
                case BPE_Type9 outline: return FormatColor(outline);
                case BPE_Type10 fog: return $"{FormatNumber(fog.F_20)} to {FormatNumber(fog.F_24)}";
                default: return null;
            }
        }

        private static string FormatNumber(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string FormatColor(object keyframe)
        {
            Xv2Color color = BpeColors.GetColor(keyframe);
            return $"#{color.R_Byte:X2}{color.G_Byte:X2}{color.B_Byte:X2}";
        }
    }

    public sealed class BpeBlurViewModel : BpeViewModelBase
    {
        private readonly BPE_Type0 keyframe;
        public float Amount { get => keyframe.F_04; set => Set(keyframe, nameof(keyframe.F_04), value, "Blur Amount"); }

        public BpeBlurViewModel(BPE_Type0 keyframe, BPE_File file, Action changed) : base(file, changed)
        {
            this.keyframe = keyframe;
        }
    }

    public sealed class BpeWhiteShineViewModel : BpeViewModelBase
    {
        private readonly BPE_Type1 keyframe;
        public float Intensity { get => keyframe.F_08; set => Set(keyframe, nameof(keyframe.F_08), value, "White Shine Intensity"); }
        public IReadOnlyList<BpeRawField> Unknown { get; }

        public BpeWhiteShineViewModel(BPE_Type1 keyframe, BPE_File file, Action changed) : base(file, changed)
        {
            this.keyframe = keyframe;
            Unknown = Raw(keyframe, nameof(keyframe.F_04), nameof(keyframe.F_12));
        }
    }

    public sealed class BpeType2ViewModel : BpeViewModelBase
    {
        private readonly BPE_Type2 keyframe;
        public bool UseStageFilter { get => keyframe.I_04 != 0; set => Set(keyframe, nameof(keyframe.I_04), value ? 1 : 0, "Use Stage Filter"); }
        public float Hue { get => keyframe.F_08; set => Set(keyframe, nameof(keyframe.F_08), value, "Hue"); }
        public float Saturation { get => keyframe.F_12; set => Set(keyframe, nameof(keyframe.F_12), value, "Saturation"); }
        public float ContrastR { get => keyframe.F_16; set => Set(keyframe, nameof(keyframe.F_16), value, "Contrast Red"); }
        public float ContrastG { get => keyframe.F_20; set => Set(keyframe, nameof(keyframe.F_20), value, "Contrast Green"); }
        public float ContrastB { get => keyframe.F_24; set => Set(keyframe, nameof(keyframe.F_24), value, "Contrast Blue"); }
        public Color Color { get => GetColor(keyframe, nameof(keyframe.F_28), nameof(keyframe.F_32), nameof(keyframe.F_36)); set => SetColor(keyframe, value, "Add Color", nameof(keyframe.F_28), nameof(keyframe.F_32), nameof(keyframe.F_36)); }
        public float GainR { get => keyframe.F_40; set => Set(keyframe, nameof(keyframe.F_40), value, "Gain Red"); }
        public float GainG { get => keyframe.F_44; set => Set(keyframe, nameof(keyframe.F_44), value, "Gain Green"); }
        public float GainB { get => keyframe.F_48; set => Set(keyframe, nameof(keyframe.F_48), value, "Gain Blue"); }

        public BpeType2ViewModel(BPE_Type2 keyframe, BPE_File file, Action changed) : base(file, changed)
        {
            this.keyframe = keyframe;
        }
    }

    public sealed class BpeZoomViewModel : BpeViewModelBase
    {
        private readonly BPE_Type6 keyframe;
        public float AreaSize { get => keyframe.F_04; set => Set(keyframe, nameof(keyframe.F_04), value, "Zoom Area Size"); }
        public float ZoomLevel { get => keyframe.F_40; set => Set(keyframe, nameof(keyframe.F_40), value, "Zoom Level"); }
        public Color Color { get => GetColor(keyframe, nameof(keyframe.F_08), nameof(keyframe.F_12), nameof(keyframe.F_16), nameof(keyframe.F_20)); set => SetColor(keyframe, value, "Color", nameof(keyframe.F_08), nameof(keyframe.F_12), nameof(keyframe.F_16), nameof(keyframe.F_20)); }
        public IReadOnlyList<BpeRawField> Unknown { get; }

        public BpeZoomViewModel(BPE_Type6 keyframe, BPE_File file, Action changed) : base(file, changed)
        {
            this.keyframe = keyframe;
            Unknown = Raw(keyframe, nameof(keyframe.F_24), nameof(keyframe.F_28), nameof(keyframe.F_32), nameof(keyframe.F_36), nameof(keyframe.F_44));
        }
    }

    public sealed class BpeHueViewModel : BpeViewModelBase
    {
        private readonly BPE_Type8 keyframe;
        public Color Color { get => GetColor(keyframe, nameof(keyframe.F_12), nameof(keyframe.F_16), nameof(keyframe.F_20), nameof(keyframe.F_24)); set => SetColor(keyframe, value, "Color", nameof(keyframe.F_12), nameof(keyframe.F_16), nameof(keyframe.F_20), nameof(keyframe.F_24)); }
        public IReadOnlyList<BpeRawField> Unknown { get; }

        public BpeHueViewModel(BPE_Type8 keyframe, BPE_File file, Action changed) : base(file, changed)
        {
            this.keyframe = keyframe;
            Unknown = Raw(keyframe, nameof(keyframe.I_04), nameof(keyframe.I_08));
        }
    }

    public sealed class BpeBodyOutlineViewModel : BpeViewModelBase
    {
        private readonly BPE_Type9 keyframe;
        public float NearFade { get => keyframe.F_04; set => Set(keyframe, nameof(keyframe.F_04), value, "Near Fade Distance"); }
        public float FarFade { get => keyframe.F_08; set => Set(keyframe, nameof(keyframe.F_08), value, "Far Fade Distance"); }
        public float Transparency { get => keyframe.F_12; set => Set(keyframe, nameof(keyframe.F_12), value, "Transparency"); }
        public Color Color { get => GetColor(keyframe, nameof(keyframe.F_20), nameof(keyframe.F_24), nameof(keyframe.F_28), nameof(keyframe.F_32)); set => SetColor(keyframe, value, "Color", nameof(keyframe.F_20), nameof(keyframe.F_24), nameof(keyframe.F_28), nameof(keyframe.F_32)); }
        public IReadOnlyList<BpeRawField> Unknown { get; }

        public BpeBodyOutlineViewModel(BPE_Type9 keyframe, BPE_File file, Action changed) : base(file, changed)
        {
            this.keyframe = keyframe;
            Unknown = Raw(keyframe, nameof(keyframe.F_16));
        }
    }

    public sealed class BpeFogViewModel : BpeViewModelBase
    {
        private readonly BPE_Type10 keyframe;
        public float NearDistance { get => keyframe.F_20; set => Set(keyframe, nameof(keyframe.F_20), value, "Fog Near Distance"); }
        public float FarDistance { get => keyframe.F_24; set => Set(keyframe, nameof(keyframe.F_24), value, "Fog Far Distance"); }
        public float Curve { get => keyframe.F_32; set => Set(keyframe, nameof(keyframe.F_32), value, "Fog Curve"); }
        public Color FogColor { get => ToColor(keyframe.I_44); set => Set(keyframe, nameof(keyframe.I_44), ToArgb(value), "Fog Color"); }
        public Color AddColor { get => ToColor(keyframe.I_48); set => Set(keyframe, nameof(keyframe.I_48), ToArgb(value), "Fog Add Color"); }
        public IReadOnlyList<BpeRawField> Unknown { get; }

        public BpeFogViewModel(BPE_Type10 keyframe, BPE_File file, Action changed) : base(file, changed)
        {
            this.keyframe = keyframe;
            Unknown = Raw(keyframe, nameof(keyframe.F_04), nameof(keyframe.F_08), nameof(keyframe.F_12), nameof(keyframe.F_16), nameof(keyframe.F_28),
                nameof(keyframe.I_36), nameof(keyframe.I_40));
        }

        private static Color ToColor(uint argb) => Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

        private static uint ToArgb(Color color) => (uint)color.A << 24 | (uint)color.R << 16 | (uint)color.G << 8 | color.B;
    }

    /// <summary>
    /// Layer types 3, 4, 5 and 7, whose keyframe fields have no known meaning.
    /// </summary>
    public sealed class BpeUnknownKeyframeViewModel : BpeViewModelBase
    {
        public IReadOnlyList<BpeRawField> Unknown { get; }

        public BpeUnknownKeyframeViewModel(object keyframe, BPE_File file, Action changed) : base(file, changed)
        {
            List<string> names = new List<string>();

            foreach (var property in keyframe.GetType().GetProperties())
            {
                if (property.Name != "I_00")
                    names.Add(property.Name);
            }

            Unknown = Raw(keyframe, names.ToArray());
        }
    }
}
