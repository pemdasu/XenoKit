using System;
using Microsoft.Xna.Framework;
using Xv2CoreLib.BAC;
using Xv2CoreLib.EAN;
using static Xv2CoreLib.ValuesDictionary.BAC;
using SimdVector3 = System.Numerics.Vector3;
using SimdQuaternion = System.Numerics.Quaternion;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine.View
{
    public class CameraAnimInstance
    {
        bool hasBacData;

        public readonly bool AutoTerminate;
        public readonly bool DontFitToBodySize;
        public const float BlendFrameCount = 15f;
        public readonly bool BlendsIn;
        public readonly int BlendStartFrame;
        public CameraState BlendFrom;
        private float _currentFrame = 0;
        public int StartFrame = 0;
        public int EndFrame = 0;
        public EAN_File EanFile;
        public readonly EAN_Animation Animation;
        public readonly CameraTarget cameraTarget;
        public readonly BacCameraSettings bacCameraSettings;

        public Actor Actor;

        public float CurrentFrame
        {
            get
            {
                return _currentFrame;
            }
            set
            {
                _currentFrame = value;
                SceneManager.InvokeCameraCurrentFrameChangedEvent();
            }
        }
        public int CurrentAnimDuration
        {
            get
            {
                if (Animation != null)
                    return Animation.FrameCount - 1;
                return 0;
            }
        }

        public CameraAnimInstance(EAN_File eanFile, EAN_Animation anim, BAC_Type10 bacCamEntry, bool autoTerminate, int targetCharacterIndex, Actor actor)
        {
            EanFile = eanFile;
            AutoTerminate = autoTerminate;
            DontFitToBodySize = bacCamEntry?.DontFitToBodySize ?? false;
            Animation = anim;
            StartFrame = (bacCamEntry != null) ? bacCamEntry.StartFrame : 0;
            //The game ends the camera once the row frame passes start + duration.
            EndFrame = (bacCamEntry != null) ? bacCamEntry.StartFrame + bacCamEntry.Duration : anim.FrameCount - 1;
            _currentFrame = StartFrame;
            hasBacData = bacCamEntry != null;
            Actor = actor;

            //A camera that focuses on the player cuts in. A camera that focuses on the target, or has the Move flag, blends in. The Snap flag always cuts.
            if (bacCamEntry != null)
            {
                BAC_Type10.CameraFlags2 flags = bacCamEntry.cameraFlags2;
                BlendsIn = !flags.HasFlag(BAC_Type10.CameraFlags2.Unk10) && (bacCamEntry.FocusOnTarget || flags.HasFlag(BAC_Type10.CameraFlags2.Unk11));
                BlendStartFrame = StartFrame + (flags.HasFlag(BAC_Type10.CameraFlags2.Unk14) ? 1 : 0);
            }

            if (bacCamEntry != null)
            {
                bacCameraSettings = new BacCameraSettings(bacCamEntry);
                cameraTarget = new CameraTarget(targetCharacterIndex, bacCamEntry.BoneLink);
            }
            else
            {
                bacCameraSettings = default;
                cameraTarget = new CameraTarget(targetCharacterIndex, 0);
            }

        }

        public void UpdateValues()
        {
            if (!hasBacData)
                EndFrame = Animation.FrameCount - 1;
        }
    }

    public struct CameraTarget
    {
        public readonly int CharacterIndex;
        public readonly string _bone;
        public string Bone
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_bone)) return "b_C_Base";
                return _bone;
            }
        }

        public CameraTarget(int charIndex, BoneLinks bone)
        {
            CharacterIndex = charIndex;
            if (!BoneNames.TryGetValue(bone, out _bone))
                _bone = "b_C_Base";
        }
    }

    public struct BacCameraSettings
    {
        public bool Enabled;
        public ushort GlobalDuration;
        public ushort PosXDuration;
        public ushort PosYDuration;
        public ushort PosZDuration;
        public ushort RotXDuration;
        public ushort RotYDuration;
        public ushort RotZDuration;
        public ushort DispXZDuration;
        public ushort DispZYDuration;
        public ushort FovDuration;

        public float PosX;
        public float PosY;
        public float PosZ;
        public float RotX;
        public float RotY;
        public float RotZ; //Roll
        public float FoV;
        public float DispXZ;
        public float DispZY;

        public BacCameraSettings(BAC_Type10 bacCameraEntry)
        {
            Enabled = bacCameraEntry.EnableTransformModifiers;
            PosX = bacCameraEntry.PositionX;
            PosY = bacCameraEntry.PositionY;
            PosZ = bacCameraEntry.PositionZ;
            RotX = bacCameraEntry.RotationX;
            RotY = bacCameraEntry.RotationY;
            RotZ = bacCameraEntry.RotationZ;
            DispXZ = bacCameraEntry.DisplacementXZ;
            DispZY = bacCameraEntry.DisplacementZY;
            FoV = bacCameraEntry.FieldOfView;
            GlobalDuration = bacCameraEntry.GlobalModiferDuration;
            PosXDuration = bacCameraEntry.PositionX_Duration;
            PosYDuration = bacCameraEntry.PositionY_Duration;
            PosZDuration = bacCameraEntry.PositionZ_Duration;
            RotXDuration = bacCameraEntry.RotationX_Duration;
            RotYDuration = bacCameraEntry.RotationY_Duration;
            RotZDuration = bacCameraEntry.RotationZ_Duration;
            FovDuration = bacCameraEntry.FieldOfView_Duration;
            DispXZDuration = bacCameraEntry.DisplacementXZ_Duration;
            DispZYDuration = bacCameraEntry.DisplacementZY_Duration;
        }

        /// <param name="up">The camera's up axis. Rotations and the X and Y shifts use it.</param>
        /// <param name="elapsed">Frames since the row started.</param>
        public void Apply(CameraState state, SimdVector3 up, float elapsed)
        {
            float global = GetGlobalFactor(elapsed);
            state.Roll += RotZ * GetAxisFactor(RotZDuration, elapsed) * global;
            state.FieldOfView += FoV * GetAxisFactor(FovDuration, elapsed) * global;
            ApplyTo(ref state.Position, ref state.TargetPosition, up, elapsed);
        }

        private void ApplyTo(ref SimdVector3 position, ref SimdVector3 targetPosition, SimdVector3 up, float elapsed)
        {
            SimdVector3 backward = position - targetPosition;

            if (backward.LengthSquared() <= float.Epsilon)
                return;

            backward = SimdVector3.Normalize(backward);
            SimdVector3 right = GetCameraRight(up, backward);

            //Position Z moves the eye along the view line. Then the aim turns around the eye, and then the eye orbits the new target.
            SimdVector3 modifiedPosition = position + backward * PosZ * GetAxisFactor(PosZDuration, elapsed);
            float distance = (targetPosition - modifiedPosition).Length();
            SimdVector3 modifiedTargetPosition = modifiedPosition + RotateCameraDirection(-backward, up, right, DispXZ * GetAxisFactor(DispXZDuration, elapsed), DispZY * GetAxisFactor(DispZYDuration, elapsed)) * distance;
            modifiedPosition = modifiedTargetPosition + RotateCameraDirection(backward, up, right, RotY * GetAxisFactor(RotYDuration, elapsed), RotX * GetAxisFactor(RotXDuration, elapsed)) * distance;

            //Position X and Y shift the whole shot, so they move the target as well
            SimdVector3 shift = right * PosX * GetAxisFactor(PosXDuration, elapsed) + up * PosY * GetAxisFactor(PosYDuration, elapsed);
            modifiedPosition += shift;
            modifiedTargetPosition += shift;

            float factor = GetGlobalFactor(elapsed);
            position = SimdVector3.Lerp(position, modifiedPosition, factor);
            targetPosition = SimdVector3.Lerp(targetPosition, modifiedTargetPosition, factor);
        }

        private static SimdVector3 GetCameraRight(SimdVector3 up, SimdVector3 backward)
        {
            SimdVector3 right = SimdVector3.Cross(up, backward);
            float rightLength = right.Length();

            return rightLength > float.Epsilon ? right / rightLength : right;
        }

        //Pitch turns around the original right axis first, then yaw turns around the up axis.
        private static SimdVector3 RotateCameraDirection(SimdVector3 direction, SimdVector3 up, SimdVector3 right, float yawDegrees, float pitchDegrees)
        {
            if (yawDegrees == 0f && pitchDegrees == 0f)
                return direction;

            SimdQuaternion rotation = SimdQuaternion.CreateFromAxisAngle(up, MathHelper.ToRadians(yawDegrees)) * SimdQuaternion.CreateFromAxisAngle(right, MathHelper.ToRadians(pitchDegrees));
            return SimdVector3.Normalize(SimdVector3.Transform(direction, rotation));
        }

        private float GetGlobalFactor(float elapsed)
        {
            if (GlobalDuration == 0 || elapsed > GlobalDuration) return 1f;
            return elapsed / GlobalDuration;
        }

        //Each axis waits for the global blend to finish before its own blend starts.
        private float GetAxisFactor(float duration, float elapsed)
        {
            elapsed -= GlobalDuration;

            if (duration == 0 || elapsed >= duration) return 1f;
            return Math.Max(0f, elapsed) / duration;
        }
    }

}
