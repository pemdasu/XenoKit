using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Model;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib;
using Xv2CoreLib.BCS;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMM;
using Xv2CoreLib.ESK;

namespace XenoKit.Engine
{
    public partial class CharaPart : EngineObject
    {
        private Actor chara;
        private PartSet parentPartSet;
        public Part part;
        private PhysicsPart physicsPart;
        public PartType partType;
        private PartTypeFlags partTypeFlag;

        //Physics Object stuff:
        private bool IsPhysicsPart;
        private CharaPart Parent;
        private CharaPart[] PhysicsParts;

        //Compiled Objects:
        public Xv2ModelFile Model { get; private set; }
        public Xv2ShaderEffect[] Materials { get; private set; }
        public Xv2Texture[] Textures { get; private set; }
        public Xv2Texture[] Dyts { get; private set; }
        public Xv2Skeleton Skeleton { get; private set; }
        private int[] SkeletoBoneIndices;
        private ScdPartPhysics ScdPhysics;
        private ScdAnimationPlayer ScdAnimation;

        //Source Files:
        private bool nullPartSet = false;
        public EMD_File EmdFile { get; private set; }
        private EMM_File EmmFile = null;
        private EMB_TextureFile EmbFile = null;
        private EMB_TextureFile DytFile = null;
        private EAN_File EanFile = null;
        private ESK_File EskFile = null;

        //Dyt Sampler:
        private SamplerInfo DytSampler;

        private readonly List<int> leftEyeMaterials = new List<int>();
        private readonly List<int> rightEyeMaterials = new List<int>();

        //Auto-updating
        private bool IsMaterialsDirty = false;
        private bool IsTexturesDirty = false;
        private bool IsDytDirty = false;

        public CharaPart(Actor chara, Part part, PartType type)
        {
            this.chara = chara;
            this.part = part;
            partType = type;
            partTypeFlag = GetPartTypeAsFlag();
            parentPartSet = chara.CharacterData.BcsFile.File.GetParentPartSet(part);

            DytSampler = new SamplerInfo()
            {
                type = SamplerType.Sampler2D,
                textureSlot = 4,
                samplerSlot = 4,
                name = ShaderManager.GetSamplerName(4),
                state = new SamplerState()
                {
                    AddressU = TextureAddressMode.Clamp,
                    AddressV = TextureAddressMode.Clamp,
                    AddressW = TextureAddressMode.Wrap,
                    BorderColor = new Microsoft.Xna.Framework.Color(1, 1, 1, 1),
                    Filter = TextureFilter.LinearMipPoint,
                    MaxAnisotropy = 1,
                    MaxMipLevel = 1,
                    MipMapLevelOfDetailBias = 0,
                    Name = ShaderManager.GetSamplerName(4)
                }
            };

            LoadPart();
            LoadChildPhysicsParts();
        }

        public CharaPart(Actor chara, PhysicsPart part, Part parentPart, CharaPart parent, PartType type)
        {
            this.chara = chara;
            this.part = parentPart;
            parentPartSet = chara.CharacterData.BcsFile.File.GetParentPartSet(part);
            physicsPart = part;
            partType = type;
            partTypeFlag = GetPartTypeAsFlag();
            Parent = parent;
            IsPhysicsPart = true;

            DytSampler = new SamplerInfo()
            {
                type = SamplerType.Sampler2D,
                textureSlot = 4,
                samplerSlot = 4,
                name = ShaderManager.GetSamplerName(4),
                state = new SamplerState()
                {
                    AddressU = TextureAddressMode.Clamp,
                    AddressV = TextureAddressMode.Clamp,
                    AddressW = TextureAddressMode.Wrap,
                    BorderColor = new Microsoft.Xna.Framework.Color(1, 1, 1, 1),
                    Filter = TextureFilter.LinearMipPoint,
                    MaxAnisotropy = 1,
                    MaxMipLevel = 1,
                    MipMapLevelOfDetailBias = 0,
                    Name = ShaderManager.GetSamplerName(4)
                }
            };

            LoadPart();
        }

        #region Load
        public void LoadPart()
        {
            LoadSources();

            if (!nullPartSet)
            {
                if (IsPhysicsPart)
                {
                    LoadSkeleton();
                }

                LoadModel(false);
                LoadMaterials();
                LoadTextures();
                LoadDyts();
            }
        }

        public void LoadModel(bool reloadMaterials = true)
        {
            if (EmdFile == null)
            {
                Log.Add($"CharaPart.LoadModel: No emd file has been loaded.", LogType.Error);
                return;
            }

            //Unsubscribe from ModelChanged event if a model already exists on this Part
            if (Model != null)
                Model.MaterialsChanged -= RefreshMaterialsOnEdit;

            Model = CompiledObjectManager.GetCompiledObject<Xv2ModelFile>(EmdFile);

            if (reloadMaterials)
                LoadMaterials();
        }

        public void LoadMaterials()
        {
            if (EmdFile == null)
            {
                Log.Add($"CharaPart.LoadMaterials: No emd file has been loaded. This must be done before materials can be initialized!", LogType.Error);
                return;
            }

            if (EmmFile == null)
            {
                Log.Add($"CharaPart.LoadMaterials: No emm file has been loaded.", LogType.Error);
                return;
            }

            if (Model != null)
            {
                Materials = Xv2ShaderEffect.LoadMaterials(EmmFile, ShaderType.Chara);
                Model.InitMaterialIndex(Materials);
                //Materials = Model.InitializeMaterials(ShaderType.Chara, EmmFile);

                leftEyeMaterials.Clear();
                rightEyeMaterials.Clear();

                if (partType == PartType.FaceEye)
                {
                    for (int materialIndex = 0; materialIndex < Materials.Length; materialIndex++)
                    {
                        switch (Materials[materialIndex].Material.Name.ToLowerInvariant())
                        {
                            case "eye_l":
                            case "eye_l_sub":
                            case "eye_l_ss":
                            case "eye_l_ss3":
                                leftEyeMaterials.Add(materialIndex);
                                break;
                            case "eye_r":
                            case "eye_r_sub":
                            case "eye_r_ss":
                            case "eye_r_ss3":
                                rightEyeMaterials.Add(materialIndex);
                                break;
                        }
                    }
                }

                Model.MaterialsChanged += RefreshMaterialsOnEdit;
            }
        }

        public void LoadTextures()
        {
            if (EmbFile == null)
            {
                Log.Add($"CharaPart.LoadTextures: No emb file has been loaded.", LogType.Error);
                return;
            }

            Textures = Xv2Texture.LoadTextureArray(EmbFile);
        }

        public void LoadDyts()
        {
            if (DytFile == null)
            {
                Log.Add($"CharaPart.LoadDyts: No dyt.emb file has been loaded.", LogType.Error);
                return;
            }

            Dyts = Xv2Texture.LoadTextureArray(DytFile);
        }

        public void LoadSkeleton()
        {
            if (EskFile == null)
            {
                Log.Add($"CharaPart.LoadSkeleton: No esk file has been loaded.", LogType.Error);
                return;
            }

            Skeleton = CompiledObjectManager.GetCompiledObject<Xv2Skeleton>(EskFile);

            if(Skeleton != null)
            {
                ScdPhysics?.Dispose();
                ScdAnimation?.Dispose();
                SkeletoBoneIndices = Skeleton.ScdGetBoneIndices(chara.Skeleton, physicsPart.BoneToAttach);
                ScdPhysics = LoadScdPhysics();
                ScdAnimation = EanFile != null ? new ScdAnimationPlayer(Skeleton, SkeletoBoneIndices) : null;
            }
        }

        private ScdPartPhysics LoadScdPhysics()
        {
            string path = GetFilePath(physicsPart.GetScdPath());

            if (path == null)
                return null;

            Xv2CoreLib.SCD.SCD_File scdFile;

            if (chara.ShortName == physicsPart.CharaCode && Utils.CompareCharaFolder(path, chara.ShortName))
            {
                Xv2CoreLib.Xv2PartSetFile file = chara.CharacterData.PartSetFiles.FirstOrDefault(x => x.Name.Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase));
                file?.Load(false);
                scdFile = file?.File as Xv2CoreLib.SCD.SCD_File;
            }
            else
            {
                byte[] bytes = FileManager.Instance.GetBytesFromGame(path, chara.CharacterData.OnlyLoadFromCPK);
                scdFile = bytes == null ? null : Xv2CoreLib.SCD.SCD_File.Load(bytes);
            }

            if (scdFile == null)
            {
                Log.Add($"SCD \"{path}\" could not be loaded. The physics part will follow the character skeleton without physics.", LogType.Error);
                return null;
            }

            try
            {
                ScdPartPhysics scdPhysics = new ScdPartPhysics(scdFile, Skeleton, SkeletoBoneIndices, chara.Skeleton, chara.CharacterData.BcsFile?.File?.SkeletonData1, chara.Transform, physicsPart.BoneToAttach, path);

                foreach (string warning in scdPhysics.Warnings)
                    Log.Add($"{path}: {warning}", LogType.Warning);

                return scdPhysics;
            }
            catch (Exception ex)
            {
                Log.Add($"SCD \"{path}\" could not be simulated: {ex.Message}. The physics part will follow the character skeleton without physics.", LogType.Error);
                return null;
            }
        }

        public void LoadCustomColors(List<CustomColorGroup> colors)
        {
            foreach(CustomColorGroup color in colors)
            {
                if(color.Color != -1 && color.Group != -1)
                {
                    Xv2CoreLib.BCS.PartColor colorGroup = chara.CharacterData.BcsFile.File.PartColors?.FirstOrDefault(x => x.ID == color.Group);
                    if (colorGroup == null) continue;

                    if (Model != null)
                    {
                        foreach (Xv2Model model in Model.Models)
                        {
                            foreach (Xv2Mesh mesh in model.Meshes)
                            {
                                foreach (Xv2Submesh submesh in mesh.Submeshes)
                                {
                                    if (IsCustomColorMaterial(submesh.Name, colorGroup.Name))
                                    {
                                        submesh.ApplyCustomColor(color.Group, color.Color);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //We have to load colors from ALL the parts, because they draw the colors from all the parts for whatever reason. No idea on the reasoning for this
            //i am now enlightened
            /*
            foreach (var _part in parentPartSet.Parts)
            {
                foreach (var colSel in _part.ColorSelectors)
                {
                    var colorGroup = chara.CharacterData.BcsFile.File.PartColors?.FirstOrDefault(x => x.ID == colSel.PartColorGroup);
                    if (colorGroup == null) continue;

                    if (Model != null)
                    {
                        foreach (var model in Model.Models)
                        {
                            foreach (var mesh in model.Meshes)
                            {
                                foreach (var submesh in mesh.Submeshes)
                                {
                                    if (IsCustomColorMaterial(submesh.Name, colorGroup.Name))
                                    {
                                        submesh.ApplyCustomColor(colSel.PartColorGroup, colSel.ColorIndex);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            */
        }

        public void ResetCustomColors()
        {
            if (Model == null) return;

            foreach (Xv2Model model in Model.Models)
            {
                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        submesh.ResetCustomColor();
                    }
                }
            }
        }

        private void LoadChildPhysicsParts()
        {
            int physicsPartCount = part.PhysicsParts != null ? part.PhysicsParts.Count : 0;

            DisposeScdPhysics();
            PhysicsParts = new CharaPart[physicsPartCount];

            for (int i = 0; i < physicsPartCount; i++)
            {
                PhysicsParts[i] = new CharaPart(chara, part.PhysicsParts[i], part, this, partType);
            }
        }

        private void RefreshMaterialsOnEdit(object sender, EventArgs e)
        {
            IsMaterialsDirty = true;
        }

        private void EmbFile_TexturesChanged(object sender, EventArgs e)
        {
            IsTexturesDirty = true;
        }

        private void DytFile_TexturesChanged(object sender, EventArgs e)
        {
            IsDytDirty = true;
        }

        private void RefreshMaterials()
        {
            if (Model != null && EmmFile != null)
            {
                LoadMaterials();
            }
        }
        #endregion

    }
}
