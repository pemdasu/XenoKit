using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using Xv2CoreLib.BPE;
using Xv2CoreLib;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BPE
{
    public abstract class BpeViewModelBase : ObservableObject
    {
        protected readonly BPE_File File;
        private readonly Action changed;

        protected BpeViewModelBase(BPE_File file, Action changed)
        {
            File = file;
            this.changed = changed;
        }

        protected void Set(object target, string property, object value, string message)
        {
            Commit(message, Change(target, property, value));
        }

        /// <summary>
        /// Sets the property now and returns the undo step, or null when the value is the same.
        /// </summary>
        protected static IUndoRedo Change(object target, string property, object value)
        {
            PropertyInfo info = target.GetType().GetProperty(property);
            object oldValue = info.GetValue(target);

            if (Equals(oldValue, value))
                return null;

            info.SetValue(target, value);
            return new UndoablePropertyGeneric(property, target, oldValue, value, property);
        }

        protected void Commit(string message, params IUndoRedo[] undos)
        {
            List<IUndoRedo> changes = undos.Where(undo => undo != null).ToList();

            if (changes.Count == 0)
                return;

            UndoManager.Instance.AddCompositeUndo(changes, "BPE " + message, UndoGroup.Default, undoContext: File);
            RaisePropertyChanged(string.Empty);
            changed();
        }

        /// <summary>
        /// The 0 to 1 color fields of a keyframe as a picker color. Without an alpha field the color is opaque.
        /// </summary>
        public static Color GetColor(object target, string red, string green, string blue, string alpha = null)
        {
            byte GetChannel(string property) => Xv2ColorConverter.ConvertColor((float)target.GetType().GetProperty(property).GetValue(target));
            return Color.FromArgb(alpha != null ? GetChannel(alpha) : (byte)255, GetChannel(red), GetChannel(green), GetChannel(blue));
        }

        protected void SetColor(object target, Color value, string message, string red, string green, string blue, string alpha = null)
        {
            //A channel the picker did not change keeps its exact float, so picking a color does not round the other channels.
            Color current = GetColor(target, red, green, blue, alpha);
            IUndoRedo SetChannel(string property, byte oldValue, byte newValue) => oldValue != newValue ? Change(target, property, Xv2ColorConverter.ConvertColor(newValue)) : null;

            Commit(message,
                SetChannel(red, current.R, value.R),
                SetChannel(green, current.G, value.G),
                SetChannel(blue, current.B, value.B),
                alpha != null ? SetChannel(alpha, current.A, value.A) : null);
        }

        /// <summary>
        /// Raw fields with no known meaning, for the Unknown group.
        /// </summary>
        protected IReadOnlyList<BpeRawField> Raw(object target, params string[] properties)
        {
            return properties.Select(name =>
            {
                PropertyInfo property = target.GetType().GetProperty(name);
                return new BpeRawField(name, () => Convert.ToDouble(property.GetValue(target), CultureInfo.InvariantCulture),
                    value => Set(target, name, Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture), name),
                    property.PropertyType == typeof(float));
            }).ToList();
        }
    }

    public sealed class BpeRawField
    {
        private readonly Func<double> getValue;
        private readonly Action<double> setValue;
        public string Label { get; }
        public string Format { get; }
        public double Interval { get; }

        public double? Number
        {
            get => getValue();
            set { if (value.HasValue) setValue(value.Value); }
        }

        internal BpeRawField(string label, Func<double> getValue, Action<double> setValue, bool isFloat)
        {
            Label = label;
            this.getValue = getValue;
            this.setValue = setValue;
            Format = isFloat ? "0.######" : "0";
            Interval = isFloat ? 0.01 : 1;
        }
    }
}
