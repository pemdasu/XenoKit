using System;
using Xv2CoreLib;
using XenoKit.Editor;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Scripting.BAC;
using XenoKit.Engine.Scripting.BSA;
using Xv2CoreLib.EAN;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BPE;
using XenoKit.Controls;
using XenoKit.Engine.Character;
using XenoKit.Engine.Collision;
using XenoKit.Engine.Shader;
using Microsoft.Xna.Framework;
using Xv2CoreLib.FPF;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;
using SimdVector4 = System.Numerics.Vector4;

namespace XenoKit.Engine
{
    public class Actor : RenderObject, ISkinned
    {
        //TODO: Clean this mess up!
        public override EngineObjectTypeEnum EngineObjectType => EngineObjectTypeEnum.Actor;

        public int Team => ActorSlot == 1 ? 1 : 0;
        //The victim only counts as an opponent while it is enabled.
        public Actor Opponent => ActorSlot == 1 ? SceneManager.Actors[0] : (SceneManager.VictimEnabled ? SceneManager.Actors[1] : null);
        public int ActorSlot = 0;
        public readonly EmRandom MobRandom = new EmRandom();
        // Stands in for the C runtime rand() stream. The real seed is unknown, so values only repeat per reset.
        public System.Random CrtRandom { get; private set; } = new System.Random(0);
        public CharaPartSet PartSet;
        public int ForceDytOverride = -1;
        //Set by BAC DYT Blend rows. A main DYT of -1 uses the DYT of each part.
        public int BacDytMain = -1;
        public int BacDytBlend = -1;
        public float BacDytRatio;
        //Set by BAC Material Parameter rows, by part type and material index
        public readonly System.Collections.Generic.Dictionary<(int part, int material), float[]> BacMatScale0 = new System.Collections.Generic.Dictionary<(int part, int material), float[]>();
        public bool IsVisible = true;
        //Cleared by BAC Function 0x17 while it is active. Tracking skips actors that cannot be tracked.
        public bool CanBeTracked = true;
        public FPF_File FpfPreviewFile = null;
        public string FpfPreviewPath = null;
        public FpfPoseMatrix FpfPreviewPoseMatrix = FpfPoseMatrix.AbsolutePoseTransform;
        public FpfPoseMatrix FpfPreviewSkinOffsetMatrix = FpfPoseMatrix.None;
        public FpfSkinOffsetMode FpfPreviewSkinOffsetMode = FpfSkinOffsetMode.InverseBindOffsetPose;
        public bool FpfPreviewUsePlacementOffset = true;

        //TimeScale:
        public int BdmTimeScaleDuration = 0;
        public float BdmTimeScale = 1f;
        public float BacTimeScale = 1f;
        public float AnimationTimeScale = 1f;
        public float ActiveTimeScale
        {
            get
            {
                return (SceneManager.CurrentSceneState == EditorTabs.Action) ? BacTimeScale * AnimationTimeScale * BdmTimeScale : 1f;
            }
        }

        //Components:
        private DebugSkeleton _debugSkeleton;
        public VisualSkeleton _visualSkeleton;
        private Xv2Skeleton _skeleton = null;

        public Xv2Skeleton Skeleton
        {
            get
            {
                return _skeleton;
            }
            set
            {
                if (_skeleton != value)
                {
                    _skeleton = value;
                    AnimationPlayer = new AnimationPlayer(_skeleton, this); //Skeleton was changed, so animationPlayer needs to be reloaded
                }
            }
        }
        public AnimationPlayer AnimationPlayer { get; private set; }
        public ActionControl ActionControl { get; private set; }
        public ActorController Controller { get; private set; }

        //Hitbox:
        public float[] AABB { get; private set; }
        public bool HitboxEnabled = true;

        //Shader Effects
        public ActorShaderExtraParameters ShaderParameters;

        //Character Source Files:
        public Xv2Character CharacterData { get; private set; }

        //The aura BPE body outline and its own frame. It does not follow the BAC entry.
        private BacScreenEffectInstance[] auraOutline;
        private float auraOutlineFrame;
        private bool auraOutlineShown;
        private BacScreenEffectEvaluator auraOutlineEvaluator;
        private readonly BacScreenEffectState auraOutlineState = new BacScreenEffectState();
        public Move Moveset { get; private set; }
        public EAN_File FceEanFile
        {
            get
            {
                //PartSet defined fce.ean file
                if (PartSet?.FceEan != null)
                    return PartSet.FceEan;

                //Default fce.ean for this character
                return CharacterData?.MovesetFiles?.GetFaceEanFile();
            }
        }
        public EAN_File FceEyeEanFile
        {
            get
            {
                //PartSet defined fce.ean file
                if (PartSet?.FceForeheadEan != null)
                    return PartSet.FceForeheadEan;

                //Default fce.ean for this character
                return CharacterData?.MovesetFiles?.GetFaceEanFile();
            }
        }

        //Voice:
        public int Voice { get; set; }
        /// <summary>
        /// A character code alias to use for skill voice lines.
        /// </summary>
        public string SkillVoiceAlias { get; set; }

        //Misc
        public string ShortName => CharacterData?.CmsEntry?.ShortName;
        public AnimationTabView AnimationViewInstance => AnimationTabView.Instance; //Required for the ISkinned interface. Associates a GUI view with the underlying skinned object.

        //Eye UV Scroll. (These are sized 4 arrays because thats what the shaders expect, though we only need the first 2 values)
        public float[] EyeIrisLeft_UV { get; set; } = new float[4];
        public float[] EyeIrisRight_UV { get; set; } = new float[4];
        public bool BacEyeMovementUsed = false;

        public Actor(Xv2Character character, int initialPartSet = 0)
        {
            ResetTransparencyValues();
            Skeleton = CompiledObjectManager.GetCompiledObject<Xv2Skeleton>(character.EskFile.File);
            Name = character.Name[0];
            ResetPosition();
            ActionControl = new ActionControl(this);
            Controller = new ActorController(this);
            Moveset = new Move(character);
            CharacterData = character;
            AABB = new float[6];

            _debugSkeleton = new DebugSkeleton();
            _visualSkeleton = new VisualSkeleton(this);

            //Create animation player
            AnimationPlayer = new AnimationPlayer(Skeleton, this);

            //Load PartSet
            PartSet = new CharaPartSet(this, initialPartSet);
        }

        #region Transform
        public override Matrix4x4 Transform => RootMotionTransform * ActionMovementTransform * BaseTransform;

        public Matrix4x4 RootMotionTransform = Matrix4x4.Identity;
        public Matrix4x4 ActionMovementTransform = Matrix4x4.Identity;
        public Matrix4x4 BaseTransform = Matrix4x4.Identity;

        /// <summary>
        /// Called when an BAC Animation is finished. All movement from that animation will be added onto the BaseTransform and preserved, while discarding all other Root Motion.
        /// </summary>
        public void MergeTransforms()
        {
            BaseTransform = ActionMovementTransform * BaseTransform;
            ActionMovementTransform = Matrix4x4.Identity;
            RootMotionTransform = Matrix4x4.Identity;
        }

        public void ApplyTranslation(SimdVector3 translation)
        {
            BaseTransform *= Matrix4x4.CreateTranslation(translation);
        }

        public void ResetRandom()
        {
            MobRandom.Seed((uint)ActorSlot);
            CrtRandom = new System.Random(ActorSlot);
        }

        public void ResetPosition()
        {
            SimdVector3 pos;
            Matrix4x4 rotation;

            switch (ActorSlot)
            {
                case 1:
                    pos = SceneManager.VictimPosition;
                    rotation = Matrix4x4.CreateFromQuaternion(SceneManager.VictimRotation.EulerToQuaternion());
                    break;
                default:
                    pos = SimdVector3.Zero;
                    rotation = Matrix4x4.Identity;
                    break;
            }

            BaseTransform = rotation * Matrix4x4.CreateTranslation(pos);
            ActionMovementTransform = Matrix4x4.Identity;
            RootMotionTransform = Matrix4x4.Identity;
        }

        public void ResetState(bool keepAnimation = false)
        {
            ResetTransparencyValues();
            ResetBacMaterialValues();
            ClearBodyOutlineValues();
            BdmTimeScaleDuration = 0;
            BdmTimeScale = 1f;
            BacTimeScale = 1f;
            Controller.ResetState(keepAnimation);

            bool retainActionPosition = SceneManager.RetainActionMovement && SceneManager.IsOnTab(EditorTabs.Action);

            //In some cases we might not want to reset animations, such as Animation > Camera (and vice versa) tab changes.
            //TODO: Check for a better place to put this. Dont want UI related stuff mixed in here
            if (!keepAnimation)
            {
                PartSet.StopScdAnimations();
                AnimationPlayer.ClearCurrentAnimation(true, !retainActionPosition);
            }

            if (!retainActionPosition)
                ResetPosition();
        }

        public void ResetBacMaterialValues()
        {
            BacDytMain = -1;
            BacDytBlend = -1;
            BacDytRatio = 0f;
            BacMatScale0.Clear();
        }

        public void ResetTransparencyValues()
        {
            ShaderParameters.ShaderPath = ActorShaderPath.Default;
            ShaderParameters.g_vColor4_PS = SimdVector4.One;
            ShaderParameters.g_vParam8_PS = new SimdVector4(0f, 0f, 1f, 1f);
            ShaderParameters.g_vParam9_PS = SimdVector4.One;
            ShaderParameters.g_vParam10_PS = new SimdVector4(0f, 0f, 1f, 0f);
            ShaderParameters.g_vUserFlag1_VS = SimdVector4.Zero;
        }

        public void StartAuraOutline(BPE_Entry outline)
        {
            auraOutline = new[] { new BacScreenEffectInstance(outline, 0, 0) };
            auraOutlineFrame = 0f;

            if (auraOutlineEvaluator == null)
                auraOutlineEvaluator = new BacScreenEffectEvaluator(this);
        }

        public void StopAuraOutline()
        {
            auraOutline = null;

            if (auraOutlineShown)
            {
                ClearBodyOutlineValues();
                auraOutlineShown = false;
            }
        }

        //The aura outline loops until the aura stops. A BAC screen effect outline from this frame wins over it.
        private void UpdateAuraOutline()
        {
            if (auraOutline == null || ShaderParameters.BodyOutlineActive) return;

            auraOutlineEvaluator.Update(auraOutline, auraOutlineState, (int)auraOutlineFrame, bpeIndex => { }, () => { });
            auraOutlineShown = ShaderParameters.BodyOutlineActive;

            if (ViewportInstance.IsPlaying)
                auraOutlineFrame += ActiveTimeScale;
        }

        public void ClearBodyOutlineValues()
        {
            ShaderParameters.BodyOutlineActive = false;
            ShaderParameters.BodyOutlineColor = SimdVector4.Zero;
            ShaderParameters.BodyOutlineParam2 = SimdVector4.Zero;
            ShaderParameters.BodyOutlineParam3 = SimdVector4.Zero;
        }

        #endregion

        #region GameLoop
        public override void Update()
        {
            DrawThisFrame = true;
            IsVisible = true;
            HitboxEnabled = true;

            //Reset Eye positions to their defaults
            EyeIrisLeft_UV[0] = EyeIrisLeft_UV[1] = EyeIrisRight_UV[0] = EyeIrisRight_UV[1] = 0;
            BacEyeMovementUsed = false;

            //Disable the ActorController for the main actor, and enable it for the victim
            //Will need to adjust this when adding BDM preview support
            if(ActorSlot == 0 && Controller.State != ActorState.Null)
            {
                Controller.State = ActorState.Null;
            }
            else if(ActorSlot == 1 && Controller.State == ActorState.Null)
            {
                Controller.State = ActorState.Idle;
            }

            if (ViewportInstance.IsPlaying)
                UpdateBdmTimeScale();

            //Clear the aura outline from the last frame, so a BAC outline set this frame can be told apart from it
            if (auraOutlineShown)
            {
                ClearBodyOutlineValues();
                auraOutlineShown = false;
            }

            Controller.Update();
            ActionControl.Update();
            UpdateAuraOutline();

            if (AnimationPlayer != null && Skeleton != null)
                AnimationPlayer.Update(Matrix4x4.Identity);

            FpfPosePreview.Apply(this);

            _visualSkeleton.Update(Skeleton.Bones);

            PartSet.Update();
            FpfPosePreview.ApplyAdditionalSkeletons(this);

        }

        public override void DelayedUpdate()
        {
            ActionControl.DelayedUpdate();
        }

        /// <summary>
        /// Simulate a frame. 
        /// </summary>
        public void Simulate(bool fullAnimUpdate, bool advance, bool useTimeScale = false)
        {
#if DEBUG
            if (Viewport.Instance.IsPlaying) throw new InvalidOperationException("invalid operation, cannot do this while Viewport.IsPlaying is true.");
#else
            if (Viewport.Instance.IsPlaying)
            {
                Log.Add("Character.Simulate(): invalid operation, cannot do this while Viewport.IsPlaying is true.", LogType.Error);
                return;
            }
#endif
            HitboxEnabled = true;

            UpdateBdmTimeScale();
            Controller.Simulate();

            if (ActorSlot != 0)
            {
                ActionControl.Update();
            }

            if (AnimationPlayer != null && Skeleton != null)
            {
                AnimationPlayer.Simulate(fullAnimUpdate || Animation.ScdPhysics.Enabled, advance, useTimeScale);
            }

            FpfPosePreview.Apply(this);
            PartSet.Update();
            FpfPosePreview.ApplyAdditionalSkeletons(this);

        }

        public override void Draw()
        {
            if (!ViewportInstance.RenderCharacters || !IsVisible) return;

            PartSet.Draw();

            if(RenderSystem.CurrentDrawPass == Rendering.DrawPass.Opaque)
            {
                if (AnimationPlayer != null && SceneManager.ShowDebugBones)
                    _debugSkeleton.Draw(Skeleton.Bones, Skeleton.Bones, Transform);

                if (AnimationPlayer != null)
                    _visualSkeleton.Draw(Skeleton.Bones, Skeleton.Bones, Transform);
            }
        }

        public override void DrawPass(bool normalPass)
        {
            if (!ViewportInstance.RenderCharacters || !IsVisible) return;
            PartSet.DrawSimple(normalPass);
        }

        /// <summary>
        /// Draw the BAC and projectile hitboxes. Call this after the scene renders, so that the hitboxes show over it.
        /// </summary>
        public void DrawHitboxes()
        {
            if (!ViewportInstance.RenderCharacters || !IsVisible) return;
            ActionControl.Draw();
        }

        #endregion

        #region TimeScale
        private void UpdateBdmTimeScale()
        {
            if(BdmTimeScaleDuration > 0)
            {
                BdmTimeScaleDuration--;
            }
            else if(BdmTimeScaleDuration == 0)
            {
                BdmTimeScale = 1f;
            }
        }

        public void SetBacTimeScale(float timeScale, bool reset)
        {
            if (reset) BacTimeScale = 1f;
            BacTimeScale *= timeScale;
        }

        #endregion

        public bool HitTest(BacHitbox hitbox)
        {
            if (hitbox.IsSupported && hitbox.CanHit() && CanBeHit(hitbox.IntersectsSphere))
            {
                Xv2CoreLib.BDM.BDM_File bdm = Files.Instance.GetBdmFile(hitbox.Hitbox.bdmFile, hitbox.BacEntry.SkillMove, hitbox.BacEntry.User, false);

                Xv2CoreLib.BDM.BDM_Entry entry = bdm?.GetEntry(hitbox.Hitbox.BdmEntryID);
                if (entry == null)
                    return false;

                Controller.ApplyDamageState(entry, hitbox.GetRelativeDirection(Transform), hitbox);
                hitbox.RecordHit();
                return true;
            }

            return false;
        }

        public bool CanBeHit(BoundingBox bounds, BsaHitboxPreview projectileHitbox = null)
        {
            return CanBeHit(sphere => sphere.Intersects(bounds) && (projectileHitbox == null || projectileHitbox.IntersectsSphere(sphere)));
        }

        private bool CanBeHit(Func<BoundingSphere, bool> hurtboxHit)
        {
            return !(ActorSlot == 1 && SceneManager.VictimInvulnerable) &&
                HitboxEnabled && Controller.InvulnerabilityFrames <= 0 && Controller.FreezeActionFrames <= 0 &&
                HurtboxIntersects(hurtboxHit);
        }

        private bool HurtboxIntersects(Func<BoundingSphere, bool> hurtboxHit)
        {
            // The game checks these IDs directly; CMS I_16 = 4 also includes other characters.
            bool largeBody = ShortName == "BRL" || ShortName == "TM1";
            bool giantBody = ShortName == "DM2";
            float pelvisRadius = giantBody ? 1.2f : largeBody ? 0.8f : 0.5f;
            float headRadius = giantBody ? 0.8f : largeBody ? 0.5f : 0.2f;
            float pelvisOffset = largeBody ? -0.4f : -0.2f;

            int bodyId = Skeleton.GetActiveBoneScaleId();
            var cbsEntry = CharacterData.CbsEntry?.Find(entry => entry.BodyId == bodyId);
            if (cbsEntry != null)
            {
                if (cbsEntry.F_20 > 0f)
                    pelvisRadius *= cbsEntry.F_20;
                if (cbsEntry.F_16 > 0f)
                    headRadius *= cbsEntry.F_16;
            }

            return BoneSphereIntersects(BoneLinks.b_C_Pelvis, new SimdVector3(0f, pelvisOffset, 0f), pelvisRadius, hurtboxHit) ||
                BoneSphereIntersects(BoneLinks.b_C_Head, SimdVector3.Zero, headRadius, hurtboxHit) ||
                (largeBody && BoneSphereIntersects(BoneLinks.b_C_Chest, SimdVector3.Zero, 0.6f, hurtboxHit));
        }

        private bool BoneSphereIntersects(BoneLinks bone, SimdVector3 offset, float radius, Func<BoundingSphere, bool> hurtboxHit)
        {
            int boneIndex = Skeleton.BAC_BoneIndices[(int)bone];
            if (boneIndex < 0)
                throw new InvalidOperationException($"Character {ShortName} has no {bone} bone for its hurtbox.");

            SimdVector3 center = SimdVector3.Transform(offset, GetAbsoluteBoneMatrix(boneIndex));
            BoundingSphere sphere = new BoundingSphere(Extensions.ToXna(center), radius);
            return hurtboxHit(sphere);
        }

        public Matrix4x4 GetAbsoluteBoneMatrix(int index)
        {
            //All b_C_Base movement is moved onto Transform. The bone is always at Identity, so this is needed to get the correct matrix.

            //b_C_Hand is a special bone that is a combination of b_R_Hand and b_L_Hand
            if (index == Xv2Skeleton.b_C_Hand_Magic)
                return Skeleton.GetHandBarycenter() * Transform;

            return Skeleton.Bones[index].AbsoluteAnimationMatrix * Transform;
        }

        public SimdVector3 GetBoneCurrentAbsolutePosition(string name)
        {
            SimdVector3 pos = AnimationPlayer.GetCurrentAbsoluteMatrix(name).Translation;

            return pos + Transform.Translation;
        }
    
        public Xv2Character ConvertToXv2Character()
        {
            if (Moveset == null) throw new InvalidOperationException("Character.ConvertToXv2Character: No moveset found.");
            if (CharacterData == null) throw new InvalidOperationException("Character.ConvertToXv2Character: No characterData found.");

            CharacterData.MovesetFiles = Moveset.Files;
            return CharacterData;
        }
    }
}
