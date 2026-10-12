using Microsoft.Xna.Framework;
using System;
using Xv2CoreLib;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.App;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Asset
{
    public abstract class VfxAsset : RenderObject, IDisposable
    {
        public float CurrentFrame { get; protected set; }
        public bool HasStarted { get; protected set; }
        public bool IsFinished { get; protected set; }
        public bool IsTerminating { get; protected set; }
        public EffectPart EffectPart { get; }

        /// <summary>
        /// Where the asset actually ends up on screen, matching the matrix used when drawing it.
        /// </summary>
        public Matrix4x4 VisualTransform => GetAdjustedTransform();

        /// <summary>
        /// The basis the Position X/Y/Z offsets are applied in. Identity when they act in world space.
        /// </summary>
        public Matrix4x4 PositionSpace { get; private set; } = Matrix4x4.Identity;

        protected readonly Actor Actor;
        protected readonly bool SpawnedByProjectile;

        protected virtual bool FinishAnimationBeforeTerminating => false;
        private int BoneIdx = -1;
        public float Scale { get; protected set; } = -1f;
        private Matrix4x4 BacSpawnSource;
        private Matrix4x4 ExternalNodeSource;
        private SimdVector3 AttachmentPosition;
        private SimdVector3 PreviousAttachmentPosition;
        private Matrix4x4 AttachmentRotation;
        private SimdVector3 AttachmentDirection;
        private Matrix4x4 CurrentRotation;
        private float rotationFactorX;
        private float rotationFactorY;
        private float rotationFactorZ;

        //Asset Type
        private AssetType AssetType;
        public bool AssetTypeChanged { get; private set; }

        public VfxAsset(Matrix4x4 startWorld, EffectPart effectPart, Actor actor, bool spawnedByProjectile = false)
        {
            EffectPart = effectPart;
            Actor = actor;
            SpawnedByProjectile = spawnedByProjectile;
            AssetType = EffectPart.AssetType;
            BacSpawnSource = startWorld;
            ExternalNodeSource = startWorld;

            Initialize();
            EffectPart.PropertyChanged += EffectPart_PropertyChanged;
        }

        private void EffectPart_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //TODO: Set up properties in EffectPart for relevant values! This does nothing right now...
            Initialize();
        }

        protected virtual void Initialize()
        {
            if (AssetType != EffectPart.AssetType)
            {
                AssetTypeChanged = true;
            }

            BoneIdx = !SpawnedByProjectile && !string.IsNullOrWhiteSpace(EffectPart.ESK) && Actor != null
                ? Actor.Skeleton.GetBoneIndex(EffectPart.ESK, true) : -1;

            //Roll where in the min/max range this instance sits, once per spawn. Keeping the factor instead of the
            //resulting angle lets the rotation follow edits to the min/max values without re-rolling and jittering.
            rotationFactorX = Xv2CoreLib.Random.Range(0f, 1f);
            rotationFactorY = Xv2CoreLib.Random.Range(0f, 1f);
            rotationFactorZ = Xv2CoreLib.Random.Range(0f, 1f);

            RefreshRotation();

            AttachmentRotation = Matrix4x4.Identity;
            AttachmentDirection = GetSpawnDirection();
            UpdateAttachment(GetAttachTransform(), true);

            Scale = Xv2CoreLib.Random.Range(EffectPart.ScaleMin, EffectPart.ScaleMax) * GetOwnerScale();

            //Reset start state
            CurrentFrame = 0f;
            HasStarted = false;
        }

        //The game applies these owner scale flags once, when the part spawns
        private float GetOwnerScale()
        {
            if (Actor == null || SpawnedByProjectile) return 1f;

            float scale = 1f;
            float bodySize = Actor.CharacterData?.BcsFile?.File?.F_48?[1] ?? 1f;

            //EMO and particle parts: I_38_b bit 0 scales by the body size, bit 1 by the largest axis of the attach bone scale
            if (EffectPart.AssetType == AssetType.EMO || EffectPart.AssetType == AssetType.PBIND)
            {
                if ((EffectPart.I_38_b & 0x1) != 0)
                    scale *= bodySize;

                if ((EffectPart.I_38_b & 0x2) != 0 && BoneIdx != -1)
                {
                    Matrix4x4.Decompose(Actor.GetAbsoluteBoneMatrix(BoneIdx), out SimdVector3 boneScale, out _, out _);
                    scale *= System.Math.Max(boneScale.X, System.Math.Max(boneScale.Y, boneScale.Z));
                }
            }

            if (EffectPart.AssetType == AssetType.PBIND && EffectPart.HasFlag2(EepkEffectPartFlags2.ScaleByBodySize))
                scale *= bodySize;

            return scale;
        }

        /// <summary>
        /// Deactivates the effect according to the Deactivation Mode.
        /// </summary>
        public virtual void Terminate()
        {
            if (EffectPart.Deactivation ==DeactivationMode.Immediate || (EffectPart.Deactivation == DeactivationMode.LoopCancel && !FinishAnimationBeforeTerminating))
            {
                IsFinished = true;
            }
            else if (EffectPart.Deactivation == DeactivationMode.LoopCancel)
            {
                IsTerminating = true;
            }
        }

        public virtual void Dispose()
        {
            EffectPart.PropertyChanged -= EffectPart_PropertyChanged;
        }

        public void SetExternalTransform(Matrix4x4 transform, Matrix4x4 nodeTransform)
        {
            BacSpawnSource = transform;
            ExternalNodeSource = nodeTransform;
            OnExternalTransformChanged();
        }

        protected Matrix4x4 GetExternalSpawnTransform()
        {
            return string.Equals(EffectPart.ESK, "TRS", StringComparison.OrdinalIgnoreCase) ||
                (SpawnedByProjectile && IsProjectileNodeName(EffectPart.ESK)) ? ExternalNodeSource : BacSpawnSource;
        }

        internal static bool IsProjectileNodeName(string name)
        {
            return string.Equals(name, "ROOT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "TRS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "NULL_0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "NULL_1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "NULL_2", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "NULL_3", StringComparison.OrdinalIgnoreCase);
        }

        protected Matrix4x4 GetExternalNodeTransform()
        {
            return ExternalNodeSource;
        }

        protected Matrix4x4 GetExternalRootTransform()
        {
            return BacSpawnSource;
        }

        protected virtual void OnExternalTransformChanged()
        {
        }

        /// <summary>
        /// Recalculates the rotation from the current EffectPart values, so edits in the editor are seen right away.
        /// </summary>
        private void RefreshRotation()
        {
            if (!EffectPart.EnableRotationValues)
            {
                CurrentRotation = Matrix4x4.Identity;
                return;
            }

            float rotX = MathHelper.ToRadians(MathHelper.Lerp(EffectPart.RotationX_Min, EffectPart.RotationX_Max, rotationFactorX));
            float rotY = MathHelper.ToRadians(MathHelper.Lerp(EffectPart.RotationY_Min, EffectPart.RotationY_Max, rotationFactorY));
            float rotZ = MathHelper.ToRadians(MathHelper.Lerp(EffectPart.RotationZ_Min, EffectPart.RotationZ_Max, rotationFactorZ));

            CurrentRotation = VfxRotation.Create(rotX, rotY, rotZ);
        }

        public override void Update()
        {
            RefreshRotation();

            if (!HasStarted)
            {
                if(CurrentFrame >= EffectPart.StartTime)
                {
                    CurrentFrame = 0f;
                    HasStarted = true;
                }
                else
                {
                    CurrentFrame += FrameStep;
                    DrawThisFrame = false;
                    return;
                }
            }

            DrawThisFrame = true;

            UpdateAttachment(EffectPart.PositionUpdate || EffectPart.RotateUpdate
                ? GetAttachTransform() : Matrix4x4.Identity, false);

            //Near and Far fade distance
            if (MathHelpers.FloatEquals(EffectPart.FarFadeDistance, 0))
            {
                DrawThisFrame = true;
            }
            else
            {
                float distanceToCamera = System.Math.Abs(Vector3.Distance(ViewportInstance.Camera.CameraState.Position, Transform.Translation));
                DrawThisFrame = distanceToCamera >= EffectPart.NearFadeDistance && distanceToCamera < EffectPart.FarFadeDistance;
            }

            if (!SettingsManager.Instance.Settings.XenoKit_VfxSimulation)
                DrawThisFrame = false;

            //Particle nodes batch while they update, so this must be set before them
            if (HideWithActor && Actor != null && !Actor.IsVisible)
                DrawThisFrame = false;
        }

        public bool HideWithActor { get; set; }

        // Effect previews can run before a character is loaded, so Actor can be null.
        protected float FrameStep => EffectPart.UseTimeScale && Actor != null ? Actor.ActiveTimeScale : 1f;

        protected bool UsesExternalSpawn()
        {
            return EffectPart.AttachementType == Attachment.External ||
                   (EffectPart.AttachementType == Attachment.Bone && string.Equals(EffectPart.ESK, "TRS", StringComparison.OrdinalIgnoreCase)) ||
                   (SpawnedByProjectile && EffectPart.AttachementType == Attachment.Bone);
        }

        private Matrix4x4 GetAttachTransform()
        {
            if (EffectPart.AttachementType == Attachment.Bone && !UsesExternalSpawn())
                return BoneIdx != -1 && Actor != null ? Actor.GetAbsoluteBoneMatrix(BoneIdx) : Matrix4x4.Identity;

            if (EffectPart.AttachementType == Attachment.Camera)
            {
                Matrix4x4 camera = VfxRotation.CreateCamera(Camera.ViewMatrix);
                SimdVector3 forward = Camera.CameraState.TargetPosition - Camera.CameraState.Position;
                if (forward != SimdVector3.Zero)
                    forward = SimdVector3.Normalize(forward);
                camera.Translation = Camera.CameraState.Position + forward;
                return camera;
            }

            return GetExternalSpawnTransform();
        }

        private SimdVector3 GetSpawnDirection()
        {
            Matrix4x4 source = Actor != null && !SpawnedByProjectile ? Actor.Transform : BacSpawnSource;
            SimdVector3 direction = SimdVector3.TransformNormal(-SimdVector3.UnitZ, source);
            if (EffectPart.OnGroundOnly && direction.Y > 0f)
                direction.Y = 0f;
            return direction == SimdVector3.Zero ? direction : SimdVector3.Normalize(direction);
        }

        private void UpdateAttachment(Matrix4x4 attachTransform, bool initialize)
        {
            Matrix4x4 attachRotation = attachTransform;
            attachRotation.Translation = SimdVector3.Zero;
            if (initialize || EffectPart.PositionUpdate)
            {
                PreviousAttachmentPosition = initialize ? BacSpawnSource.Translation : AttachmentPosition;
                AttachmentPosition = attachTransform.Translation;
            }

            if (initialize || EffectPart.RotateUpdate)
            {
                switch (EffectPart.Orientation)
                {
                    case OrientationType.None:
                        AttachmentRotation = Matrix4x4.Identity;
                        break;
                    case OrientationType.User:
                        AttachmentDirection = GetSpawnDirection();
                        if (AttachmentDirection != SimdVector3.Zero)
                            AttachmentRotation = VfxRotation.CreateUser(EffectPart.I_06, AttachmentDirection);
                        break;
                    case OrientationType.AttachmentBone:
                        AttachmentRotation = attachRotation;
                        break;
                    case OrientationType.Camera:
                        AttachmentRotation = EffectPart.AttachementType == Attachment.Camera
                            ? attachRotation : VfxRotation.CreateCamera(Camera.ViewMatrix);
                        break;
                    case OrientationType.RotateMovement:
                        AttachmentRotation = VfxRotation.CreateMovement(EffectPart.I_06,
                            PreviousAttachmentPosition - AttachmentPosition, ref AttachmentDirection);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown effect orientation: {EffectPart.Orientation}.");
                }
            }

            if (initialize || EffectPart.PositionUpdate)
            {
                // UseBoneDirection rotates the offset. It does not aim the effect at a child bone.
                PositionSpace = !EffectPart.UseBoneDirection ? Matrix4x4.Identity :
                    EffectPart.AttachementType == Attachment.Bone || EffectPart.AttachementType == Attachment.Camera
                        ? attachRotation : AttachmentRotation;
            }

            SimdVector3 offset = new SimdVector3(EffectPart.PositionX, EffectPart.PositionY, EffectPart.PositionZ);
            Matrix4x4 transform = AttachmentRotation;
            transform.Translation = AttachmentPosition + SimdVector3.TransformNormal(offset, PositionSpace);
            Transform = transform;
        }

        public virtual void Simulate()
        {
            //Update();
        }

        public virtual void SeekNextFrame()
        {
            //Update();
        }

        public virtual void SeekPrevFrame()
        {

        }

        protected Matrix4x4 GetAdjustedTransform()
        {
            return CurrentRotation * Transform;
        }
    }
}
