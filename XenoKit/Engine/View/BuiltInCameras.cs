using System;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource;
using SimdQuaternion = System.Numerics.Quaternion;
using SimdVector3 = System.Numerics.Vector3;
using XvRandom = Xv2CoreLib.Random;

namespace XenoKit.Engine.View
{
    /// <summary>
    /// Built-in BAC cameras: camera rows whose EAN File loads no camera EAN (shakes, victim focus, front and back shots, two-shots, arcs and holds).
    /// Each type runs on its own timer and changes the eye and target after the normal camera has been placed.
    /// </summary>
    public sealed class BuiltInCameras
    {
        private const float FrameTime = 1f / 60f;
        private const float ShotDistance = 2f;
        private const int TypeCount = 32;

        //The order the game updates the active types in. Later types overwrite earlier ones in the same frame.
        private static readonly int[] UpdateOrder = { 18, 24, 14, 0, 1, 2, 15, 21, 22, 23, 6, 8, 13, 10, 12, 17, 19, 7 };
        private static readonly bool[] IsShake = CreateTypeSet(0, 1, 2, 14, 15, 21, 22, 23);
        //These timers run at the user's time scale. The others count real frames.
        private static readonly bool[] UsesTimeScale = CreateTypeSet(7, 8, 12, 13, 17, 18, 19, 24);

        private readonly float[] timers = new float[TypeCount];
        //Only one shake runs at a time. Starting one stops the others.
        private int activeShake = -1;
        private SimdVector3 shakeOffset;

        //Smooth shake: a new random target every 0.1 seconds, reached with a straight line.
        private float smoothSegment;
        private SimdVector3 smoothPrevious, smoothTarget;

        //Type 18 eases toward its shot instead of jumping there.
        private bool behindStarted;
        private SimdVector3 behindEye, behindTarget;

        //Types 6 and 10 swing the shot with a damped spring.
        private SimdVector3 springOffset, springVelocity;
        private float wideArcSign, wideArcBlend;

        //Type 7 holds the camera where it was when the row started.
        private CameraState heldState;

        //Type 8 uses a fixed eye above the user when the row starts with the user airborne above the opponent.
        private bool airborneFocus;
        private SimdVector3 airborneEye;

        //Type 24 frames the two players where they stood when the row started.
        private SimdVector3 twoShotFirst, twoShotSecond;

        //Rows with transform modifiers replace these. They stay on until the next such row or a reset, even after the type timers end.
        private BacCameraSettings modifiers;
        private float modifierFrames;

        public bool IsActive => modifiers.Enabled || Array.Exists(timers, timer => timer > 0f);

        public static Actor GetOpponent()
        {
            return SceneManager.VictimEnabled && SceneManager.ActorsEnable[1] ? SceneManager.Actors[1] : null;
        }

        public void Clear()
        {
            Array.Clear(timers, 0, timers.Length);
            activeShake = -1;
            shakeOffset = SimdVector3.Zero;
            behindStarted = false;
            heldState = null;
            airborneFocus = false;
            modifiers = default;
        }

        /// <summary>
        /// Starts the camera type of a camera row. Rows of other types, such as EAN cameras with a missing file, are ignored.
        /// </summary>
        public void Start(BAC_Type10 row, CameraState current, Actor user, Actor opponent)
        {
            int type = (int)row.EanType;

            if (row.EnableTransformModifiers)
            {
                modifiers = new BacCameraSettings(row);
                modifierFrames = 0f;
            }

            if (type >= TypeCount || Array.IndexOf(UpdateOrder, type) < 0)
                return;

            timers[type] = (row.Duration + 1) * FrameTime;

            if (IsShake[type])
            {
                if (activeShake >= 0 && activeShake != type)
                    timers[activeShake] = 0f;

                activeShake = type;
                smoothSegment = 0f;
                smoothPrevious = smoothTarget = SimdVector3.Zero;
                return;
            }

            switch (type)
            {
                case 6:
                    springOffset = SimdVector3.Zero;
                    springVelocity = RandomDirection();
                    break;
                case 7:
                    heldState = current.Copy();
                    break;
                case 8:
                    airborneFocus = TryGetAirborneEye(user, opponent, out airborneEye);
                    break;
                case 10:
                    SimdVector3 aim = GetAimDirection(user, opponent);
                    springOffset = SimdVector3.Zero;
                    springVelocity = aim;
                    wideArcBlend = 0f;
                    //The arc swings to the side of the user's facing that the camera already sits on.
                    SimdVector3 facing = GetForward(user);
                    SimdVector3 toCamera = current.Position - user.Transform.Translation;
                    wideArcSign = toCamera.Z * facing.X - facing.Z * toCamera.X < 0f ? -1f : 1f;
                    break;
                case 18:
                    behindStarted = false;
                    break;
                case 24:
                    if (opponent == null)
                    {
                        timers[type] = 0f;
                        break;
                    }
                    twoShotFirst = GetBodyCenter(user);
                    twoShotSecond = GetBodyCenter(opponent);
                    break;
            }
        }

        /// <param name="advance">False while paused: the shot is drawn again without moving its timers, springs or shake.</param>
        public void Update(CameraState state, Actor user, Actor opponent, bool advance)
        {
            if (user == null) return;

            float timeScale = user.ActiveTimeScale;

            foreach (int type in UpdateOrder)
            {
                if (timers[type] <= 0f) continue;

                switch (type)
                {
                    case 0: Shake(state, 0.1f, advance); break;
                    case 14: Shake(state, 0.05f, advance); break;
                    case 1: Shake(state, 0.25f, advance); break;
                    case 2: Shake(state, 0.5f, advance); break;
                    case 15: Shake(state, 0.95f, advance); break;
                    case 21: SmoothShake(state, 0.25f, advance); break;
                    case 22: SmoothShake(state, 0.5f, advance); break;
                    case 23: SmoothShake(state, 1f, advance); break;
                    case 6: Arc(state, user, opponent, advance); break;
                    case 7: state.SetState(heldState); break;
                    case 8 when airborneFocus:
                        if (!AirborneVictimFocus(state, user, opponent)) timers[type] = 0f;
                        break;
                    case 8:
                    case 13:
                        if (!VictimFocus(state, user, opponent)) timers[type] = 0f;
                        break;
                    case 10: WideArc(state, user, opponent, timeScale, advance); break;
                    case 12: Shot(state, user, SimdVector3.Normalize(GetUp(user) - GetForward(user))); break;
                    case 17: Shot(state, user, GetForward(user)); break;
                    case 18: BehindShot(state, user, advance); break;
                    case 19: BehindLookBetween(state, user, opponent); break;
                    case 24: SideTwoShot(state); break;
                }

                if (advance)
                    timers[type] -= UsesTimeScale[type] ? FrameTime * timeScale : FrameTime;
            }

            //The game applies the modifiers to the shot the types made, with the world up axis.
            if (modifiers.Enabled)
            {
                modifiers.Apply(state, MathHelpers.Up, modifierFrames);

                if (advance)
                    modifierFrames += timeScale;
            }

            if (activeShake >= 0 && timers[activeShake] <= 0f)
                activeShake = -1;

            if (timers[18] <= 0f)
                behindStarted = false;
        }

        #region Shakes
        //The target moves by the full offset and the eye by a quarter of it, so the shot trembles instead of sliding.
        private void Shake(CameraState state, float strength, bool advance)
        {
            if (advance)
                shakeOffset = RandomOffset(strength);

            ApplyShakeOffset(state, shakeOffset);
        }

        private void SmoothShake(CameraState state, float strength, bool advance)
        {
            const float segmentTime = 0.1f;

            if (advance)
            {
                if (smoothSegment <= 0f)
                {
                    smoothPrevious = smoothTarget;
                    smoothTarget = RandomOffset(strength);
                    smoothSegment = segmentTime;
                }

                smoothSegment -= FrameTime;
            }

            float blend = smoothSegment > 0f ? 1f - smoothSegment / segmentTime : 1f;
            ApplyShakeOffset(state, SimdVector3.Lerp(smoothPrevious, smoothTarget, blend));
        }

        private static void ApplyShakeOffset(CameraState state, SimdVector3 offset)
        {
            state.Position += offset * 0.25f;
            state.TargetPosition += offset;
        }

        private static SimdVector3 RandomOffset(float strength)
        {
            float half = strength * 0.5f;
            return new SimdVector3(XvRandom.Range(-half, half), XvRandom.Range(-half, half), XvRandom.Range(-half, half));
        }

        private static SimdVector3 RandomDirection()
        {
            SimdVector3 direction = RandomOffset(2f);
            return direction.LengthSquared() > 1e-6f ? SimdVector3.Normalize(direction) : SimdVector3.UnitX;
        }
        #endregion

        #region Shots
        /// <summary>
        /// Looks at the opponent from beside the user, 30 degrees off the line between them.
        /// </summary>
        private static bool VictimFocus(CameraState state, Actor user, Actor opponent)
        {
            if (opponent == null) return false;

            SimdVector3 userPelvis = GetBone(user, BoneLinks.b_C_Pelvis);
            SimdVector3 opponentPelvis = GetBone(opponent, BoneLinks.b_C_Pelvis);
            SimdVector3 away = userPelvis - opponentPelvis;

            if (away.LengthSquared() < 1e-6f) return false;

            away = SimdVector3.Normalize(away);
            float size = GetBodySize(user);
            float horizontal = Math.Max(size * 2.8f, 2.5f);
            float vertical = Math.Max(size * 2.3f, 1f);
            const float cos30 = 0.8660254f;
            const float sin30 = 0.5f;

            state.Position = userPelvis + new SimdVector3(
                horizontal * (cos30 * away.X + sin30 * away.Z),
                vertical * away.Y,
                horizontal * (-sin30 * away.X + cos30 * away.Z));
            state.TargetPosition = opponentPelvis;
            return true;
        }

        //The eye is a point fixed above the user's g_x_LND bone at the row start. Landing ends the camera.
        private static bool TryGetAirborneEye(Actor user, Actor opponent, out SimdVector3 eye)
        {
            eye = SimdVector3.Zero;
            int landBone = user.Skeleton.BAC_BoneIndices[(int)BoneLinks.g_x_LND];

            if (opponent == null || landBone == -1 || !IsAirborne(user) || opponent.Transform.Translation.Y >= user.Transform.Translation.Y)
                return false;

            SimdVector3 local = new SimdVector3(0.83665f, 4.80272f, 0.19977f) * GetBodySize(user);
            eye = SimdVector3.Transform(local, user.GetAbsoluteBoneMatrix(landBone));
            return true;
        }

        private bool AirborneVictimFocus(CameraState state, Actor user, Actor opponent)
        {
            if (opponent == null || !IsAirborne(user)) return false;

            state.Position = airborneEye;
            state.TargetPosition = GetBone(opponent, BoneLinks.b_C_Chest);
            return true;
        }

        //Looks at the user's pelvis from the given direction.
        private static void Shot(CameraState state, Actor user, SimdVector3 direction)
        {
            SimdVector3 pelvis = GetBone(user, BoneLinks.b_C_Pelvis);
            state.Position = pelvis + direction * ShotDistance;
            state.TargetPosition = pelvis;
        }

        //Eases toward the shot by a fixed share each frame, independent of time scale.
        private void BehindShot(CameraState state, Actor user, bool advance)
        {
            const float ease = 0.02f;
            SimdVector3 pelvis = GetBone(user, BoneLinks.b_C_Pelvis);
            SimdVector3 eye = pelvis - GetForward(user) * ShotDistance;

            if (!behindStarted)
            {
                behindEye = eye;
                behindTarget = pelvis;
                behindStarted = true;
            }

            if (advance)
            {
                behindEye += (eye - behindEye) * ease;
                behindTarget += (pelvis - behindTarget) * ease;
            }

            state.Position = behindEye;
            state.TargetPosition = behindTarget;
        }

        private static void BehindLookBetween(CameraState state, Actor user, Actor opponent)
        {
            Shot(state, user, -GetForward(user));

            if (opponent != null)
            {
                SimdVector3 middle = (user.Transform.Translation + opponent.Transform.Translation) * 0.5f;
                state.TargetPosition = middle;
                state.Position = new SimdVector3(state.Position.X, middle.Y, state.Position.Z);
            }
        }

        //Side view of both players, as far from them as they are from each other.
        private void SideTwoShot(CameraState state)
        {
            SimdVector3 between = twoShotFirst - twoShotSecond;
            float distance = between.Length();

            if (distance < 1e-4f) return;

            SimdVector3 direction = between / distance;
            SimdVector3 middle = twoShotSecond + direction * (distance * 0.5f);
            SimdVector3 side = SimdVector3.Cross(MathHelpers.Up, direction);

            if (side.LengthSquared() < 1e-6f) return;

            state.TargetPosition = middle;
            state.Position = middle + SimdVector3.Normalize(side) * distance;
        }
        #endregion

        #region Arcs
        //A damped spring per frame: the velocity pulls back toward zero offset and loses 44% each frame.
        private void StepSpring(float stiffness)
        {
            springVelocity = (springVelocity - springOffset * stiffness) * 0.56f;
            springOffset += springVelocity;
        }

        private void Arc(CameraState state, Actor user, Actor opponent, bool advance)
        {
            if (advance)
                StepSpring(Math.Max(0f, Math.Min(GetBodySize(user), 2f)) * 2.5f);

            //The game uses the point where the opponent was last hit. Its body center is close to that.
            Actor other = opponent ?? user;
            SimdVector3 focus = ((GetBone(user, BoneLinks.b_C_Chest) + GetBone(other, BoneLinks.b_C_Chest)) * 0.5f + GetBodyCenter(other)) * 0.5f;
            SimdVector3 target = state.TargetPosition + (focus - state.TargetPosition) * 0.3f;
            SimdVector3 eye = state.Position + (target - state.Position) * 0.3f;

            state.TargetPosition = target + springOffset;
            state.Position = eye + springOffset;
        }

        private void WideArc(CameraState state, Actor user, Actor opponent, float timeScale, bool advance)
        {
            const float lift = 0.4f;
            SimdVector3 pelvis = GetBone(user, BoneLinks.b_C_Pelvis);
            SimdVector3 target = pelvis;

            if (opponent != null)
            {
                SimdVector3 opponentPelvis = GetBone(opponent, BoneLinks.b_C_Pelvis);

                if (SimdVector3.DistanceSquared(opponentPelvis, pelvis) < 9f)
                    target = (pelvis + opponentPelvis) * 0.5f;
            }

            float size = GetBodySize(user);
            float blend = Math.Max(0f, Math.Min(wideArcBlend, 1f));
            float radius = (2f - blend) * blend * size + (1f - blend) * (1f - blend) * 0.38f;

            //Tilt the view 10 degrees down, then turn it 45 degrees to the side the camera started on.
            SimdQuaternion turn = SimdQuaternion.CreateFromAxisAngle(SimdVector3.UnitY, wideArcSign * -(float)Math.PI / 4f) * SimdQuaternion.CreateFromAxisAngle(SimdVector3.UnitX, (float)Math.PI / 18f);
            SimdVector3 direction = GetAimDirection(user, opponent) * (Math.Max(1f, Math.Min(size, 1.25f)) + 1.8f);
            SimdVector3 eye = pelvis + SimdVector3.Transform(direction, turn) * radius;

            if (advance)
            {
                wideArcBlend = 0.5f;
                StepSpring(size * 2.5f * timeScale);
            }

            SimdVector3 raise = new SimdVector3(0f, lift, 0f);
            state.Position = eye + springOffset + raise;
            state.TargetPosition = target + springOffset + raise;
        }
        #endregion

        #region Actor helpers
        private static bool[] CreateTypeSet(params int[] types)
        {
            bool[] set = new bool[TypeCount];

            foreach (int type in types)
                set[type] = true;

            return set;
        }

        private static SimdVector3 GetBone(Actor actor, BoneLinks bone)
        {
            int index = actor.Skeleton.BAC_BoneIndices[(int)bone];
            return index != -1 ? actor.GetAbsoluteBoneMatrix(index).Translation : actor.Transform.Translation;
        }

        private static SimdVector3 GetForward(Actor actor)
        {
            SimdVector3 forward = actor.Transform.GetForward();
            return forward.LengthSquared() > 1e-6f ? SimdVector3.Normalize(forward) : MathHelpers.Forward;
        }

        private static SimdVector3 GetUp(Actor actor)
        {
            SimdVector3 up = SimdVector3.TransformNormal(SimdVector3.UnitY, actor.Transform);
            return up.LengthSquared() > 1e-6f ? SimdVector3.Normalize(up) : MathHelpers.Up;
        }

        //The game counts an actor as airborne when it is more than 0.2 above the ground. The preview ground is at height 0.
        private static bool IsAirborne(Actor actor)
        {
            return actor.Transform.Translation.Y > 0.2f;
        }

        private static SimdVector3 GetAimDirection(Actor user, Actor opponent)
        {
            if (opponent != null)
            {
                SimdVector3 toOpponent = opponent.Transform.Translation - user.Transform.Translation;

                if (toOpponent.LengthSquared() > 1e-6f)
                    return SimdVector3.Normalize(toOpponent);
            }

            return GetForward(user);
        }

        private static SimdVector3 GetBodyCenter(Actor actor)
        {
            return GetBone(actor, BoneLinks.b_C_Spine1);
        }

        //Body height ratio, 1 for a normal sized character.
        private static float GetBodySize(Actor actor)
        {
            float[] size = actor.CharacterData?.BcsFile?.File?.F_48;
            return size != null && size.Length > 1 && size[1] > 0f ? size[1] : 1f;
        }
        #endregion
    }
}
