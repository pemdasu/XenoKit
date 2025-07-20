using System;
using System.Linq;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Vertex;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using XenoKit.Engine.Objects;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMO;
using Xv2CoreLib.EMD;
using Xv2CoreLib.NSK;
using Xv2CoreLib;
using Xv2CoreLib.Resource;
using EmmMaterial = Xv2CoreLib.EMM.EmmMaterial;
using static Xv2CoreLib.EMD.EMD_TextureSamplerDef;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;
using SimdQuaternion = System.Numerics.Quaternion;

namespace XenoKit.Engine.Model
{
    public class Xv2ModelFile : RenderObject
    {
        public EventHandler MaterialsChanged;
        public NSK_File SourceNskFile { get; private set; }
        public EMD_File SourceEmdFile { get; private set; }
        public EMO_File SourceEmoFile { get; private set; }
        private bool IsReflectionMesh = false;

        public ModelType Type { get; private set; }
        public List<Xv2Model> Models { get; private set; } = new List<Xv2Model>();
        public Xv2Skeleton Skeleton { get; private set; }

        private Xv2ModelFile()
        {
        }

        #region Load
        public static Xv2ModelFile LoadEmd(EMD_File emdFile, string name = null)
        {
            //Create EmdModel
            Xv2ModelFile newEmdFile = new Xv2ModelFile();
            newEmdFile.Name = name;
            newEmdFile.Type = ModelType.Emd;
            newEmdFile.SourceEmdFile = emdFile;
            newEmdFile.LoadEmd(true);

            return newEmdFile;
        }

        public static Xv2ModelFile LoadNsk(NSK_File nskFile, string name = null)
        {
            //Create EmdModel
            Xv2ModelFile modelFile = new Xv2ModelFile();
            modelFile.Name = name;
            modelFile.Type = ModelType.Nsk;
            modelFile.SourceNskFile = nskFile;
            modelFile.SourceEmdFile = nskFile.EmdFile;
            modelFile.LoadEmd(true);
            modelFile.LoadInternalSkeleton();

            return modelFile;
        }

        public static Xv2ModelFile LoadEmo(EMO_File emoFile)
        {
            Xv2ModelFile modelFile = new Xv2ModelFile();
            modelFile.Type = ModelType.Emo;
            modelFile.SourceEmoFile = emoFile;
            modelFile.LoadEmo(true);
            modelFile.LoadInternalSkeleton();

            return modelFile;
        }

        /// <summary>
        /// Load the first submesh from an EMG, and ignore samplers and blend weights. This is intended for loading particle EMGs.
        /// </summary>
        public static Xv2Submesh LoadEmg(EMG_File emgFile)
        {
            if (emgFile.EmgMeshes.Count == 0) return null;
            if (emgFile.EmgMeshes[0].Submeshes.Count == 0) return null;

            EMG_Mesh mesh = emgFile.EmgMeshes[0];
            EMG_Submesh submesh = emgFile.EmgMeshes[0].Submeshes[0];

            Xv2Submesh xv2Submesh = new Xv2Submesh(submesh.MaterialName, ModelType.Emg, submesh, null);

            xv2Submesh.BoundingBox = mesh.AABB.ConvertToBoundingBox();
            xv2Submesh.BoundingBoxCenter = mesh.AABB.GetCenter();

            //Triangles
            xv2Submesh.Indices = ArrayConvert.ConvertToIntArray(submesh.Faces);

            //Create vertex array
            xv2Submesh.Vertices = new VertexPositionNormalTextureBlend[mesh.Vertices.Count];

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                xv2Submesh.Vertices[i].Position = new Vector3(mesh.Vertices[i].PositionX, mesh.Vertices[i].PositionY, mesh.Vertices[i].PositionZ);
                xv2Submesh.Vertices[i].Normal = new Vector3(mesh.Vertices[i].NormalX, mesh.Vertices[i].NormalY, mesh.Vertices[i].NormalZ);
                xv2Submesh.Vertices[i].Tangent = new Vector3(mesh.Vertices[i].TangentX, mesh.Vertices[i].TangentY, mesh.Vertices[i].TangentZ);
                xv2Submesh.Vertices[i].TextureUV0 = new Vector2(mesh.Vertices[i].TextureU, mesh.Vertices[i].TextureV);
                xv2Submesh.Vertices[i].TextureUV1 = new Vector2(mesh.Vertices[i].Texture2U, mesh.Vertices[i].Texture2V);
                xv2Submesh.Vertices[i].Color_R = mesh.Vertices[i].ColorR;
                xv2Submesh.Vertices[i].Color_G = mesh.Vertices[i].ColorG;
                xv2Submesh.Vertices[i].Color_B = mesh.Vertices[i].ColorB;
                xv2Submesh.Vertices[i].Color_A = mesh.Vertices[i].ColorA;
            }

            xv2Submesh.CreateBuffers();
            return xv2Submesh;
        }

        public static Xv2ModelFile LoadEmgInContainer(EMG_File emgFile)
        {
            Xv2ModelFile modelFile = new Xv2ModelFile();
            modelFile.Type = ModelType.Emg;

            modelFile.Models.Add(new Xv2Model("root_model", null));
            modelFile.Models[0].Meshes.Add(new Xv2Mesh("root_mesh", null, modelFile.Models[0]));
            modelFile.Models[0].Meshes[0].Submeshes.Add(LoadEmg(emgFile));

            return modelFile;
        }

        private void LoadEmd(bool registerEvent = false)
        {
            Models.Clear();

            //Models
            foreach (EMD_Model emdModel in SourceEmdFile.Models)
            {
                Xv2Model model = new Xv2Model(emdModel.Name, emdModel);

                foreach (EMD_Mesh emdMesh in emdModel.Meshes)
                {
                    Xv2Mesh mesh = new Xv2Mesh(emdMesh.Name, emdMesh, model);

                    foreach (EMD_Submesh emdSubmesh in emdMesh.Submeshes)
                    {
                        //EACH triangle list needs to be its own Xv2Submesh. Merging them together cannot work with the vanilla game shaders as they cant accept more than 24 bones

                        foreach (EMD_Triangle triangleList in emdSubmesh.Triangles)
                        {
                            Xv2Submesh submesh = new Xv2Submesh(emdSubmesh.Name, Type, emdSubmesh, mesh);

                            submesh.BoundingBox = emdSubmesh.AABB.ConvertToBoundingBox();
                            submesh.BoundingBoxCenter = emdSubmesh.AABB.GetCenter();

                            //Triangles
                            submesh.Indices = new int[triangleList.Faces.Count];

                            for (int i = 0; i < triangleList.Faces.Count; i++)
                            {
                                submesh.Indices[i] = triangleList.Faces[i];
                            }

                            //Create vertex array
                            submesh.Vertices = new VertexPositionNormalTextureBlend[emdSubmesh.VertexCount];

                            for (int i = 0; i < emdSubmesh.VertexCount; i++)
                            {
                                submesh.Vertices[i].Position = new Vector3(emdSubmesh.Vertexes[i].PositionX, emdSubmesh.Vertexes[i].PositionY, emdSubmesh.Vertexes[i].PositionZ);
                                submesh.Vertices[i].Tangent = new Vector3(emdSubmesh.Vertexes[i].TangentX, emdSubmesh.Vertexes[i].TangentY, emdSubmesh.Vertexes[i].TangentZ);
                                submesh.Vertices[i].TextureUV0 = new Vector2(emdSubmesh.Vertexes[i].TextureU, emdSubmesh.Vertexes[i].TextureV);
                                submesh.Vertices[i].TextureUV1 = new Vector2(emdSubmesh.Vertexes[i].Texture2U, emdSubmesh.Vertexes[i].Texture2V);
                                submesh.Vertices[i].Color_R = emdSubmesh.Vertexes[i].ColorR;
                                submesh.Vertices[i].Color_G = emdSubmesh.Vertexes[i].ColorG;
                                submesh.Vertices[i].Color_B = emdSubmesh.Vertexes[i].ColorB;
                                submesh.Vertices[i].Color_A = emdSubmesh.Vertexes[i].ColorA;

                                if (emdSubmesh.VertexFlags.HasFlag(VertexFlags.Normal))
                                {
                                    submesh.Vertices[i].Normal = new Vector3(emdSubmesh.Vertexes[i].NormalX, emdSubmesh.Vertexes[i].NormalY, emdSubmesh.Vertexes[i].NormalZ);
                                }

                                if (emdSubmesh.VertexFlags.HasFlag(VertexFlags.BlendWeight))
                                {
                                    submesh.Vertices[i].BlendIndex0 = emdSubmesh.Vertexes[i].BlendIndexes[0];
                                    submesh.Vertices[i].BlendIndex1 = emdSubmesh.Vertexes[i].BlendIndexes[1];
                                    submesh.Vertices[i].BlendIndex2 = emdSubmesh.Vertexes[i].BlendIndexes[2];
                                    submesh.Vertices[i].BlendIndex3 = emdSubmesh.Vertexes[i].BlendIndexes[3];
                                    submesh.Vertices[i].BlendWeights = new Vector3(emdSubmesh.Vertexes[i].BlendWeights[0], emdSubmesh.Vertexes[i].BlendWeights[1], emdSubmesh.Vertexes[i].BlendWeights[2]);
                                }
                            }

                            submesh.SamplerDefs = emdSubmesh.TextureSamplerDefs;
                            submesh.InitSamplers();

                            submesh.EnableSkinning = emdSubmesh.VertexFlags.HasFlag(VertexFlags.BlendWeight);
                            submesh.BoneNames = triangleList.Bones.ToArray();

                            if (submesh.Vertices.Length > 0)
                            {
                                mesh.Submeshes.Add(submesh);
                            }
                        }
                    }

                    model.Meshes.Add(mesh);
                }

                Models.Add(model);
            }

            if (registerEvent)
            {
                SourceEmdFile.ModelModified += SourceEmdFile_ModelModified;
            }

            CalculateAABBs();
            CreateBuffers();
        }

        private void LoadEmo(bool registerEvent = false)
        {
            int partIdx = 0;

            //Models are rendered in the OPPOSITE order of which they are defined in EMO, so we have to read this backwards
            for (int a = SourceEmoFile.Parts.Count - 1; a >= 0; a--)
            {
                foreach (var model in SourceEmoFile.Parts[a].EmgFiles)
                {
                    Xv2Model xv2Model = new Xv2Model(SourceEmoFile.Parts[a].Name, model);
                    var bone = SourceEmoFile.Skeleton.Bones.FirstOrDefault(x => x.EmoPartIndex == partIdx);

                    //The bone and model name SHOULD be the same (they are in all vanilla files), but just to guard against them possibilily being different, we can overwrite the model name with the bone name
                    //Ensuring that the bone linking will work later (it is done by bone name, in the same manner as NSK)
                    if(bone != null)
                        xv2Model.Name = bone.Name;

                    foreach (var mesh in model.EmgMeshes)
                    {
                        Xv2Mesh xv2Mesh = new Xv2Mesh("", mesh, xv2Model);

                        foreach (var submesh in mesh.Submeshes)
                        {
                            Xv2Submesh xv2Submesh = new Xv2Submesh(submesh.MaterialName, Type, submesh, xv2Mesh);

                            xv2Submesh.BoundingBox = mesh.AABB.ConvertToBoundingBox();
                            xv2Submesh.BoundingBoxCenter = mesh.AABB.GetCenter();

                            //Triangles
                            xv2Submesh.Indices = ArrayConvert.ConvertToIntArray(submesh.Faces);

                            //Create vertex array
                            xv2Submesh.Vertices = new VertexPositionNormalTextureBlend[mesh.Vertices.Count];

                            for (int i = 0; i < mesh.Vertices.Count; i++)
                            {
                                xv2Submesh.Vertices[i].Position = new Vector3(mesh.Vertices[i].PositionX, mesh.Vertices[i].PositionY, mesh.Vertices[i].PositionZ);
                                xv2Submesh.Vertices[i].Normal = new Vector3(mesh.Vertices[i].NormalX, mesh.Vertices[i].NormalY, mesh.Vertices[i].NormalZ);
                                xv2Submesh.Vertices[i].Tangent = new Vector3(mesh.Vertices[i].TangentX, mesh.Vertices[i].TangentY, mesh.Vertices[i].TangentZ);
                                xv2Submesh.Vertices[i].TextureUV0 = new Vector2(mesh.Vertices[i].TextureU, mesh.Vertices[i].TextureV);
                                xv2Submesh.Vertices[i].TextureUV1 = new Vector2(mesh.Vertices[i].Texture2U, mesh.Vertices[i].Texture2V);
                                xv2Submesh.Vertices[i].Color_R = mesh.Vertices[i].ColorR;
                                xv2Submesh.Vertices[i].Color_G = mesh.Vertices[i].ColorG;
                                xv2Submesh.Vertices[i].Color_B = mesh.Vertices[i].ColorB;
                                xv2Submesh.Vertices[i].Color_A = mesh.Vertices[i].ColorA;

                                if (mesh.VertexFlags.HasFlag(VertexFlags.BlendWeight))
                                {
                                    xv2Submesh.Vertices[i].BlendIndex0 = mesh.Vertices[i].BlendIndexes[0];
                                    xv2Submesh.Vertices[i].BlendIndex1 = mesh.Vertices[i].BlendIndexes[1];
                                    xv2Submesh.Vertices[i].BlendIndex2 = mesh.Vertices[i].BlendIndexes[2];
                                    xv2Submesh.Vertices[i].BlendIndex3 = mesh.Vertices[i].BlendIndexes[3];
                                    xv2Submesh.Vertices[i].BlendWeights = new Vector3(mesh.Vertices[i].BlendWeights[0], mesh.Vertices[i].BlendWeights[1], mesh.Vertices[i].BlendWeights[2]);
                                }
                            }

                            //Samplers
                            xv2Submesh.SamplerDefs = mesh.TextureLists[submesh.TextureListIndex].TextureSamplerDefs;
                            xv2Submesh.InitSamplers();

                            xv2Submesh.EnableSkinning = mesh.VertexFlags.HasFlag(VertexFlags.BlendWeight);

                            //Generate bone index list
                            xv2Submesh.BoneNames = new string[24];

                            for (short i = 0; i < submesh.Bones.Count; i++)
                            {
                                xv2Submesh.BoneNames[i] = SourceEmoFile.Skeleton.Bones[submesh.Bones[i]].Name;
                            }

                            if (xv2Submesh.Vertices.Length > 0)
                            {
                                xv2Mesh.Submeshes.Add(xv2Submesh);
                            }
                        }

                        xv2Model.Meshes.Add(xv2Mesh);
                    }

                    Models.Add(xv2Model);
                }

                partIdx++;
            }

            CalculateAABBs();
            CreateBuffers();
        }

        private void LoadInternalSkeleton()
        {
            switch (Type)
            {
                case ModelType.Nsk:
                    Skeleton = new Xv2Skeleton(SourceNskFile.EskFile);
                    break;
                case ModelType.Emo:
                    Skeleton = new Xv2Skeleton(SourceEmoFile.Skeleton);
                    break;
                default:
                    break;
                    throw new InvalidOperationException($"Xv2ModelFile.LoadInternalSkeleton: Model type {Type} does not have a internal skeleton!");
            }

            InitializeAttachBones();
        }

        private void InitializeAttachBones()
        {
            foreach(var model in Models)
            {
                model.SetAttachBone(Skeleton);
            }
        }

        private void SourceEmdFile_ModelModified(object source, ModelModifiedEventArgs e)
        {
            if (e.EditType == EditTypeEnum.Remove || e.EditType == EditTypeEnum.Add)
            {
                //TODO: Use context to remove/add only whats needed

                if (SourceEmdFile != null)
                {
                    LoadEmd(false);
                    LoadInternalSkeleton();
                }
                else if (SourceEmoFile != null)
                {
                    LoadEmo(false);
                    LoadInternalSkeleton();
                }

                MaterialsChanged?.Invoke(this, EventArgs.Empty);
            }
            else if(e.EditType == EditTypeEnum.Sampler)
            {
                if (e.Context is EMD_Submesh emdSubmesh)
                {
                    foreach(var submesh in GetSubmeshes(emdSubmesh))
                    {
                        submesh.InitSamplers();
                    }
                }
                else if (e.Context is EMG_Submesh emgSubmesh)
                {
                    foreach (var submesh in GetSubmeshes(emgSubmesh))
                    {
                        submesh.InitSamplers();
                    }
                }
                else
                {
                    ReinitializeTextureSamplers();
                }
            }
            else if(e.EditType == EditTypeEnum.Material)
            {
                MaterialsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ReinitializeTextureSamplers()
        {
            for (int i = 0; i < SourceEmdFile.Models.Count; i++)
            {
                for (int a = 0; a < SourceEmdFile.Models[i].Meshes.Count; a++)
                {
                    for (int s = 0; s < SourceEmdFile.Models[i].Meshes[a].Submeshes.Count; s++)
                    {
                        //Triangle lists are separated into submeshes in XenoKit, so we must account for that here.
                        for (int b = 0; b < SourceEmdFile.Models[i].Meshes[a].Submeshes[s].Triangles.Count; b++)
                        {
                            Models[i].Meshes[a].Submeshes[s + b].InitSamplers();
                        }
                    }
                }
            }
        }

        #endregion

        #region Rendering

        public void Draw(Matrix4x4 world, int actor, Xv2ShaderEffect[] materials, Xv2Texture[] textures, Xv2Texture[] dyts, int dytIdx, Xv2Skeleton skeleton = null)
        {
            if (skeleton == null)
                skeleton = Skeleton;

            foreach (Xv2Model model in Models)
            {
                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        submesh.Draw(ref world, actor, materials, textures, dyts, dytIdx, skeleton);
                    }
                }
            }
        }

        /// <summary>
        /// Draw this model with just one material and no textures or dyts. Intended for earlier passes only (shadow/normals).
        /// </summary>
        public void Draw(Matrix4x4 world, int actor, Xv2ShaderEffect material, Xv2Skeleton skeleton = null)
        {
            if (skeleton == null)
                skeleton = Skeleton;

            foreach (Xv2Model model in Models)
            {
                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        submesh.Draw(ref world, actor, material, skeleton);
                    }
                }
            }
        }

        #endregion

        public void SetAsReflectionMesh(bool isReflection)
        {
            if (isReflection == IsReflectionMesh) return;
            IsReflectionMesh = isReflection;

            //Invert normal Y so they are correct in the reflection
            foreach(Xv2Model model in Models)
            {
                foreach(Xv2Mesh mesh in model.Meshes)
                {
                    foreach(Xv2Submesh submesh in mesh.Submeshes)
                    {
                        for(int i = 0; i < submesh.Vertices.Length; i++)
                        {
                            submesh.Vertices[i].Normal = new Vector3(submesh.Vertices[i].Normal.X, -submesh.Vertices[i].Normal.Y, submesh.Vertices[i].Normal.Z);
                            submesh.Vertices[i].Tangent = new Vector3(submesh.Vertices[i].Tangent.X, -submesh.Vertices[i].Tangent.Y, submesh.Vertices[i].Tangent.Z);
                        }

                    }
                }
            }

            CreateBuffers();
        }

        public void CreateBuffers()
        {
            foreach (Xv2Model model in Models)
            {
                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        submesh.CreateBuffers();
                    }
                }
            }
        }

        public void InitMaterialIndex(Xv2ShaderEffect[] materials)
        {
            foreach (Xv2Model model in Models)
            {
                foreach (Xv2Mesh mesh in model.Meshes)
                {
                    foreach (Xv2Submesh submesh in mesh.Submeshes)
                    {
                        submesh.InitMaterialIndex(materials);
                    }
                }
            }
        }


        #region Helpers

        public Xv2Model GetModel(object sourceModelObject)
        {
            foreach (var model in Models)
            {
                if (model.SourceModel == sourceModelObject) return model;
            }

            return null;
        }

        public Xv2Mesh GetMesh(object sourceMeshObject)
        {
            foreach (var model in Models)
            {
                foreach(var mesh in model.Meshes)
                {
                    if (mesh.SourceMesh == sourceMeshObject) return mesh;
                }
            }

            return null;
        }

        public IEnumerable<Xv2Submesh> GetSubmeshes(object sourceSubmesh)
        {
            foreach (var model in Models)
            {
                foreach (var mesh in model.Meshes)
                {
                    foreach (var submesh in mesh.Submeshes)
                    {
                        if (submesh.SourceSubmesh == sourceSubmesh)
                            yield return submesh;
                    }
                }
            }
        }

        public void GetAllSubmeshesFromSourceObjects(IList<object> sourceObjects, List<Xv2Submesh> outputList)
        {
            foreach(var obj in sourceObjects)
            {
                var submeshes = GetAllSubmeshesFromSourceObject(obj);

                if(submeshes != null)
                    outputList.AddRange(submeshes);
            }
        }

        public List<Xv2Submesh> GetAllSubmeshesFromSourceObject(object sourceObject)
        {
            if (sourceObject is EMD_Model model)
            {
                var _model = GetModel(model);
                if(_model != null)
                    return new List<Xv2Submesh>(_model.GetSubmeshes());
            }
            else if (sourceObject is EMD_Mesh mesh)
            {
                var _mesh = GetMesh(mesh);
                if (_mesh != null)
                    return new List<Xv2Submesh>(_mesh.GetSubmeshes());
            }
            else if (sourceObject is EMD_Submesh submesh)
            {
                return new List<Xv2Submesh>(GetSubmeshes(submesh));
            }

            return null;
        }
        
        public void CalculateAABBs()
        {
            foreach(var model in Models)
            {
                model.CalculateBounds();
            }
        }
        #endregion

    }

    public class Xv2Model : RenderObject
    {
        public Xv2Bone AttachBone { get; private set; }

        public List<Xv2Mesh> Meshes { get; private set; } = new List<Xv2Mesh>();

        public object SourceModel { get; private set; }

        public BoundingBox BoundingBox { get; set; }
        public SimdVector3 BoundingBoxCenter { get; set; }


        public Xv2Model(string name, object sourceModelObj)
        {
            Name = name;
            SourceModel = sourceModelObj;
        }

        public void SetAttachBone(Xv2Skeleton skeleton)
        {
            int boneIdx = skeleton.GetBoneIndex(Name);
            AttachBone = skeleton.Bones.Length > boneIdx && boneIdx != -1 ? skeleton.Bones[boneIdx] : null;
        }

        private Matrix GetTransformedWorld(ref Matrix world, Xv2Skeleton skeleton)
        {
            /*
            if (AttachBoneIdx != -1 && skeleton != null)
            {
                //This is right and wrong depending on the case... why?
                //When return world = fixes; broken effects, like sparks, but breaks boost auras
                //When skeleton.Bones[AttachBone].SkinningMatrix * world; fixes boost auras, breaks sparks
                //Stages only work properly with AbsoluteMatrix * world

                //return world;
                return skeleton.Bones[AttachBoneIdx].AbsoluteAnimationMatrix * world;
            }
            */
            return world;
        }

        public IEnumerable<Xv2Submesh> GetSubmeshes()
        {
            foreach (var mesh in Meshes)
            {
                foreach (var submesh in mesh.Submeshes)
                {
                    yield return submesh;
                }
            }
        }

        public void CalculateBounds()
        {
            SimdVector3 min = new SimdVector3(float.PositiveInfinity);
            SimdVector3 max = new SimdVector3(float.NegativeInfinity);

            foreach (var mesh in Meshes)
            {
                min = SimdVector3.Min(mesh.BoundingBox.Min.ToNumerics(), min);
                max = SimdVector3.Max(mesh.BoundingBox.Max.ToNumerics(), max);
            }

            BoundingBox = new BoundingBox(min, max);
            BoundingBoxCenter = (min + max) * 0.5f;
        }
    }

    public class Xv2Mesh : RenderObject
    {
        public List<Xv2Submesh> Submeshes { get; set; } = new List<Xv2Submesh>();

        public Xv2Model Parent { get; private set; }
        public object SourceMesh { get; private set; }

        public BoundingBox BoundingBox { get; set; }
        public SimdVector3 BoundingBoxCenter { get; set; }

        public Xv2Mesh(string name, object sourceMeshObj, Xv2Model parent)
        {
            Name = name;
            SourceMesh = sourceMeshObj;
            Parent = parent;
        }

        public List<Xv2Submesh> GetSubmeshes(EMD_Submesh submesh)
        {
            List<Xv2Submesh> submeshes = new List<Xv2Submesh>();

            foreach(var _submesh in Submeshes)
            {
                if(_submesh.SourceSubmesh == submesh)
                {
                    submeshes.Add(_submesh);
                }
            }

            return submeshes;
        }
    
        public IEnumerable<Xv2Submesh> GetSubmeshes()
        {
            foreach (var submesh in Submeshes)
            {
                yield return submesh;
            }
        }
    
        public void CalculateBounds()
        {
            SimdVector3 min = new SimdVector3(float.PositiveInfinity);
            SimdVector3 max = new SimdVector3(float.NegativeInfinity);

            foreach(var submesh in Submeshes)
            {
                min = SimdVector3.Min(submesh.BoundingBox.Min.ToNumerics(), min);
                max = SimdVector3.Max(submesh.BoundingBox.Max.ToNumerics(), max);
            }

            BoundingBox = new BoundingBox(min, max);
            BoundingBoxCenter = (min + max) * 0.5f;
        }
    }

    public class Xv2Submesh : RenderObject
    {
        public override EngineObjectTypeEnum EngineObjectType => EngineObjectTypeEnum.Model;

        public int MaterialIndex { get; private set; }
        public ModelType Type { get; private set; }

        //Source:
        public Xv2Mesh Parent { get; private set; }
        public object SourceSubmesh { get; private set; } //EMD_Submesh or EMG_Submesh
        public AsyncObservableCollection<EMD_TextureSamplerDef> SamplerDefs { get; set; } //From EMD_Submesh or EMG_Mesh


        //Vertices:
        public VertexBuffer VertexBuffer { get; set; }
        public IndexBuffer IndexBuffer { get; set; }
        public VertexPositionNormalTextureBlend[] Vertices { get; set; }
        public int[] Indices { get; set; }
        public int[] UsedIndices { get; set; }

        //AABB
        public BoundingBox BoundingBox { get; set; }
        public SimdVector3 BoundingBoxCenter { get; set; }
        private readonly DrawableBoundingBox VisibleAABB;

        //Samplers:
        public SamplerInfo[] Samplers { get; set; }
        public float[] TexTile01 { get; private set; } = new float[4];
        public float[] TexTile23 { get; private set; } = new float[4];

        //Custom Color DYT
        private int CustomColrDytCreatedFromIndex = 0;
        private int CustomColorIndex = -1;
        private int CustomColorGroup = -1;
        public Xv2Texture CustomColorDyt = null;

        //Skinning:
        public bool EnableSkinning { get; set; }
        public string[] BoneNames;
        public readonly Dictionary<Xv2Skeleton, short[]> BoneIdx = new Dictionary<Xv2Skeleton, short[]>(); //Bone indices are cached per skeleton instance
        public Matrix4x4[] SkinningMatrices = new Matrix4x4[24];
        private static Matrix4x4[] DefaultSkinningMatrices = new Matrix4x4[24];

        //Actor-Specific Information:
        private Matrix4x4[] PrevWVP = new Matrix4x4[SceneManager.NumActors];

        static Xv2Submesh()
        {
            for (int i = 0; i < 24; i++)
                DefaultSkinningMatrices[i] = Matrix4x4.Identity;
        }

        public Xv2Submesh(string name, ModelType type, object sourceSubmesh, Xv2Mesh parent)
        {
            Name = name;
            Type = type;
            SourceSubmesh = sourceSubmesh;
            Parent = parent;

#if DEBUG
            VisibleAABB = new DrawableBoundingBox();
#endif
        }

        public void Draw(ref Matrix4x4 world, int actor, Xv2ShaderEffect[] materials, Xv2Texture[] textures, Xv2Texture[] dyts, int dytIdx, Xv2Skeleton skeleton = null)
        {
            if (materials == null) return;

            Matrix4x4 newWorld = Parent.Parent.AttachBone != null ? Transform * Parent.Parent.AttachBone.AbsoluteAnimationMatrix * world : Transform * world;

            Xv2ShaderEffect material = MaterialIndex != -1 ? materials[MaterialIndex] : DefaultShaders.VertexColor_W;

            if (!RenderSystem.CheckDrawPass(material)) return;

            if (!FrustumIntersects(newWorld))
                return;

            //Handle BCS Colors
            if (dyts != null)
            {
                UpdateCustomColorDyt(actor, dytIdx, dyts, material);

                if (CustomColorDyt != null)
                {
                    GraphicsDevice.Textures[4] = CustomColorDyt.Texture;
                    GraphicsDevice.VertexTextures[4] = CustomColorDyt.Texture;
                }
            }

            material.World = newWorld;
            material.PrevWVP = PrevWVP[actor];

            //Set samplers/textures
            foreach (SamplerInfo sampler in Samplers)
            {
                GraphicsDevice.SamplerStates[sampler.samplerSlot] = sampler.state;
                //GraphicsDevice.VertexSamplerStates[sampler.samplerSlot] = sampler.state;

                //Set textures if index is valid.
                if (sampler.parameter <= textures?.Length - 1 && sampler.parameter >= 0)
                {
                    //GraphicsDevice.VertexTextures[sampler.textureSlot] = textures[sampler.parameter].Texture;
                    GraphicsDevice.Textures[sampler.textureSlot] = textures[sampler.parameter].Texture;
                }
            }

            material.SetTextureTile(TexTile01, TexTile23);

            DrawEnd(actor, material, skeleton);

            //Draw AABBs
            if(SceneManager.BoundingBoxVisible && VisibleAABB != null)
            {
                VisibleAABB.Draw(world, BoundingBox);
            }
        }

        public void Draw(ref Matrix4x4 world, int actor, Xv2ShaderEffect material, Xv2Skeleton skeleton = null)
        {
            //if (!RenderSystem.CheckDrawPass(material)) return;

            Matrix4x4 newWorld = Parent.Parent.AttachBone != null ? Transform * Parent.Parent.AttachBone.AbsoluteAnimationMatrix * world : Transform * world;

            if (!FrustumIntersects(newWorld))
                return;

            material.World = newWorld;
            material.PrevWVP = PrevWVP[actor];

            DrawEnd(actor, material, skeleton);
        }

        private void DrawEnd(int actor, Xv2ShaderEffect material, Xv2Skeleton skeleton)
        {
            RenderSystem.MeshDrawCalls++;
            material.ActorSlot = actor;

            if (EnableSkinning && skeleton != null)
            {
                CreateSkinningMatrices(skeleton);
                material.SetSkinningMatrices(SkinningMatrices);
            }
            else
            {
                material.SetSkinningMatrices(DefaultSkinningMatrices);
            }

            //Shader passes and vertex drawing
            foreach (EffectPass pass in material.CurrentTechnique.Passes)
            {
                if (Type == ModelType.Emd)
                    material.SetColorFade(SceneManager.Actors[actor]);

                material.SetVfxLight();

                pass.Apply();

                //GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Vertices, 0, Vertices.Length, Indices, 0, Indices.Length / 3);
                GraphicsDevice.SetVertexBuffer(VertexBuffer);
                GraphicsDevice.Indices = IndexBuffer;
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexBuffer.IndexCount / 3);

            }

            PrevWVP[actor] = material.WVP;
        }

        private bool FrustumIntersects(Matrix world)
        {
            if (Type != ModelType.Nsk) return true; //Only do culling for NSKs right now. EMD and EMO can be animated, which causes problems for calculating the bounding boxes since the final transformation is done in shader code (on GPU), and not known before drawing. Not worth the effort to workaround
#if DEBUG
            if (!SceneManager.FrustumCullEnabled) return true;
#endif
            if (Vector3.Distance(world.Translation, ViewportInstance.Camera.CameraState.Position) < 1f) return true;

            BoundingFrustum frustum = RenderSystem.IsShadowPass ? ViewportInstance.SunLight.LightFrustum : ViewportInstance.Camera.Frustum;

            return frustum.Intersects(BoundingBox.Transform(world));
        }

        private short[] GetBoneIndices(Xv2Skeleton skeleton)
        {
            short[] indices;

            if (BoneIdx.TryGetValue(skeleton, out indices))
                return indices;

            indices = new short[BoneNames.Length];

            for (int i = 0; i < BoneNames.Length; i++)
            {
                indices[i] = (short)skeleton.GetBoneIndex(BoneNames[i]);
            }

            BoneIdx.Add(skeleton, indices);

            return indices;
        }

        private void CreateSkinningMatrices(Xv2Skeleton skeleton)
        {
            short[] baseBoneIndices = GetBoneIndices(skeleton);

            for (int i = 0; i < BoneNames.Length; i++)
            {
                int boneIdx = baseBoneIndices[i];

                if (boneIdx != -1)
                {
                    SkinningMatrices[i] = skeleton.Bones[boneIdx].SkinningMatrix;
                }
                else
                {
                    SkinningMatrices[i] = Matrix4x4.Identity;
                }
            }
        }

        #region CustomColor
        private void UpdateCustomColorDyt(int actor, int dytIdx, Xv2Texture[] Dyts, Xv2ShaderEffect material)
        {
            if ((CustomColrDytCreatedFromIndex != dytIdx || CustomColorDyt == null) && CustomColorIndex != -1 && Dyts.Length > CustomColrDytCreatedFromIndex)
            {
                CustomColorDyt?.Dispose();

                var colors = SceneManager.Actors[actor].CharacterData.BcsFile.File.GetColor(CustomColorGroup, CustomColorIndex);

                if (colors == null)
                {
                    CustomColorDyt = null;
                    return;
                }

                var dyt = Dyts[dytIdx].HardCopy();
                int dytLineBase = (int)(material.Material.DecompiledParameters.MatScale1.X) * 16;

                for (int c = 0; c < 4; c++)
                {
                    //As far as I can tell, Color 2 isn't used by the game... so skip
                    if (c == 1) continue;

                    int dytLine = c * 4;
                    int height = dytLineBase + dytLine;

                    for (int width = 0; width < dyt.EmbEntry.Texture.PixelWidth; width++)
                    {
                        var bcsColor = colors.GetColor(c);
                        Xv2CoreLib.HslColor.HslColor color = new Xv2CoreLib.HslColor.RgbColor(bcsColor).ToHsl();

                        var pixelRgb = dyt.EmbEntry.Texture.GetPixel(width, height + dytLine);
                        Xv2CoreLib.HslColor.HslColor pixelColor = new Xv2CoreLib.HslColor.RgbColor(pixelRgb.R, pixelRgb.G, pixelRgb.B).ToHsl();
                        pixelColor.SetHue(color.Hue);
                        pixelColor.Saturation = color.Saturation;
                        pixelColor.Lightness = (color.Lightness * 0.8f) + (pixelColor.Lightness * 0.2f); //BCS colors only keep 20% of the pixels original lightness

                        var newPixelRgb = pixelColor.ToRgb();

                        //The pixel is colored by a factor of the original color and that defined in the BCS color (e.g: if BCS A is 0, then only the original pixels color is kept, but if its 1, then it should only be the BCS color, or if its somewhere inbetween, then they are merged)
                        float originalFactor = 1f - bcsColor.A;
                        byte r = (byte)((newPixelRgb.R_int * bcsColor.A) + (pixelRgb.R * originalFactor));
                        byte g = (byte)((newPixelRgb.G_int * bcsColor.A) + (pixelRgb.G * originalFactor));
                        byte b = (byte)((newPixelRgb.B_int * bcsColor.A) + (pixelRgb.B * originalFactor));

                        dyt.EmbEntry.Texture.SetPixel(width, height + dytLine, pixelRgb.A, r, g, b);
                        dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 1, pixelRgb.A, r, g, b);
                        dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 2, pixelRgb.A, r, g, b);
                        dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 3, pixelRgb.A, r, g, b);

                        //dyt.EmbEntry.Texture.SetPixel(width, height + dytLine, pixelRgb.A, newPixelRgb.R_int, newPixelRgb.G_int, newPixelRgb.B_int);
                        //dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 1, pixelRgb.A, newPixelRgb.R_int, newPixelRgb.G_int, newPixelRgb.B_int);
                        //dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 2, pixelRgb.A, newPixelRgb.R_int, newPixelRgb.G_int, newPixelRgb.B_int);
                        //dyt.EmbEntry.Texture.SetPixel(width, height + dytLine + 3, pixelRgb.A, newPixelRgb.R_int, newPixelRgb.G_int, newPixelRgb.B_int);

                    }
                }

                dyt.EmbEntry.SaveDds(false);
                dyt.IsDirty = true;
                CustomColorDyt = dyt;

                //System.IO.File.WriteAllBytes("OG.dds", Dyts[dytIdx].EmbEntry.Data);
                //System.IO.File.WriteAllBytes("NEW.dds", dyt.EmbEntry.Data);

                CustomColrDytCreatedFromIndex = dytIdx;
            }
        }

        public void ApplyCustomColor(int colorGroup, int colorIndex)
        {
            if (CustomColorGroup != colorGroup || CustomColorIndex != colorIndex)
            {
                CustomColorDyt = null;
                CustomColorIndex = colorIndex;
                CustomColorGroup = colorGroup;
            }
        }

        public void ResetCustomColor()
        {
            CustomColorDyt = null;
            CustomColorGroup = -1;
            CustomColorIndex = -1;
        }
        #endregion

        #region Init
        public void CreateBuffers()
        {
            if (VertexBuffer == null)
            {
                VertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTextureBlend), Vertices.Length, BufferUsage.WriteOnly);
            }
            VertexBuffer.SetData(Vertices);

            if (IndexBuffer == null)
            {
                IndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, Indices.Length, BufferUsage.WriteOnly);
            }
            IndexBuffer.SetData(Indices);
        }

        public void InitSamplers()
        {
            if (SamplerDefs == null)
            {
                throw new Exception($"Xv2Submesh.InitSamplers: Samplers have not been set");
            }
            Samplers = new SamplerInfo[SamplerDefs.Count];

            for (int i = 0; i < SamplerDefs.Count; i++)
            {
                Samplers[i].type = SamplerType.Sampler2D; //todo: shadow maps are a different sampler type (sampler_c... SamplerCube?)
                Samplers[i].textureSlot = i;
                Samplers[i].samplerSlot = i;

                Samplers[i].state = new SamplerState();
                Samplers[i].state.AddressU = GetTextureAddressMode(SamplerDefs[i].AddressModeU);
                Samplers[i].state.AddressV = GetTextureAddressMode(SamplerDefs[i].AddressModeV);
                Samplers[i].state.AddressW = TextureAddressMode.Wrap;
                Samplers[i].state.BorderColor = new Color(1, 1, 1, 1);
                Samplers[i].state.Filter = GetTextureFilter(SamplerDefs[i].FilteringMin, SamplerDefs[i].FilteringMag);
                Samplers[i].state.MaxAnisotropy = 1;
                Samplers[i].state.MaxMipLevel = 1;
                Samplers[i].state.FilterMode = TextureFilterMode.Default;
                Samplers[i].name = ShaderManager.GetSamplerName(i);
                Samplers[i].state.Name = Samplers[i].name;
                Samplers[i].parameter = SamplerDefs[i].EmbIndex;
            }
        
            //Set the texture tile parameters. These are used by the vertex shader to apply the correct tiling to the textures
            if(SamplerDefs.Count >= 1)
            {
                TexTile01[0] = SamplerDefs[0].ScaleU;
                TexTile01[1] = SamplerDefs[0].ScaleV;
            }

            if (SamplerDefs.Count >= 2)
            {
                TexTile01[2] = SamplerDefs[1].ScaleU;
                TexTile01[3] = SamplerDefs[1].ScaleV;
            }

            if (SamplerDefs.Count >= 3)
            {
                TexTile23[0] = SamplerDefs[2].ScaleU;
                TexTile23[1] = SamplerDefs[2].ScaleV;
            }

            if (SamplerDefs.Count >= 4)
            {
                TexTile23[2] = SamplerDefs[3].ScaleU;
                TexTile23[3] = SamplerDefs[3].ScaleV;
            }
        }

        public void InitMaterialIndex(Xv2ShaderEffect[] materials)
        {
            MaterialIndex = -1;
            string matName = GetMaterialName();

            for(int i = 0; i < materials.Length; i++)
            {
                if (materials[i].Material.Name == matName)
                {
                    MaterialIndex = i;
                    break;
                }
            }
        }

        private string GetMaterialName()
        {
            if(SourceSubmesh is EMD_Submesh emdSubmesh)
            {
                return emdSubmesh.Name;
            }
            else if(SourceSubmesh is EMG_Submesh emgSubmesh)
            {
                return emgSubmesh.MaterialName;
            }
            else
            {
                return null;
            }
        }

        public void SetLodBias(EmmMaterial material)
        {
            for (int i = 0; i < Samplers.Length; i++)
            {
                //Cant modify sampler after it is bound to the GPU (exception)
                try
                {
                    Samplers[i].state.MipMapLevelOfDetailBias = material != null ? material.DecompiledParameters.GetMipMapLod(i) : 0f;
                }
                catch { }
            }
        }

        private TextureFilter GetTextureFilter(Filtering min, Filtering mag)
        {
            //Mip always linear
            if (min == Filtering.Linear && mag == Filtering.Linear)
            {
                return TextureFilter.Linear;
            }
            else if (min == Filtering.Linear && mag == Filtering.Point)
            {
                return TextureFilter.MinLinearMagPointMipLinear;
            }
            else if (min == Filtering.Point && mag == Filtering.Point)
            {
                return TextureFilter.PointMipLinear;
            }
            else if (min == Filtering.Point && mag == Filtering.Linear)
            {
                return TextureFilter.MinPointMagLinearMipLinear;
            }

            return TextureFilter.Linear;
        }

        private TextureAddressMode GetTextureAddressMode(AddressMode mode)
        {
            switch (mode)
            {
                case AddressMode.Clamp:
                    return TextureAddressMode.Clamp;
                case AddressMode.Mirror:
                    return TextureAddressMode.Mirror;
                case AddressMode.Wrap:
                default:
                    return TextureAddressMode.Wrap;
            }
        }
        #endregion

        public static SimdVector3 CalculateCenter(IList<Xv2Submesh> submeshes)
        {
            SimdVector3 min = new SimdVector3(float.PositiveInfinity);
            SimdVector3 max = new SimdVector3(float.NegativeInfinity);

            if (submeshes?.Count > 0)
            {
                foreach (var submesh in submeshes)
                {
                    min = SimdVector3.Min(submesh.BoundingBox.Min.ToNumerics(), min);
                    max = SimdVector3.Max(submesh.BoundingBox.Max.ToNumerics(), max);
                }
            }

            return (min + max) * 0.5f;
        }
        
        public static BoundingBox CalculateBoundingBox(IList<Xv2Submesh> submeshes)
        {
            CalculateAABB(submeshes, out BoundingBox aabb, out _);
            return aabb;
        }

        private static void CalculateAABB(IList<Xv2Submesh> submeshes, out BoundingBox aabb, out SimdVector3 center)
        {
            SimdVector3 min = new SimdVector3(float.PositiveInfinity);
            SimdVector3 max = new SimdVector3(float.NegativeInfinity);

            if (submeshes?.Count > 0)
            {
                foreach (var submesh in submeshes)
                {
                    Matrix world = submesh.Transform;

                    if (submesh.Parent.Parent.AttachBone != null)
                    {
                        ;
                        world *= submesh.Parent.Parent.AttachBone.AbsoluteAnimationMatrix;
                        BoundingBox transformedBoundingBox = submesh.BoundingBox.Transform(world);
                        min = SimdVector3.Min(transformedBoundingBox.Min.ToNumerics(), min);
                        max = SimdVector3.Max(transformedBoundingBox.Max.ToNumerics(), max);
                    }
                    else
                    {
                        BoundingBox transformedBoundingBox = submesh.BoundingBox.Transform(world);
                        min = SimdVector3.Min(transformedBoundingBox.Min.ToNumerics(), min);
                        max = SimdVector3.Max(transformedBoundingBox.Max.ToNumerics(), max);
                    }
                }
            }

            center = (min + max) * 0.5f;
            aabb = new BoundingBox(min, max);
        }
    
    }

    public enum ModelType
    {
        Emd,
        Nsk,
        Emo,
        Emg
    }
}