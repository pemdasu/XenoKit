using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BCS;
using Xv2CoreLib.FPF;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine
{
    public partial class CharaPart
    {
        #region Rendering
        public override void Update()
        {
            if (IsTexturesDirty && EmbFile != null)
            {
                Textures = Xv2Texture.LoadTextureArray(EmbFile);
            }

            if (IsDytDirty && DytFile != null)
            {
                Dyts = Xv2Texture.LoadTextureArray(DytFile);
            }

            if (!IsPhysicsPart)
            {
                foreach (CharaPart physicsPart in PhysicsParts)
                {
                    physicsPart.UpdatePhysicsPart();
                }
            }
        }

        public void DisposeScdPhysics()
        {
            ScdPhysics?.Dispose();
            ScdPhysics = null;
            ScdAnimation?.Dispose();
            ScdAnimation = null;

            if (PhysicsParts == null)
                return;

            foreach (CharaPart physicsPart in PhysicsParts)
                physicsPart?.DisposeScdPhysics();
        }

        public void ReloadScdPhysics()
        {
            if (IsPhysicsPart && Skeleton != null)
            {
                ScdPhysics?.Dispose();
                ScdPhysics = LoadScdPhysics();
            }
            if (PhysicsParts != null)
                foreach (CharaPart part in PhysicsParts) part?.ReloadScdPhysics();
        }

        public void UpdatePhysicsPart()
        {
            if (ScdAnimation?.IsActive == true)
                ScdAnimation.Apply(chara.Skeleton);
            else if (ScdPhysics != null)
                ScdPhysics.Update(chara.Skeleton, chara.Transform, chara.AnimationPlayer?.PrimaryAnimation, chara.AnimationPlayer?.PoseFrame ?? 0f, chara.ActionControl?.BacPlayer?.BacEntryInstance, ViewportInstance.IsPlaying);
            else
                Skeleton?.ScdUpdate(chara.Skeleton, SkeletoBoneIndices);
        }

        public void ControlScdAnimation(BAC_Type18 row)
        {
            if (PhysicsParts == null)
                return;

            foreach (CharaPart physicsPart in PhysicsParts)
            {
                if (physicsPart?.ScdAnimation == null)
                    continue;

                physicsPart.ScdPhysics?.Reset();
                if (((ushort)row.Function & 0x200) != 0)
                    physicsPart.ScdAnimation.Stop();
                else
                {
                    int endFrame = BitConverter.ToInt16(BitConverter.GetBytes(row.F_16), 0);
                    if (!physicsPart.ScdAnimation.Play(physicsPart.EanFile, (short)row.EanIndex, (short)row.StartFrame, endFrame, ((ushort)row.Function & 0x100) != 0))
                        Log.Add($"SCD animation {row.EanIndex} was not found for {physicsPart.physicsPart.GetEanPath()}.", LogType.Error);
                }
            }
        }

        public void AdvanceScdAnimations(float frameStep)
        {
            if (PhysicsParts != null)
                foreach (CharaPart physicsPart in PhysicsParts)
                    physicsPart?.ScdAnimation?.Advance(frameStep);
        }

        public void StopScdAnimations()
        {
            if (PhysicsParts == null)
                return;

            foreach (CharaPart physicsPart in PhysicsParts)
            {
                physicsPart?.ScdAnimation?.Stop();
                physicsPart?.ScdPhysics?.Reset();
            }
        }

        public void DrawScdDebugShapes()
        {
            ScdPhysics?.DrawDebugShapes();

            if (PhysicsParts == null)
                return;

            foreach (CharaPart physicsPart in PhysicsParts)
                physicsPart?.DrawScdDebugShapes();
        }

        public void ApplyFpfPreviewToPhysicsParts(FPF_File fpfFile, FpfPoseMatrix poseMatrices, FpfPoseMatrix skinOffsetMatrices, FpfSkinOffsetMode skinOffsetMode, HashSet<int> usedEntries)
        {
            if (IsPhysicsPart && Skeleton != null)
            {
                FPF_Entry entry = FindFpfPreviewEntry(fpfFile, Skeleton.Bones.Length, usedEntries);

                if (entry != null)
                    FpfPosePreview.ApplyEntryToScdSkeleton(Skeleton, entry, poseMatrices, skinOffsetMatrices, skinOffsetMode, SkeletoBoneIndices);
            }

            if (PhysicsParts == null)
                return;

            foreach (CharaPart physicsPart in PhysicsParts)
                physicsPart?.ApplyFpfPreviewToPhysicsParts(fpfFile, poseMatrices, skinOffsetMatrices, skinOffsetMode, usedEntries);
        }

        public int BakeFpfPreviewPhysicsPoses(FPF_File fpfFile, FpfPoseBakeMode bakeMode, HashSet<int> usedEntries)
        {
            int bakedCount = 0;

            if (IsPhysicsPart && Skeleton != null)
            {
                FPF_Entry entry = FindFpfPreviewEntry(fpfFile, Skeleton.Bones.Length, usedEntries);

                if (entry != null)
                    bakedCount += FpfPosePreview.BakeScdSkeletonPoseToEntry(Skeleton, entry, GetPhysicsPartAttachMatrix());
            }

            if (PhysicsParts == null)
                return bakedCount;

            foreach (CharaPart physicsPart in PhysicsParts)
                bakedCount += physicsPart?.BakeFpfPreviewPhysicsPoses(fpfFile, bakeMode, usedEntries) ?? 0;

            return bakedCount;
        }

        private Matrix4x4 GetPhysicsPartAttachMatrix()
        {
            int boneIndex = chara.Skeleton.GetBoneIndex(physicsPart?.BoneToAttach);
            return boneIndex >= 0 ? chara.Skeleton.Bones[boneIndex].AbsoluteAnimationMatrix : Matrix4x4.Identity;
        }

        private static FPF_Entry FindFpfPreviewEntry(FPF_File fpfFile, int boneCount, HashSet<int> usedEntries)
        {
            if (fpfFile?.Entries == null)
                return null;

            FPF_Entry entry = fpfFile.Entries
                .Where(fpfEntry => fpfEntry.ID != FPF_File.MainSkeletonEntryId)
                .Where(fpfEntry => fpfEntry.BonePoses?.Count == boneCount)
                .FirstOrDefault(fpfEntry => !usedEntries.Contains(fpfEntry.ID));

            if (entry == null)
            {
                entry = fpfFile.Entries
                    .Where(fpfEntry => fpfEntry.ID != FPF_File.MainSkeletonEntryId)
                    .FirstOrDefault(fpfEntry => fpfEntry.BonePoses?.Count == boneCount);
            }

            if (entry != null)
                usedEntries.Add(entry.ID);

            return entry;
        }

        /// <summary>
        /// Normal part draw function.
        /// </summary>
        public void Draw(PartTypeFlags hideFlags)
        {
            if (IsMaterialsDirty)
            {
                RefreshMaterials();
                IsMaterialsDirty = false;
            }

            //Check if part hiding is enabled for this Part Type
            if (hideFlags.HasFlag(partTypeFlag)) return;

            int texIdx = GetDytIndex(part.Texture);

            BindDyt(texIdx);

            //Set eye movement parameters in shaders
            if (partType == PartType.FaceEye)
            {
                foreach (int materialIndex in leftEyeMaterials)
                    Materials[materialIndex].SetEyeMovement(chara.EyeIrisLeft_UV);

                foreach (int materialIndex in rightEyeMaterials)
                    Materials[materialIndex].SetEyeMovement(chara.EyeIrisRight_UV);
            }

            if (Model != null)
            {
                //Draw Model
                SetBacMaterialOverrides(true);
                Model.Draw(chara.Transform, chara.ActorSlot, Materials, Textures, Dyts, texIdx, chara.Skeleton);
                SetBacMaterialOverrides(false);
            }

            if (!IsPhysicsPart)
            {
                foreach (var physicsPart in PhysicsParts)
                {
                    physicsPart.DrawPhysicsPart();
                }
            }
        }

        public void DrawPhysicsPart()
        {
            if (IsMaterialsDirty)
            {
                RefreshMaterials();
                IsMaterialsDirty = false;
            }

            int texIdx = GetDytIndex(physicsPart.Texture);

            BindDyt(texIdx);

            if (Model != null)
            {
                Model.Draw(chara.Transform, chara.ActorSlot, Materials, Textures, Dyts, texIdx, Skeleton);
            }
        }

        private int GetDytIndex(int partIdx)
        {
            if (chara.ForceDytOverride > -1) return chara.ForceDytOverride;
            if (chara.BacDytMain > -1) return chara.BacDytRatio >= 1f ? chara.BacDytBlend : chara.BacDytMain;
            return partIdx;
        }

        private Texture2D blendedDyt;
        private Color[] blendedDytPixels;
        private int blendedDytMain = -1;
        private int blendedDytBlend = -1;
        private float blendedDytRatio;
        private readonly Dictionary<Xv2Texture, Color[]> dytPixels = new Dictionary<Xv2Texture, Color[]>();

        private void BindDyt(int texIdx)
        {
            if (Dyts == null) return;

            GraphicsDevice.SamplerStates[4] = DytSampler.state;
            GraphicsDevice.VertexSamplerStates[4] = DytSampler.state;

            Texture2D texture = GetBlendedDyt() ?? (texIdx >= 0 && texIdx < Dyts.Length ? Dyts[texIdx].Texture : null);

            if (texture != null)
            {
                GraphicsDevice.Textures[4] = texture;
                GraphicsDevice.VertexTextures[4] = texture;
            }
        }

        /// <summary>
        /// The main and blend DYTs mixed by the BAC DYT Blend ratio, or null when no mix is needed.
        /// </summary>
        private Texture2D GetBlendedDyt()
        {
            int main = chara.BacDytMain;
            int blend = chara.BacDytBlend;
            float ratio = chara.BacDytRatio;

            if (chara.ForceDytOverride > -1 || main < 0 || blend < 0 || ratio <= 0f || ratio >= 1f || main >= Dyts.Length || blend >= Dyts.Length)
                return null;

            if (blendedDyt != null && main == blendedDytMain && blend == blendedDytBlend && ratio == blendedDytRatio)
                return blendedDyt;

            Color[] mainPixels = GetDytPixels(Dyts[main]);
            Color[] blendPixels = GetDytPixels(Dyts[blend]);

            if (mainPixels == null || blendPixels == null || mainPixels.Length != blendPixels.Length)
                return null;

            int width = Dyts[main].EmbEntry.Texture.PixelWidth;
            int height = Dyts[main].EmbEntry.Texture.PixelHeight;

            if (blendedDyt == null || blendedDyt.Width != width || blendedDyt.Height != height)
            {
                blendedDyt?.Dispose();
                blendedDyt = new Texture2D(GraphicsDevice, width, height);
                blendedDytPixels = new Color[width * height];
            }

            for (int i = 0; i < blendedDytPixels.Length; i++)
                blendedDytPixels[i] = Color.Lerp(mainPixels[i], blendPixels[i], ratio);

            blendedDyt.SetData(blendedDytPixels);
            blendedDytMain = main;
            blendedDytBlend = blend;
            blendedDytRatio = ratio;
            return blendedDyt;
        }

        private Color[] GetDytPixels(Xv2Texture dyt)
        {
            if (dytPixels.TryGetValue(dyt, out Color[] pixels))
                return pixels;

            System.Windows.Media.Imaging.WriteableBitmap bitmap = dyt.EmbEntry?.Texture;

            if (bitmap != null && bitmap.Format.BitsPerPixel == 32)
            {
                int[] bgra = new int[bitmap.PixelWidth * bitmap.PixelHeight];
                bitmap.CopyPixels(bgra, bitmap.PixelWidth * 4, 0);
                pixels = new Color[bgra.Length];

                for (int i = 0; i < bgra.Length; i++)
                    pixels[i] = new Color((bgra[i] >> 16) & 0xFF, (bgra[i] >> 8) & 0xFF, bgra[i] & 0xFF, (bgra[i] >> 24) & 0xFF);
            }
            else
            {
                Log.Add("DYT Blend: a DYT is not a 32 bit texture, so the preview switches between the DYTs without mixing them.", LogType.Warning);
            }

            dytPixels[dyt] = pixels;
            return pixels;
        }

        //Materials can be shared between actors, so the override is only set while this part draws.
        private void SetBacMaterialOverrides(bool set)
        {
            if (chara.BacMatScale0.Count == 0 || Materials == null) return;

            for (int i = 0; i < Materials.Length; i++)
            {
                if (chara.BacMatScale0.TryGetValue(((int)partType, i), out float[] matScale0))
                    Materials[i].MatScale0Override = set ? matScale0 : null;
            }
        }

        public void DrawSimple(bool normalPass, PartTypeFlags hideFlags)
        {
            //Check if part hiding is enabled for this Part Type
            if (hideFlags.HasFlag(partTypeFlag)) return;

            if (Model != null)
            {
                //Draw Model
                Model.Draw(chara.Transform, chara.ActorSlot, normalPass ? RenderSystem.NORMAL_FADE_WATERDEPTH_W_M : RenderSystem.ShadowModel_W, chara.Skeleton);
            }

            if (!IsPhysicsPart)
            {
                foreach (CharaPart physicsPart in PhysicsParts)
                {
                    physicsPart.DrawSimplePhysicsPart(normalPass);
                }
            }
        }

        public void DrawSimplePhysicsPart(bool normalPass)
        {
            if (Model != null)
            {
                Model.Draw(chara.Transform, chara.ActorSlot, normalPass ? RenderSystem.NORMAL_FADE_WATERDEPTH_W_M : RenderSystem.ShadowModel_W, Skeleton);
            }
        }


        #endregion

        private bool IsCustomColorMaterial(string materialName, string colorMaterialPrefix)
        {
            if (materialName.Length <= colorMaterialPrefix.Length) return false;

            for (int i = 0; i < colorMaterialPrefix.Length; i++)
            {
                if (colorMaterialPrefix[i] != materialName[i]) return false;
            }

            return true;
        }

        public PartTypeFlags GetHideFlags()
        {
            PartTypeFlags flags = 0;

            if (part != null)
            {
                flags |= part.HideFlags;

                if (PhysicsParts != null)
                {
                    for (int i = 0; i < PhysicsParts.Length; i++)
                    {
                        if (PhysicsParts[i] != null)
                        {
                            flags |= PhysicsParts[i].physicsPart.HideFlags;
                        }
                    }
                }
            }

            return flags;
        }

        public PartTypeFlags GetHideMatFlags()
        {
            PartTypeFlags flags = 0;

            if (part != null)
            {
                flags |= part.HideMatFlags;

                if (PhysicsParts != null)
                {
                    for (int i = 0; i < PhysicsParts.Length; i++)
                    {
                        if (PhysicsParts[i] != null)
                        {
                            flags |= PhysicsParts[i].physicsPart.HideMatFlags;
                        }
                    }
                }
            }

            return flags;
        }

        public PartTypeFlags GetPartTypeAsFlag()
        {
            return GetPartTypeAsFlag(partType);
        }

        public static PartTypeFlags GetPartTypeAsFlag(PartType type)
        {
            switch (type)
            {
                case PartType.Boots:
                    return PartTypeFlags.Boots;
                case PartType.Bust:
                    return PartTypeFlags.Bust;
                case PartType.FaceBase:
                    return PartTypeFlags.FaceBase;
                case PartType.FaceEar:
                    return PartTypeFlags.FaceEar;
                case PartType.FaceEye:
                    return PartTypeFlags.FaceEye;
                case PartType.FaceForehead:
                    return PartTypeFlags.FaceForehead;
                case PartType.FaceNose:
                    return PartTypeFlags.FaceNose;
                case PartType.Hair:
                    return PartTypeFlags.Hair;
                case PartType.Pants:
                    return PartTypeFlags.Pants;
                case PartType.Rist:
                    return PartTypeFlags.Rist;
            }

            return 0;
        }

    }

}
