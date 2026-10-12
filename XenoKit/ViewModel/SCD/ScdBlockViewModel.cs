using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.SCD;

namespace XenoKit.ViewModel.SCD
{
    internal sealed class ScdBlockViewModel : INotifyPropertyChanged
    {
        private readonly SCD_File file;
        private readonly Action changed;
        private readonly object config;
        private readonly SCD_Config1 config1;
        private readonly SCD_Body body;
        private readonly SCD_Constraint joint;
        private readonly SCD_Spring spring;
        //Joints and springs share the bone and pivot fields
        private readonly object pair;
        private readonly SCD_Vector3 bonePivot;
        //Body versions store some fields at different offsets, so these hold the field names for this body
        private readonly string frictionField;
        private readonly string linearDampingField;
        private readonly string angularDampingField;
        private readonly string gravityFactorField;
        private readonly string restitutionField;
        private readonly string unknownBodyField;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasConfig0 { get; }
        public bool HasConfig1 { get; }
        public bool HasBody { get; }
        public bool HasJoint { get; }
        public bool HasSpring { get; }
        public bool HasRaw { get; }
        public bool HasV2Material { get; }
        public bool HasLegacyMaterial { get; }
        public bool HasConvexRadius { get; }
        public bool HasBonePivot { get; }
        public double ConvexRadiusMinimum => HasV2Material ? -float.MaxValue : 0;
        public string RawDescription { get; }
        public string UnknownBodyLabel => unknownBodyField;
        public Dictionary<int, string> MotionChoices { get; }
        public Dictionary<int, string> ShapeChoices { get; } = new Dictionary<int, string> { { 1, "Box" }, { 2, "Sphere" }, { 3, "Convex hull" }, { 4, "Capsule" } };
        public Dictionary<int, string> TwistChoices { get; } = new Dictionary<int, string> { { 0, "Off" }, { 1, "On" } };
        public Dictionary<int, string> JointChoices { get; } = new Dictionary<int, string> { { 0, "Ball and socket" }, { 1, "Stiff spring" }, { 2, "Hinge" }, { 3, "Limited hinge" }, { 6, "Ragdoll" } };
        public string MotionLabel => HasBody ? ChoiceLabel(MotionChoices, Motion) : null;
        public string ShapeLabel => HasBody ? ChoiceLabel(ShapeChoices, Shape) : null;
        public string TwistLabel => HasBody ? ChoiceLabel(TwistChoices, TwistLimit) : null;
        public string JointLabel => HasJoint ? ChoiceLabel(JointChoices, JointType) : null;

        //Config
        public float MaximumSpeed { get => GetFloat(config, "MaxSpeed"); set => Set(config, "MaxSpeed", value); }
        public float RootFollow { get => config1?.RootFollow ?? 0f; set => Set(config1, nameof(SCD_Config1.RootFollow), value); }

        //Body
        public string Bone { get => body?.Bone; set => Set(body, nameof(SCD_Body.Bone), value); }
        public int Motion { get => body?.MotionType ?? 0; set => Set(body, nameof(SCD_Body.MotionType), value); }
        public float Mass { get => body?.Mass ?? 0f; set => Set(body, nameof(SCD_Body.Mass), value); }
        public float MovementDrag { get => body?.MovementDrag ?? 0f; set => Set(body, nameof(SCD_Body.MovementDrag), value); }
        public bool KeepAnimatedOffset { get => ((body?.Flags ?? 0u) & 4u) != 0; set => SetFlags(body, 4u, value ? 4u : 0u); }
        public int TwistLimit { get => (int)((body?.Flags ?? 0u) & 3u); set => SetFlags(body, 3u, (uint)value); }
        public float FirstTwistAngle { get => body?.LimitAngle1 ?? 0f; set => Set(body, nameof(SCD_Body.LimitAngle1), value); }
        public float SecondTwistAngle { get => body?.LimitAngle2 ?? 0f; set => Set(body, nameof(SCD_Body.LimitAngle2), value); }
        public int Shape { get => (int)(body?.ShapeType ?? 0); set => Set(body, nameof(SCD_Body.ShapeType), value); }
        public float Dimension0 { get => body?.ShapeDim0 ?? 0f; set => Set(body, nameof(SCD_Body.ShapeDim0), value); }
        public float Dimension1 { get => body?.ShapeDim1 ?? 0f; set => Set(body, nameof(SCD_Body.ShapeDim1), value); }
        public float Dimension2 { get => body?.ShapeDim2 ?? 0f; set => Set(body, nameof(SCD_Body.ShapeDim2), value); }
        public float Dimension3 { get => body?.ShapeDim3 ?? 0f; set => Set(body, nameof(SCD_Body.ShapeDim3), value); }
        public float Dimension4 { get => body?.ShapeDim4 ?? 0f; set => Set(body, nameof(SCD_Body.ShapeDim4), value); }
        public float Dimension5 { get => body?.ShapeDim5 ?? 0f; set => Set(body, nameof(SCD_Body.ShapeDim5), value); }
        public float Dimension6 { get => body?.ShapeDim6 ?? 0f; set => Set(body, nameof(SCD_Body.ShapeDim6), value); }
        public float ConvexRadius { get => HasConvexRadius ? GetFloat(body, "ConvexRadius") : 0f; set => Set(body, "ConvexRadius", value); }
        public float IdleDamping { get => HasConvexRadius ? GetFloat(body, "IdleDamping") : 0f; set => Set(body, "IdleDamping", value); }
        public float Friction { get => GetFloat(body, frictionField); set => Set(body, frictionField, value); }
        public float LinearDamping { get => GetFloat(body, linearDampingField); set => Set(body, linearDampingField, value); }
        public float AngularDamping { get => GetFloat(body, angularDampingField); set => Set(body, angularDampingField, value); }
        public float GravityFactor { get => GetFloat(body, gravityFactorField); set => Set(body, gravityFactorField, value); }
        public float Restitution { get => GetFloat(body, restitutionField); set => Set(body, restitutionField, value); }
        public float TimeFactor { get => (body as SCD_BodyV2)?.TimeFactor ?? 0f; set => Set(body, nameof(SCD_BodyV2.TimeFactor), value); }
        public float DynamicFriction { get => (body as SCD_BodyV2)?.F_124 ?? 0f; set => Set(body, nameof(SCD_BodyV2.F_124), value); }
        public float StaticFriction { get => (body as SCD_BodyV2)?.F_128 ?? 0f; set => Set(body, nameof(SCD_BodyV2.F_128), value); }
        public double UnknownBody { get => unknownBodyField != null ? Convert.ToDouble(Get(body, unknownBodyField), CultureInfo.InvariantCulture) : 0d; set => Set(body, unknownBodyField, value); }
        public float TwistAxisX { get => body?.LimitAxis.X ?? 0f; set => Set(body.LimitAxis, nameof(SCD_Vector3.X), value); }
        public float TwistAxisY { get => body?.LimitAxis.Y ?? 0f; set => Set(body.LimitAxis, nameof(SCD_Vector3.Y), value); }
        public float TwistAxisZ { get => body?.LimitAxis.Z ?? 0f; set => Set(body.LimitAxis, nameof(SCD_Vector3.Z), value); }
        public float ShapePositionX { get => body?.ShapeTranslation.X ?? 0f; set => Set(body.ShapeTranslation, nameof(SCD_Vector3.X), value); }
        public float ShapePositionY { get => body?.ShapeTranslation.Y ?? 0f; set => Set(body.ShapeTranslation, nameof(SCD_Vector3.Y), value); }
        public float ShapePositionZ { get => body?.ShapeTranslation.Z ?? 0f; set => Set(body.ShapeTranslation, nameof(SCD_Vector3.Z), value); }
        public float ShapeRotationX { get => body?.ShapeRotation.X ?? 0f; set => Set(body.ShapeRotation, nameof(SCD_Vector3.X), value); }
        public float ShapeRotationY { get => body?.ShapeRotation.Y ?? 0f; set => Set(body.ShapeRotation, nameof(SCD_Vector3.Y), value); }
        public float ShapeRotationZ { get => body?.ShapeRotation.Z ?? 0f; set => Set(body.ShapeRotation, nameof(SCD_Vector3.Z), value); }
        public float CenterOfMassX { get => body?.CenterOfMassOffset.X ?? 0f; set => Set(body.CenterOfMassOffset, nameof(SCD_Vector3.X), value); }
        public float CenterOfMassY { get => body?.CenterOfMassOffset.Y ?? 0f; set => Set(body.CenterOfMassOffset, nameof(SCD_Vector3.Y), value); }
        public float CenterOfMassZ { get => body?.CenterOfMassOffset.Z ?? 0f; set => Set(body.CenterOfMassOffset, nameof(SCD_Vector3.Z), value); }
        public float InertiaX { get => body?.InertiaMultipliers.X ?? 0f; set => Set(body.InertiaMultipliers, nameof(SCD_Vector3.X), value); }
        public float InertiaY { get => body?.InertiaMultipliers.Y ?? 0f; set => Set(body.InertiaMultipliers, nameof(SCD_Vector3.Y), value); }
        public float InertiaZ { get => body?.InertiaMultipliers.Z ?? 0f; set => Set(body.InertiaMultipliers, nameof(SCD_Vector3.Z), value); }
        public float BonePivotX { get => bonePivot?.X ?? 0f; set => Set(bonePivot, nameof(SCD_Vector3.X), value); }
        public float BonePivotY { get => bonePivot?.Y ?? 0f; set => Set(bonePivot, nameof(SCD_Vector3.Y), value); }
        public float BonePivotZ { get => bonePivot?.Z ?? 0f; set => Set(bonePivot, nameof(SCD_Vector3.Z), value); }

        //Joint and spring
        public string FirstBone { get => (string)Get(pair, "BoneA"); set => Set(pair, "BoneA", value); }
        public string SecondBone { get => (string)Get(pair, "BoneB"); set => Set(pair, "BoneB", value); }
        public float FirstPivotX { get => GetVector(pair, "PivotA")?.X ?? 0f; set => Set(GetVector(pair, "PivotA"), nameof(SCD_Vector3.X), value); }
        public float FirstPivotY { get => GetVector(pair, "PivotA")?.Y ?? 0f; set => Set(GetVector(pair, "PivotA"), nameof(SCD_Vector3.Y), value); }
        public float FirstPivotZ { get => GetVector(pair, "PivotA")?.Z ?? 0f; set => Set(GetVector(pair, "PivotA"), nameof(SCD_Vector3.Z), value); }
        public float SecondPivotX { get => GetVector(pair, "PivotB")?.X ?? 0f; set => Set(GetVector(pair, "PivotB"), nameof(SCD_Vector3.X), value); }
        public float SecondPivotY { get => GetVector(pair, "PivotB")?.Y ?? 0f; set => Set(GetVector(pair, "PivotB"), nameof(SCD_Vector3.Y), value); }
        public float SecondPivotZ { get => GetVector(pair, "PivotB")?.Z ?? 0f; set => Set(GetVector(pair, "PivotB"), nameof(SCD_Vector3.Z), value); }

        //Joint
        public int JointType { get => (int)(joint?.ConstraintType ?? 0); set => Set(joint, nameof(SCD_Constraint.ConstraintType), value); }
        public float BreakingThreshold { get => joint?.BreakingThreshold ?? 0f; set => Set(joint, nameof(SCD_Constraint.BreakingThreshold), value); }
        public float Angle1 { get => joint?.Angle1 ?? 0f; set => Set(joint, nameof(SCD_Constraint.Angle1), value); }
        public float Angle2 { get => joint?.Angle2 ?? 0f; set => Set(joint, nameof(SCD_Constraint.Angle2), value); }
        public float JointFriction { get => joint?.Friction ?? 0f; set => Set(joint, nameof(SCD_Constraint.Friction), value); }
        public float Tau { get => joint?.Tau ?? 0f; set => Set(joint, nameof(SCD_Constraint.Tau), value); }
        public float RagdollAngle1 { get => joint?.RagdollAngle1 ?? 0f; set => Set(joint, nameof(SCD_Constraint.RagdollAngle1), value); }
        public float RagdollAngle2 { get => joint?.RagdollAngle2 ?? 0f; set => Set(joint, nameof(SCD_Constraint.RagdollAngle2), value); }
        public float RagdollAngle3 { get => joint?.RagdollAngle3 ?? 0f; set => Set(joint, nameof(SCD_Constraint.RagdollAngle3), value); }
        public float UnknownJoint { get => joint?.F_44 ?? 0f; set => Set(joint, nameof(SCD_Constraint.F_44), value); }
        public float FirstAxisX { get => joint?.AxisA.X ?? 0f; set => Set(joint.AxisA, nameof(SCD_Vector3.X), value); }
        public float FirstAxisY { get => joint?.AxisA.Y ?? 0f; set => Set(joint.AxisA, nameof(SCD_Vector3.Y), value); }
        public float FirstAxisZ { get => joint?.AxisA.Z ?? 0f; set => Set(joint.AxisA, nameof(SCD_Vector3.Z), value); }
        public float SecondAxisX { get => joint?.AxisB.X ?? 0f; set => Set(joint.AxisB, nameof(SCD_Vector3.X), value); }
        public float SecondAxisY { get => joint?.AxisB.Y ?? 0f; set => Set(joint.AxisB, nameof(SCD_Vector3.Y), value); }
        public float SecondAxisZ { get => joint?.AxisB.Z ?? 0f; set => Set(joint.AxisB, nameof(SCD_Vector3.Z), value); }
        public float FirstPerpendicularX { get => joint?.PerpendicularA.X ?? 0f; set => Set(joint.PerpendicularA, nameof(SCD_Vector3.X), value); }
        public float FirstPerpendicularY { get => joint?.PerpendicularA.Y ?? 0f; set => Set(joint.PerpendicularA, nameof(SCD_Vector3.Y), value); }
        public float FirstPerpendicularZ { get => joint?.PerpendicularA.Z ?? 0f; set => Set(joint.PerpendicularA, nameof(SCD_Vector3.Z), value); }
        public float SecondPerpendicularX { get => joint?.PerpendicularB.X ?? 0f; set => Set(joint.PerpendicularB, nameof(SCD_Vector3.X), value); }
        public float SecondPerpendicularY { get => joint?.PerpendicularB.Y ?? 0f; set => Set(joint.PerpendicularB, nameof(SCD_Vector3.Y), value); }
        public float SecondPerpendicularZ { get => joint?.PerpendicularB.Z ?? 0f; set => Set(joint.PerpendicularB, nameof(SCD_Vector3.Z), value); }

        //Spring
        public float RestLength { get => spring?.RestLength ?? 0f; set => Set(spring, nameof(SCD_Spring.RestLength), value); }
        public float Strength { get => spring?.Strength ?? 0f; set => Set(spring, nameof(SCD_Spring.Strength), value); }
        public float SpringDamping { get => spring?.Damping ?? 0f; set => Set(spring, nameof(SCD_Spring.Damping), value); }
        public int UnknownSpring16 { get => spring?.I_16 ?? 0; set => Set(spring, nameof(SCD_Spring.I_16), value); }
        public int UnknownSpring60 { get => spring?.I_60 ?? 0; set => Set(spring, nameof(SCD_Spring.I_60), value); }
        public bool Compression { get => ((spring?.Flags ?? 0u) & 1u) != 0; set => SetFlags(spring, 1u, value ? 1u : 0u); }
        public bool Extension { get => ((spring?.Flags ?? 0u) & 2u) != 0; set => SetFlags(spring, 2u, value ? 2u : 0u); }

        public ScdBlockViewModel(SCD_Block block, SCD_File file, Action changed)
        {
            this.file = file;
            this.changed = changed;
            HasRaw = block.Raw != null;
            RawDescription = HasRaw ? $"{block.Tag}: {block.Raw.Length} bytes, unsupported block" : null;
            HasConfig0 = !HasRaw && block.Config0 != null;
            HasConfig1 = !HasRaw && block.Config1 != null;
            HasBody = !HasRaw && block.Body != null;
            HasJoint = !HasRaw && block.Constraint != null;
            HasSpring = !HasRaw && block.Spring != null;
            MotionChoices = file.IsVersion1
                ? new Dictionary<int, string> { { 0, "Dynamic" }, { 4, "Follow animation" }, { 5, "Fixed" } }
                : new Dictionary<int, string> { { 0, "Fixed" }, { 1, "Follow animation" }, { 2, "Dynamic" } };

            config = HasConfig1 ? block.Config1 : HasConfig0 ? (object)block.Config0 : null;
            config1 = HasConfig1 ? block.Config1 : null;
            joint = HasJoint ? block.Constraint : null;
            spring = HasSpring ? block.Spring : null;
            pair = (object)joint ?? spring;

            if (HasBody)
            {
                body = block.Body;
                HasV2Material = body is SCD_BodyV2;
                HasLegacyMaterial = !HasV2Material;
                HasConvexRadius = !(body is SCD_Body3);
                HasBonePivot = body is SCD_Body5 || HasV2Material;
                bonePivot = HasBonePivot ? GetVector(body, "Pivot") : null;

                if (HasV2Material)
                {
                    linearDampingField = nameof(SCD_BodyV2.LinearDamping);
                    angularDampingField = nameof(SCD_BodyV2.AngularDamping);
                    gravityFactorField = nameof(SCD_BodyV2.GravityFactor);
                    restitutionField = nameof(SCD_BodyV2.F_132);
                    unknownBodyField = nameof(SCD_BodyV2.I_112);
                }
                else
                {
                    frictionField = "Damping";
                    linearDampingField = "Friction";
                    angularDampingField = body is SCD_Body3 ? "F_120" : body is SCD_Body4 ? "F_124" : "F_136";
                    gravityFactorField = body is SCD_Body3 ? "F_124" : body is SCD_Body4 ? "F_128" : "F_140";
                    restitutionField = body is SCD_Body3 ? "F_108" : body is SCD_Body4 ? "F_112" : "F_124";
                    unknownBodyField = body is SCD_Body3 ? "F_112" : body is SCD_Body4 ? "F_116" : "F_128";
                }
            }
        }

        private static object Get(object target, string property)
        {
            return target == null || property == null ? null : target.GetType().GetProperty(property).GetValue(target);
        }

        private static float GetFloat(object target, string property) => Get(target, property) is float value ? value : 0f;

        private static SCD_Vector3 GetVector(object target, string property) => Get(target, property) as SCD_Vector3;

        private void SetFlags(object target, uint mask, uint bits)
        {
            uint flags = (uint)Get(target, "Flags");
            Set(target, "Flags", (flags & ~mask) | (bits & mask));
        }

        private void Set(object target, string property, object value)
        {
            if (value is float number && (float.IsNaN(number) || float.IsInfinity(number)))
                throw new ArgumentException("Enter a finite number.");

            PropertyInfo info = target.GetType().GetProperty(property);
            object newValue = info.PropertyType.IsEnum ? Enum.ToObject(info.PropertyType, value) : Convert.ChangeType(value, info.PropertyType, CultureInfo.InvariantCulture);
            object oldValue = info.GetValue(target);
            if (Equals(oldValue, newValue)) return;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(property, target, oldValue, newValue, property), UndoGroup.Default, undoContext: file);
            info.SetValue(target, newValue);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            changed();
        }

        private static string ChoiceLabel(Dictionary<int, string> choices, int value)
        {
            return choices.TryGetValue(value, out string label) ? label : $"Unknown ({value})";
        }
    }
}
