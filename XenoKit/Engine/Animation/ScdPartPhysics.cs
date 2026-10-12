using BEPUphysics.CollisionShapes.ConvexShapes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using XenoKit.Editor;
using XenoKit.Engine.Collision;
using XenoKit.Engine.Scripting.BAC;
using Xv2CoreLib.BCS;
using Xv2CoreLib.SCD;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Animation
{
    /// <summary>
    /// Runs <see cref="ScdPhysics"/> for one physics part. Reads the character pose and writes the skinning matrices of the SCD skeleton.
    /// </summary>
    public sealed class ScdPartPhysics : IDisposable
    {
        //A larger root move in one update is treated as a teleport.
        private const float TeleportDistance = 2f;
        //A larger forward frame change in one update is treated as a seek.
        private const float MaxFrameAdvance = 2f;

        private static readonly Color ColliderColor = Color.Yellow;
        private static readonly Color BoneDrivenBodyColor = Color.DarkCyan;
        private static readonly Color DynamicBodyColor = Color.Cyan;
        private static readonly Color DeepContactColor = Color.Red;

        private readonly ScdPhysics physics;
        private readonly Xv2Skeleton scdSkeleton;
        private readonly int[] charaBoneIndices;
        private readonly int attachBoneIndex;
        private readonly int[] colliderBoneIndices;
        private readonly Matrix4x4[] boneWorld;
        private readonly Matrix4x4[] colliderBoneWorld;

        private bool needsReset = true;
        private float lastFrame;
        private Matrix4x4 lastRoot;
        private AnimationInstance lastAnimation;
        private BacEntryInstance lastBacEntry;
        private HitboxVisual[] debugShapes;
        private HitboxVisual[] deepContactShapes;
        private bool[] deepByShape;
        private bool checkStartOverlap;
        private readonly HashSet<string> reportedStartOverlaps = new HashSet<string>();
        private readonly string partName;

        public IReadOnlyList<string> Warnings => physics.Warnings;

        public void Reset()
        {
            needsReset = true;
        }

        /// <param name="charaBoneIndices">Character bone index for each SCD bone, or -1 for bones that only exist in the SCD skeleton.</param>
        public ScdPartPhysics(SCD_File scdFile, Xv2Skeleton scdSkeleton, int[] charaBoneIndices, Xv2Skeleton charaSkeleton, SkeletonData bcsSkeleton, Matrix4x4 charaWorld, string boneToAttach, string partName)
        {
            this.partName = partName;
            this.scdSkeleton = scdSkeleton;
            this.charaBoneIndices = charaBoneIndices;
            attachBoneIndex = charaSkeleton.GetBoneIndex(boneToAttach);

            if (attachBoneIndex == -1)
                throw new InvalidDataException($"SCD attachment bone \"{boneToAttach}\" was not found in the character skeleton.");

            List<Bone> colliderBones = new List<Bone>();
            List<int> colliderIndices = new List<int>();

            if (bcsSkeleton?.Bones != null)
            {
                foreach (Bone bcsBone in bcsSkeleton.Bones)
                {
                    int boneIndex = charaSkeleton.GetBoneIndex(bcsBone.BoneName);

                    if (boneIndex == -1) continue;

                    colliderBones.Add(bcsBone);
                    colliderIndices.Add(boneIndex);
                }
            }

            colliderBoneIndices = colliderIndices.ToArray();
            boneWorld = new Matrix4x4[scdSkeleton.Bones.Length];
            colliderBoneWorld = new Matrix4x4[colliderBoneIndices.Length];

            string[] boneNames = new string[scdSkeleton.Bones.Length];
            int[] parentIndices = new int[scdSkeleton.Bones.Length];

            for (int boneIndex = 0; boneIndex < boneNames.Length; boneIndex++)
            {
                boneNames[boneIndex] = scdSkeleton.Bones[boneIndex].Name;
                parentIndices[boneIndex] = scdSkeleton.Bones[boneIndex].ParentIndex;
            }

            FillWorldMatrices(charaSkeleton, charaWorld);
            physics = new ScdPhysics(scdFile, boneNames, parentIndices, boneWorld, colliderBones, colliderBoneWorld);
        }

        /// <param name="bacEntry">The BAC entry the character is playing, or null. A BAC loop keeps the same instance, a new or restarted entry does not.</param>
        public void Update(Xv2Skeleton charaSkeleton, Matrix4x4 charaWorld, AnimationInstance animation, float frame, BacEntryInstance bacEntry, bool isPlaying)
        {
            FillWorldMatrices(charaSkeleton, charaWorld);

            if (!ScdPhysics.Enabled)
            {
                needsReset = true;
                WriteSkinning(charaWorld);
                return;
            }

            Matrix4x4 root = charaSkeleton.Bones[attachBoneIndex].AbsoluteAnimationMatrix * charaWorld;
            float frameChange = frame - lastFrame;
            bool frameAdvanced = frameChange > 0f && frameChange <= MaxFrameAdvance;
            //A backward jump while playing is the timeline looping.
            bool looped = frameChange < 0f && (isPlaying || (animation != null && frame <= animation.StartFrame));
            bool seeked = frameChange != 0f && !frameAdvanced && !looped;
            bool rootChanged = root != lastRoot;
            bool teleported = (root.Translation - lastRoot.Translation).Length() > TeleportDistance;
            //A new BAC entry starts from rest. Animation changes inside a BAC entry keep the simulation, but a new animation picked with no BAC entry active starts from rest.
            bool sourceChanged = bacEntry != lastBacEntry || (bacEntry == null && animation != lastAnimation);

            lastFrame = frame;
            lastRoot = root;
            lastAnimation = animation;
            lastBacEntry = bacEntry;

            if (needsReset || sourceChanged || seeked || (teleported && !looped) || (looped && ScdPhysics.ResetOnLoop))
            {
                physics.Reset(boneWorld, colliderBoneWorld);
                needsReset = false;
                checkStartOverlap = true;
                WriteSkinning(charaWorld);
            }
            else if (isPlaying || frameAdvanced || looped)
            {
                physics.Step(boneWorld, colliderBoneWorld, root);
                WriteSkinning(charaWorld);

                if (checkStartOverlap)
                {
                    checkStartOverlap = false;

                    if (ScdPhysics.ShowColliders)
                        LogStartOverlaps();
                }
            }
            else if (rootChanged)
            {
                //No step this update, so the next step must not read this move as one frame of root speed.
                physics.SkipRootMove(root);
            }
        }

        public void Dispose()
        {
            DisposeVisuals(debugShapes);
            DisposeVisuals(deepContactShapes);
            debugShapes = null;
            deepContactShapes = null;
        }

        private static void DisposeVisuals(HitboxVisual[] visuals)
        {
            if (visuals == null)
                return;

            foreach (HitboxVisual visual in visuals)
                visual.Dispose();
        }

        /// <summary>
        /// The first step after a reset still has the contacts of the starting pose, so deep ones there are bodies and colliders that start inside each other.
        /// </summary>
        private void LogStartOverlaps()
        {
            Dictionary<string, float> pairDepths = new Dictionary<string, float>();
            physics.FindDeepContacts(new bool[physics.DebugShapeCount], pairDepths);

            foreach (KeyValuePair<string, float> pair in pairDepths)
            {
                if (reportedStartOverlaps.Add(pair.Key))
                    Log.Add($"{partName}: physics body / collider \"{pair.Key}\" start {pair.Value * 100f:0.0} cm inside each other.", LogType.Warning);
            }
        }

        /// <summary>
        /// Draw the colliders and bodies as wireframes at their current physics transforms.
        /// </summary>
        public void DrawDebugShapes()
        {
            if (!ScdPhysics.Enabled)
                return;

            if (debugShapes == null)
            {
                debugShapes = CreateDebugShapes(false);
                deepContactShapes = CreateDebugShapes(true);
                deepByShape = new bool[debugShapes.Length];
            }

            physics.FindDeepContacts(deepByShape, null);

            for (int index = 0; index < debugShapes.Length; index++)
            {
                physics.GetDebugShape(index, out _, out Matrix4x4 world, out _);
                (deepByShape[index] ? deepContactShapes : debugShapes)[index].Draw(world);
            }
        }

        private HitboxVisual[] CreateDebugShapes(bool deepContact)
        {
            HitboxVisual[] visuals = new HitboxVisual[physics.DebugShapeCount];

            for (int index = 0; index < visuals.Length; index++)
            {
                physics.GetDebugShape(index, out ConvexShape shape, out _, out ScdPhysics.DebugShapeKind kind);

                Color color = kind == ScdPhysics.DebugShapeKind.Collider ? ColliderColor :
                              kind == ScdPhysics.DebugShapeKind.DynamicBody ? DynamicBodyColor : BoneDrivenBodyColor;
                HitboxVisual visual = new HitboxVisual(Color.Transparent, deepContact ? DeepContactColor : color);

                visual.SetShape(shape);

                visuals[index] = visual;
            }

            return visuals;
        }

        private void FillWorldMatrices(Xv2Skeleton charaSkeleton, Matrix4x4 charaWorld)
        {
            Matrix4x4 root = charaSkeleton.Bones[attachBoneIndex].AbsoluteAnimationMatrix * charaWorld;

            for (int boneIndex = 0; boneIndex < boneWorld.Length; boneIndex++)
            {
                int charaBoneIndex = charaBoneIndices[boneIndex];
                int parentIndex = scdSkeleton.Bones[boneIndex].ParentIndex;

                if (charaBoneIndex != -1)
                    boneWorld[boneIndex] = charaSkeleton.Bones[charaBoneIndex].AbsoluteAnimationMatrix * charaWorld;
                else if (parentIndex != -1)
                    boneWorld[boneIndex] = scdSkeleton.Bones[boneIndex].RelativeMatrix * boneWorld[parentIndex];
                else
                    boneWorld[boneIndex] = scdSkeleton.Bones[boneIndex].RelativeMatrix * root;
            }

            for (int index = 0; index < colliderBoneIndices.Length; index++)
                colliderBoneWorld[index] = charaSkeleton.Bones[colliderBoneIndices[index]].AbsoluteAnimationMatrix * charaWorld;
        }

        private void WriteSkinning(Matrix4x4 charaWorld)
        {
            Matrix4x4.Invert(charaWorld, out Matrix4x4 inverseCharaWorld);

            for (int boneIndex = 0; boneIndex < boneWorld.Length; boneIndex++)
                scdSkeleton.Bones[boneIndex].SkinningMatrix = scdSkeleton.Bones[boneIndex].InverseBindPoseMatrix * boneWorld[boneIndex] * inverseCharaWorld;
        }
    }
}
