using System;
using System.IO;
using XenoKit.Editor;
using Xv2CoreLib;
using Xv2CoreLib.BDM;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Character
{
    public class DamageManager
    {
        private ActorController controller;
        public bool HasEntry => BdmEntry != null;

        public Matrix4x4 HitPosition { get; private set; }
        public Actor Attacker { get; private set; }
        public Actor Victim => controller.Actor;
        public Move Move { get; private set; }
        public BDM_Entry BdmEntry { get; private set; }
        public Type0SubEntry BdmSubEntry { get; private set; }
        public float CurrentFrame = 0f;

        //Damage:
        public int HitDirectionAll = 0; //0 = Front, 1 = Back, 2 = Left, 3 = Right
        public int HitDirectionFrontBack = 0; //0 = Front, 1 = Back
        public SimdVector3 HitVector;

        //Pushback
        public bool UsePushback = false;
        public float PushbackStrength = 0f;
        public SimdVector3 KnockbackVelocity;

        //BAC
        private int SingleAnimation;
        private int KnockbackAnimation;
        private int FallAnimation;
        private int ImpactAnimation;
        private int RecoveryAnimation;

        //ActorState
        private ActorState[] ActorStates = new ActorState[4];
        private int ActorStateIdx = 0;

        public DamageManager(ActorController actorController)
        {
            controller = actorController;
        }

        public void InitBdmEntry(BDM_Entry bdmEntry, SimdVector3 damageDirection, Actor attacker, Move move, Matrix4x4 hitPosition)
        {
            ActorState previousState = controller.State;
            DamageType previousDamageType = BdmSubEntry?.DamageType ?? DamageType.None;
            ResetBdmEntry();
            Attacker = attacker;
            Move = move;
            HitPosition = hitPosition;
            SetDamageDirection(damageDirection);

            HitboxState hitboxState = HitDirectionFrontBack == 1 ? HitboxState.Back : HitboxState.Default;
            if (controller.Actor.ActorSlot == 1 && SceneManager.VictimHitboxState.HasValue)
                hitboxState = SceneManager.VictimHitboxState.Value;
            else
            {
                switch (previousState)
                {
                    case ActorState.GroundImpact:
                        hitboxState = HitboxState.GroundImpact;
                        break;
                    case ActorState.Knockback:
                    case ActorState.Falling:
                        hitboxState = previousDamageType == DamageType.Knockback1 || previousDamageType == DamageType.LightStaminaBreak
                            ? HitboxState.FloatingKnockback : HitboxState.PrimaryKnockback;
                        break;
                    case ActorState.SingleAnimation:
                        switch (previousDamageType)
                        {
                            case DamageType.Standard:
                            case DamageType.Heavy:
                            case DamageType.GuardBreak:
                            case DamageType.HoldStomach:
                            case DamageType.HoldEyes:
                                hitboxState = HitboxState.Stumble;
                                break;
                        }
                        break;
                }
            }

            if (bdmEntry.Type0Entries != null)
            {
                foreach (Type0SubEntry subEntry in bdmEntry.Type0Entries)
                {
                    if (subEntry.Index == (int)hitboxState)
                    {
                        BdmSubEntry = subEntry;
                        break;
                    }
                }
            }
            if (BdmSubEntry == null)
                throw new InvalidDataException($"BDM entry {bdmEntry.ID} has no subentry for {hitboxState} ({(int)hitboxState}).");
            BdmEntry = bdmEntry;

            bool isBackHit = HitDirectionFrontBack == 1;

            switch (BdmSubEntry.DamageType)
            {
                case DamageType.None:
                    UsePushback = true;
                    break;
                case DamageType.Block:
                    {
                        UsePushback = true;
                        SingleAnimation = controller.IsInAir ? BLOCK_AIR_SET[HitDirectionAll] : BLOCK_SET[HitDirectionAll];
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.GuardBreak:
                    {
                        SingleAnimation = controller.IsInAir ? BAC_STAMINA_BREAK_AIR : BAC_STAMINA_BREAK_GROUND;
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.Standard:
                    {
                        UsePushback = true;
                        int entryId = controller.IsInAir ? (HitDirectionAll * 2) + 1 : HitDirectionAll * 2;
                        SingleAnimation = GetStumbleEntry(BdmSubEntry.StumbleType) + entryId;
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.Heavy:
                    {
                        int stumbleId = GetHeavyStumbleEntry(BdmSubEntry.StumbleType);
                        int entryId = controller.IsInAir ? (HitDirectionAll * 2) + 1 : HitDirectionAll * 2;

                        //Heavy stumble 3 only has frontal and back hit animations
                        if (stumbleId == BAC_HEAVY_STUMBLE_3 && entryId > 3)
                            entryId = 0;

                        SingleAnimation = stumbleId + entryId;
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.HoldStomach:
                    {
                        SingleAnimation = 175;
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.HoldEyes:
                    {
                        SingleAnimation = controller.IsInAir ? 177 : 176;
                        SetActorStates(ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.Dazed:
                    SingleAnimation = 181;
                    RecoveryAnimation = 83;
                    SetActorStates(ActorState.SingleAnimation, ActorState.StatusRecovery);
                    break;
                case DamageType.Electric:
                    SingleAnimation = 179;
                    SetActorStates(ActorState.SingleAnimation);
                    break;
                case DamageType.Paralysis:
                    SingleAnimation = 183;
                    RecoveryAnimation = 184;
                    SetActorStates(ActorState.SingleAnimation, ActorState.StatusRecovery);
                    break;
                case DamageType.Knockback:
                case DamageType.Knockback5:
                case DamageType.Knockback6:
                case DamageType.Knockback7:
                case DamageType.Knockback8:
                case DamageType.Knockback9:
                    {
                        KnockbackAnimation = !isBackHit ? 265 : 267;
                        FallAnimation = !isBackHit ? 266 : 268;
                        ImpactAnimation = 74;
                        SetActorStates(ActorState.Knockback, ActorState.Falling, ActorState.GroundImpact);
                        break;
                    }
                case DamageType.Knockback1:
                case DamageType.LightStaminaBreak:
                    {
                        //Knockback, then recover with a stumble animation. No gravity phase
                        KnockbackAnimation = !isBackHit ? 269 : 271;
                        SingleAnimation = !isBackHit ? 270 : 272;
                        SetActorStates(ActorState.Knockback, ActorState.SingleAnimation);
                        break;
                    }
                case DamageType.Knockback2:
                    KnockbackAnimation = !isBackHit ? 168 : 169;
                    FallAnimation = KnockbackAnimation;
                    RecoveryAnimation = !isBackHit ? 91 : 92;
                    SetActorStates(ActorState.Knockback, ActorState.Falling, ActorState.RecoveryFromGround);
                    break;
                case DamageType.Knockback3:
                    KnockbackAnimation = !isBackHit ? 273 : 275;
                    FallAnimation = !isBackHit ? 274 : 276;
                    ImpactAnimation = !isBackHit ? 75 : 76;
                    SetActorStates(ActorState.Knockback, ActorState.Falling, ActorState.GroundImpact);
                    break;
                case DamageType.Knockback4:
                    KnockbackAnimation = 170;
                    FallAnimation = 266;
                    ImpactAnimation = 80;
                    SetActorStates(ActorState.Knockback, ActorState.Falling, ActorState.GroundImpact);
                    break;
                case DamageType.HeavyStaminaBreak:
                    KnockbackAnimation = !isBackHit ? 168 : 169;
                    FallAnimation = KnockbackAnimation;
                    ImpactAnimation = !isBackHit ? 91 : 92;
                    SetActorStates(ActorState.Knockback, ActorState.Falling, ActorState.GroundImpact);
                    break;
            }

            if (GetInitialActorState() == ActorState.Knockback)
            {
                SimdVector3 direction = new SimdVector3(-HitVector.X, 0f, -HitVector.Z);
                direction = direction.LengthSquared() > 0f ? SimdVector3.Normalize(direction) : SimdVector3.UnitZ;
                SimdVector3 localVelocity = direction * BdmSubEntry.KnockbackStrengthZ +
                    SimdVector3.Cross(direction, SimdVector3.UnitY) * BdmSubEntry.KnockbackStrengthX +
                    SimdVector3.UnitY * BdmSubEntry.KnockbackStrengthY;
                KnockbackVelocity = SimdVector3.TransformNormal(localVelocity, controller.Actor.Transform);
            }
        }

        public void ResetBdmEntry()
        {
            if (HasEntry && BdmSubEntry.DamageType != DamageType.Grab && BdmSubEntry.DamageType != DamageType.None)
                controller.ClearBacEntries();

            BdmEntry = null;
            BdmSubEntry = null;
            CurrentFrame = 0f;
            HitDirectionAll = 0;
            HitDirectionFrontBack = 0;
            UsePushback = false;
            PushbackStrength = 0f;
            KnockbackVelocity = SimdVector3.Zero;
            ActorStateIdx = 0;
            SetActorStates();
        }

        private void SetDamageDirection(SimdVector3 directionVector)
        {
            HitVector = directionVector;
            float xAbs = Math.Abs(directionVector.X);
            float zAbs = Math.Abs(directionVector.Z);

            if (directionVector.Z < 0 && zAbs > xAbs)
            {
                HitDirectionAll = 0; //Front
                HitDirectionFrontBack = 0;
            }
            else if (directionVector.Z > 0 && zAbs > xAbs)
            {
                HitDirectionAll = 1; //Back
                HitDirectionFrontBack = 1;
            }
            else if (directionVector.X < 0 && xAbs > zAbs)
            {
                HitDirectionAll = 2; //Left
            }
            else if (directionVector.X > 0 && xAbs > zAbs)
            {
                HitDirectionAll = 3; //Right
            }

            //If direction was left or right, then set the front and back direction to the next closest
            if(HitDirectionAll > 1 && directionVector.Z < 0)
            {
                HitDirectionFrontBack = 0;
            }
            else if (HitDirectionAll > 1 && directionVector.Z > 0)
            {
                HitDirectionFrontBack = 1;
            }
        }

        public void SetActorStates(params ActorState[] states)
        {
            for (int i = 0; i < ActorStates.Length; i++)
            {
                if (i < states.Length)
                {
                    ActorStates[i] = states[i];
                }
                else
                {
                    ActorStates[i] = ActorState.Null;
                }
            }
        }

        public ActorState GetInitialActorState()
        {
            return ActorStates[0];
        }

        public ActorState GetNextActorState()
        {
            ActorStateIdx++;
            return ActorStateIdx >= ActorStates.Length ? ActorState.Null : ActorStates[ActorStateIdx];
        }

        #region BAC IDs

        private const int BAC_BLOCK_STANDING = 60;
        private const int BAC_BLOCK_AIR = 61;
        private const int BAC_BLOCK_LEFT_STANDING = 62;
        private const int BAC_BLOCK_LEFT_AIR = 63;
        private const int BAC_BLOCK_RIGHT_STANDING = 64;
        private const int BAC_BLOCK_RIGHT_AIR = 65;

        private readonly int[] BLOCK_SET = new int[] { BAC_BLOCK_STANDING, BAC_BLOCK_STANDING, BAC_BLOCK_LEFT_STANDING, BAC_BLOCK_RIGHT_STANDING };
        private readonly int[] BLOCK_AIR_SET = new int[] { BAC_BLOCK_AIR, BAC_BLOCK_AIR, BAC_BLOCK_LEFT_AIR, BAC_BLOCK_RIGHT_AIR };

        private const int BAC_STAMINA_BREAK_GROUND = 66;
        private const int BAC_STAMINA_BREAK_AIR = 67;

        private const int BAC_STUMBLE_1 = 93;
        private const int BAC_STUMBLE_2 = 101;
        private const int BAC_STUMBLE_3 = 109;
        private const int BAC_STUMBLE_4 = 117;
        private const int BAC_STUMBLE_5 = 125;
        private const int BAC_STUMBLE_6 = 133;
        private const int BAC_STUMBLE_7 = 226;
        private const int BAC_STUMBLE_8 = 234;
        private const int BAC_STUMBLE_9 = 242;
        private const int BAC_HEAVY_STUMBLE_1 = 141;
        private const int BAC_HEAVY_STUMBLE_2 = 149;
        private const int BAC_HEAVY_STUMBLE_3 = 157;

        private static readonly int[,] STUMBLE_ENTRIES = new int[,]
        {
            { BAC_STUMBLE_3, BAC_STUMBLE_4, BAC_STUMBLE_8 },
            { BAC_STUMBLE_1, BAC_STUMBLE_2, BAC_STUMBLE_7 },
            { BAC_STUMBLE_5, BAC_STUMBLE_6, BAC_STUMBLE_9 }
        };
        private static readonly int[] HEAVY_STUMBLE_ENTRIES = new int[] { BAC_HEAVY_STUMBLE_2, BAC_HEAVY_STUMBLE_1, BAC_HEAVY_STUMBLE_3 };

        private int GetStumbleEntry(Stumble stumbleFlags)
        {
            int flags = (stumbleFlags & Stumble.AllStumbleSets) != 0 ? 0 : (int)stumbleFlags;
            int row = PickStumbleOption(flags & 0x7);
            int column = PickStumbleOption((flags >> 3) & 0x7);
            return STUMBLE_ENTRIES[row, column];
        }

        private int GetHeavyStumbleEntry(Stumble stumbleFlags)
        {
            return HEAVY_STUMBLE_ENTRIES[PickStumbleOption((int)stumbleFlags & 0x7)];
        }

        private static int PickStumbleOption(int flags)
        {
            if (flags == 0) flags = 0x7;
            int count = (flags & 1) + ((flags >> 1) & 1) + ((flags >> 2) & 1);
            int choice = Xv2CoreLib.Random.Range(0, count - 1);

            for (int index = 0; index < 3; index++)
            {
                if ((flags & (1 << index)) != 0 && choice-- == 0)
                    return index;
            }

            throw new InvalidOperationException("No stumble animation was selected.");
        }

        public int GetBacEntryForActorState(ActorState state)
        {
            switch(state)
            {
                case ActorState.SingleAnimation:
                    return SingleAnimation;
                case ActorState.Knockback:
                    return KnockbackAnimation;
                case ActorState.Falling:
                    return FallAnimation;
                case ActorState.GroundImpact:
                    return ImpactAnimation;
                case ActorState.RecoveryFromGround:
                case ActorState.StatusRecovery:
                    return RecoveryAnimation;
                default:
                    Log.Add($"ActorState {state} is not a valid damage state.", LogType.Error);
                    return -1;
            }
        }

        #endregion
    }
}
