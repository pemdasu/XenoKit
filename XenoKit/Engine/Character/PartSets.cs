using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using XenoKit.Editor;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Textures;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BCS;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EMD;
using Xv2CoreLib.FPF;

namespace XenoKit.Engine
{
    public partial class CharaPartSet : EngineObject
    {
        //PartSet (base)
        public int ID;
        private PartSet partSet = null;

        private Actor chara;

        public EAN_File FceEan = null;
        public EAN_File FceForeheadEan = null;

        public CharaPart[] Parts = new CharaPart[10];
        public CharaPart[] TransformedParts = new CharaPart[10];

        //BAC PartSet Function:
        public CharaPart[] BacTransformedParts = new CharaPart[10];
        public bool[] BacPermanentParts = new bool[10];

        public PartTypeFlags HideFlags = 0;
        public PartTypeFlags HideMatFlags = 0;

        //Custom Colors
        public List<CustomColorGroup> BcsColors = new List<CustomColorGroup>();

        public CharaPartSet(Actor chara, int partSetId)
        {
            ID = partSetId;
            this.chara = chara;
            LoadPartSet();
        }

        #region LoadBase
        public void LoadPartSet()
        {
            partSet = chara.CharacterData.BcsFile.File.PartSets.FirstOrDefault(x => x.ID == ID);
            if (partSet == null)
            {
                Log.Add($"CharaPartSet.LoadPartSet: CharaPartSet is no longer valid, cant load.", LogType.Error);
                return;
            }

            ResetParts();

            //Load parts. Null parts will be ignored.
            LoadPart(0, false, false);
            LoadPart(1, false, false);
            LoadPart(2, false, false);
            LoadPart(3, false, false);
            LoadPart(4, false, false);
            LoadPart(5, false, false);
            LoadPart(6, false, false);
            LoadPart(7, false, false);
            LoadPart(8, false, false);
            LoadPart(9, false, false);

            //Apply BCS colors after all parts have been loaded
            SetBcsColors(partSet);
            ApplyCustomColors();

            //Load flags
            LoadHideFlags();
            LoadHideMatFlags();

            //Load fce.ean
            GetFceEan();

            //Create hitbox for character
            GetAABB();
            
        }

        public void LoadPart(int index, bool fetchPartSet = true, bool loadFlags = true)
        {
            if (fetchPartSet)
                partSet = chara.CharacterData.BcsFile.File.PartSets.FirstOrDefault(x => x.ID == ID);

            if (partSet == null)
            {
                Log.Add($"CharaPartSet.LoadPart: CharaPartSet is no longer valid, cant load.", LogType.Error);
                return;
            }

            Part part = partSet.GetPart(index);
            Parts[index]?.DisposeScdPhysics();

            //Part was null. 
            if (part == null)
            {
                Parts[index] = null;
                return;
            }

            Parts[index] = new CharaPart(chara, part, (PartType)index);

            if (loadFlags)
            {
                LoadHideFlags();
                LoadHideMatFlags();
            }

            //Reload fce.ean
            GetFceEan();
        }

        private void ResetParts()
        {
            for (int i = 0; i < Parts.Length; i++)
            {
                Parts[i]?.DisposeScdPhysics();
                Parts[i] = null;
            }

            //Reload fce.ean
            GetFceEan();
        }

        public void LoadHideFlags()
        {
            PartTypeFlags flags = 0;

            for (int i = 0; i < Parts.Length; i++)
            {
                if (Parts[i] != null)
                {
                    flags |= Parts[i].GetHideFlags();
                }
            }

            for (int i = 0; i < TransformedParts.Length; i++)
            {
                if (Parts[i] != null)
                {
                    flags |= Parts[i].GetHideFlags();
                }
            }

            for (int i = 0; i < BacTransformedParts.Length; i++)
            {
                if (BacTransformedParts[i] != null)
                {
                    flags |= BacTransformedParts[i].GetHideFlags();
                }
            }

            HideFlags = flags;
        }

        public void LoadHideMatFlags()
        {
            PartTypeFlags flags = 0;

            for (int i = 0; i < Parts.Length; i++)
            {
                if (Parts[i] != null)
                {
                    flags |= Parts[i].GetHideMatFlags();
                }
            }

            for (int i = 0; i < TransformedParts.Length; i++)
            {
                if (Parts[i] != null)
                {
                    flags |= Parts[i].GetHideMatFlags();
                }
            }

            for (int i = 0; i < BacTransformedParts.Length; i++)
            {
                if (BacTransformedParts[i] != null)
                {
                    flags |= BacTransformedParts[i].GetHideMatFlags();
                }
            }

            HideMatFlags = flags;
        }

        private void GetFceEan()
        {
            FceForeheadEan = null;
            FceEan = null;

            //Look on base parts first
            for (int i = 0; i < Parts.Length; i++)
            {
                if (Parts[i] != null)
                {
                    if (Parts[i].GetEan() != null)
                    {
                        if (Parts[i].partType == PartType.FaceForehead)
                        {
                            FceForeheadEan = Parts[i].GetEan();
                        }
                        else if (Parts[i].partType == PartType.FaceBase)
                        {
                            FceEan = Parts[i].GetEan();
                        }
                    }
                }
            }

            //Transformed parts have priority, so they come last
            for (int i = 0; i < TransformedParts.Length; i++)
            {
                if (TransformedParts[i] != null)
                {
                    if (TransformedParts[i].GetEan() != null)
                    {
                        if (TransformedParts[i].partType == PartType.FaceForehead)
                        {
                            FceForeheadEan = TransformedParts[i].GetEan();
                        }
                        else if (TransformedParts[i].partType == PartType.FaceBase)
                        {
                            FceEan = TransformedParts[i].GetEan();
                        }
                    }
                }
            }

            //Transformed parts have priority, so they come last
            for (int i = 0; i < BacTransformedParts.Length; i++)
            {
                if (BacTransformedParts[i] != null)
                {
                    if (BacTransformedParts[i].GetEan() != null)
                    {
                        if (BacTransformedParts[i].partType == PartType.FaceForehead)
                        {
                            FceForeheadEan = BacTransformedParts[i].GetEan();
                        }
                        else if (BacTransformedParts[i].partType == PartType.FaceBase)
                        {
                            FceEan = BacTransformedParts[i].GetEan();
                        }
                    }
                }
            }
        }

        public void ApplyFpfPreviewToPhysicsParts(FPF_File fpfFile, FpfPoseMatrix poseMatrices, FpfPoseMatrix skinOffsetMatrices, FpfSkinOffsetMode skinOffsetMode)
        {
            HashSet<int> usedEntries = new HashSet<int>();

            ApplyFpfPreviewToPhysicsParts(Parts, fpfFile, poseMatrices, skinOffsetMatrices, skinOffsetMode, usedEntries);
            ApplyFpfPreviewToPhysicsParts(TransformedParts, fpfFile, poseMatrices, skinOffsetMatrices, skinOffsetMode, usedEntries);
            ApplyFpfPreviewToPhysicsParts(BacTransformedParts, fpfFile, poseMatrices, skinOffsetMatrices, skinOffsetMode, usedEntries);
        }

        private static void ApplyFpfPreviewToPhysicsParts(CharaPart[] parts, FPF_File fpfFile, FpfPoseMatrix poseMatrices, FpfPoseMatrix skinOffsetMatrices, FpfSkinOffsetMode skinOffsetMode, HashSet<int> usedEntries)
        {
            if (parts == null)
                return;

            foreach (CharaPart part in parts)
                part?.ApplyFpfPreviewToPhysicsParts(fpfFile, poseMatrices, skinOffsetMatrices, skinOffsetMode, usedEntries);
        }

        public int BakeFpfPreviewPhysicsPoses(FPF_File fpfFile, FpfPoseBakeMode bakeMode)
        {
            HashSet<int> usedEntries = new HashSet<int>();
            int bakedCount = 0;

            bakedCount += BakeFpfPreviewPhysicsPoses(Parts, fpfFile, bakeMode, usedEntries);
            bakedCount += BakeFpfPreviewPhysicsPoses(TransformedParts, fpfFile, bakeMode, usedEntries);
            bakedCount += BakeFpfPreviewPhysicsPoses(BacTransformedParts, fpfFile, bakeMode, usedEntries);

            return bakedCount;
        }

        private static int BakeFpfPreviewPhysicsPoses(CharaPart[] parts, FPF_File fpfFile, FpfPoseBakeMode bakeMode, HashSet<int> usedEntries)
        {
            if (parts == null)
                return 0;

            int bakedCount = 0;

            foreach (CharaPart part in parts)
                bakedCount += part?.BakeFpfPreviewPhysicsPoses(fpfFile, bakeMode, usedEntries) ?? 0;

            return bakedCount;
        }
        #endregion

        #region LoadTransformation
        public void ApplyTransformation(int index, PartTypeFlags partFlags = PartTypeFlags.AllParts, bool permanent = false)
        {
            if (index == -1) return;

            PartSet partSetTrans = chara.CharacterData.BcsFile.File.PartSets.FirstOrDefault(x => x.ID == index);
            if (partSetTrans == null)
            {
                Log.Add($"CharaPartSet.ApplyTransformation: PartSet {index} not found.", LogType.Warning);
                return;
            }

            //Load parts
            for (int i = 0; i < 10; i++)
            {
                if (partFlags.HasFlag(CharaPart.GetPartTypeAsFlag((PartType)i)))
                {
                    ApplyTransformedPart(i, partSetTrans, permanent);
                }
            }

            //Apply BCS colors after all parts have been loaded
            ApplyCustomColors();

            //Reload fce.ean
            GetFceEan();
        }

        private void ApplyTransformedPart(int index, PartSet partSet, bool permanent)
        {
            Part part = partSet.GetPart(index);

            if (part != null)
            {
                SetBcsColors(part);

                TransformedParts[index]?.DisposeScdPhysics();

                if (permanent)
                {
                    Parts[index]?.DisposeScdPhysics();
                    Parts[index] = new CharaPart(chara, part, (PartType)index);
                    TransformedParts[index] = null;
                }
                else
                {
                    TransformedParts[index] = new CharaPart(chara, part, (PartType)index);
                }
            }
        }

        public void ResetTransformation()
        {
            for (int i = 0; i < TransformedParts.Length; i++)
            {
                TransformedParts[i]?.DisposeScdPhysics();
                TransformedParts[i] = null;
            }

            //Reload fce.ean
            GetFceEan();
        }
        #endregion

        #region BacPartSetSwap
        public void ApplyBacPartSetSwap(int index, bool permanent)
        {
            //A negative index restores the actor's own part set.
            if (index < 0)
            {
                ResetBacPartSetSwap(true);
                return;
            }

            var partSetTrans = chara.CharacterData.BcsFile.File.PartSets.FirstOrDefault(x => x.ID == index);
            if (partSetTrans == null)
            {
                Log.Add($"CharaPartSet.ApplyBacTransformation: PartSet {index} not found.", LogType.Warning);
                return;
            }

            //Load parts
            ApplyBacPartSetSwap(0, partSetTrans, permanent);
            ApplyBacPartSetSwap(1, partSetTrans, permanent);
            ApplyBacPartSetSwap(2, partSetTrans, permanent);
            ApplyBacPartSetSwap(3, partSetTrans, permanent);
            ApplyBacPartSetSwap(4, partSetTrans, permanent);
            ApplyBacPartSetSwap(5, partSetTrans, permanent);
            ApplyBacPartSetSwap(6, partSetTrans, permanent);
            ApplyBacPartSetSwap(7, partSetTrans, permanent);
            ApplyBacPartSetSwap(8, partSetTrans, permanent);
            ApplyBacPartSetSwap(9, partSetTrans, permanent);

            //Reload fce.ean
            GetFceEan();
        }

        private void ApplyBacPartSetSwap(int index, PartSet partSet, bool permanent)
        {
            Part part = partSet.GetPart(index);

            if (part != null)
            {
                BacTransformedParts[index]?.DisposeScdPhysics();
                BacTransformedParts[index] = new CharaPart(chara, part, (PartType)index);
                BacPermanentParts[index] = permanent;
            }
        }

        public void ResetBacPartSetSwap(bool revertPermanentSwaps)
        {
            //Always set this to true currently. 
            revertPermanentSwaps = true;

            for (int i = 0; i < BacTransformedParts.Length; i++)
            {
                if (BacPermanentParts[i] == false || revertPermanentSwaps)
                {
                    BacTransformedParts[i]?.DisposeScdPhysics();
                    BacTransformedParts[i] = null;
                    BacPermanentParts[i] = false;
                }
            }
        }
        #endregion

        #region Rendering
        public override void Update()
        {
            for (int i = 0; i < Parts.Length; i++)
            {
                if (BacTransformedParts[i] != null)
                    BacTransformedParts[i].Update();
                else if (TransformedParts[i] != null)
                    TransformedParts[i].Update();
                else if (Parts[i] != null)
                    Parts[i].Update();
            }
        }

        public override void Draw()
        {
            for (int i = 0; i < Parts.Length; i++)
            {
                //Draw Priority:
                //-TransformedPart
                //-Else, Part if no TransformedPart

                if (BacTransformedParts[i] != null)
                    BacTransformedParts[i].Draw(HideFlags);
                else if (TransformedParts[i] != null)
                    TransformedParts[i].Draw(HideFlags);
                else if (Parts[i] != null)
                    Parts[i].Draw(HideFlags);
            }
        }

        public void DrawSimple(bool normalPass)
        {
            for (int i = 0; i < Parts.Length; i++)
            {
                if (BacTransformedParts[i] != null)
                    BacTransformedParts[i].DrawSimple(normalPass, HideFlags);
                else if (TransformedParts[i] != null)
                    TransformedParts[i].DrawSimple(normalPass, HideFlags);
                else if (Parts[i] != null)
                    Parts[i].DrawSimple(normalPass, HideFlags);
            }
        }

        public void DrawScdDebugShapes()
        {
            for (int i = 0; i < Parts.Length; i++)
            {
                if (BacTransformedParts[i] != null)
                    BacTransformedParts[i].DrawScdDebugShapes();
                else if (TransformedParts[i] != null)
                    TransformedParts[i].DrawScdDebugShapes();
                else if (Parts[i] != null)
                    Parts[i].DrawScdDebugShapes();
            }
        }

        public void ReloadScdPhysics()
        {
            foreach (CharaPart part in Parts.Concat(TransformedParts).Concat(BacTransformedParts).Where(part => part != null).Distinct())
                part.ReloadScdPhysics();
        }

        public void ControlScdAnimation(BAC_Type18 row)
        {
            for (int partIndex = 0; partIndex < Parts.Length; partIndex++)
            {
                if (row.PartMask == 0 || (row.PartMask & (1 << partIndex)) != 0)
                    (BacTransformedParts[partIndex] ?? TransformedParts[partIndex] ?? Parts[partIndex])?.ControlScdAnimation(row);
            }
        }

        public void AdvanceScdAnimations(float frameStep)
        {
            for (int partIndex = 0; partIndex < Parts.Length; partIndex++)
                (BacTransformedParts[partIndex] ?? TransformedParts[partIndex] ?? Parts[partIndex])?.AdvanceScdAnimations(frameStep);
        }

        public void StopScdAnimations()
        {
            foreach (CharaPart part in Parts.Concat(TransformedParts).Concat(BacTransformedParts).Where(part => part != null).Distinct())
                part.StopScdAnimations();
        }
        #endregion



        public bool IsPartSet(PartSet partSet)
        {
            return this.partSet == partSet;
        }

        public bool IsPartSet(Part part)
        {
            return partSet?.Parts?.Contains(part) == true;
        }

        public Vector2 GetEyeMovementScale()
        {
            CharaPart eye = BacTransformedParts[(int)PartType.FaceEye] ?? TransformedParts[(int)PartType.FaceEye] ?? Parts[(int)PartType.FaceEye];
            return eye != null ? new Vector2(eye.part.F_36, eye.part.F_40) : Vector2.Zero;
        }

        public void GetAABB()
        {
            for(int i = 0; i < Parts.Length; i++)
            {
                if (Parts[i]?.EmdFile != null)
                {
                    foreach(EMD_Model model in Parts[i].EmdFile.Models)
                    {
                        foreach(EMD_Mesh mesh in model.Meshes)
                        {
                            if(mesh.AABB.MinX < chara.AABB[0])
                                chara.AABB[0] = mesh.AABB.MinX;

                            if (mesh.AABB.MinY < chara.AABB[1])
                                chara.AABB[1] = mesh.AABB.MinY;

                            if (mesh.AABB.MinZ < chara.AABB[2])
                                chara.AABB[2] = mesh.AABB.MinZ;

                            if (mesh.AABB.MaxX > chara.AABB[3])
                                chara.AABB[3] = mesh.AABB.MaxX;

                            if (mesh.AABB.MaxY > chara.AABB[4])
                                chara.AABB[4] = mesh.AABB.MaxY;

                            if (mesh.AABB.MaxZ > chara.AABB[5])
                                chara.AABB[5] = mesh.AABB.MaxZ;
                        }
                    }
                }
            }
        }
    }

}
