using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMG;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMO;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Editor.Data
{
    [Serializable]
    public class SerializedModel
    {
        public const string CLIPBOARD_EMD_MODEL = "EmdModelSerialized";
        public const string CLIPBOARD_EMD_MESH = "EmdMeshSerialized";
        public const string CLIPBOARD_EMD_SUBMESH = "EmdSubmeshSerialized";
        public const string CLIPBOARD_EMO_MODEL = "EmoModelSerialized";
        public const string CLIPBOARD_EMO_MESH = "EmoMeshSerialized";
        public const string CLIPBOARD_EMO_SUBMESH = "EmoSubmeshSerialized";

        //The model object to be serialized. Only one of these will be used, the rest will be null
        public EMD_Model[] EmdModel { get; set; }
        public EMD_Mesh[] EmdMesh { get; set; }
        public EMD_Submesh[] EmdSubmesh { get; set; }
        public EMO_Part[] EmoModel { get; set; }
        public EMG_File[] EmoMesh { get; set; }
        public EMG_Submesh[] EmoSubmesh { get; set; }

        //Material and textures (only those used by the model object will be serialized)
        public List<EmmMaterial> Materials { get; set; } = new List<EmmMaterial>();
        public List<EmbEntry> Textures { get; set; } = new List<EmbEntry>();

        public SerializedModel() { }

        public SerializedModel(EMD_Model[] emdModel, EMM_File emmFile, EMB_File embFile)
        {
            EmdModel = emdModel;

            foreach(var model in EmdModel)
            {
                foreach(var mesh in model.Meshes)
                {
                    foreach(var submesh in mesh.Submeshes)
                    {
                        CreateTextureAndMaterials(submesh, emmFile, embFile);
                    }
                }
            }
        }

        public SerializedModel(EMD_Mesh[] emdMesh, EMM_File emmFile, EMB_File embFile)
        {
            EmdMesh = emdMesh;

            foreach (var mesh in EmdMesh)
            {
                foreach (var submesh in mesh.Submeshes)
                {
                    CreateTextureAndMaterials(submesh, emmFile, embFile);
                }
            }
        }

        public SerializedModel(EMD_Submesh[] emdSubmesh, EMM_File emmFile, EMB_File embFile)
        {
            EmdSubmesh = emdSubmesh;

            foreach (var submesh in EmdSubmesh)
            {
                CreateTextureAndMaterials(submesh, emmFile, embFile);
            }
        }

        private void CreateTextureAndMaterials(EMD_Submesh submesh, EMM_File emmFile, EMB_File embFile)
        {
            CreateTextures(submesh.TextureSamplerDefs, embFile);

            if(Materials.FirstOrDefault(x => x.Name == submesh.Name) == null)
            {
                Materials.Add(emmFile.GetMaterial(submesh.Name));
            }
        }

        private void CreateTextures(IList<EMD_TextureSamplerDef> textureDefs, EMB_File embFile)
        {
            foreach(var textureDef in textureDefs)
            {
                if (Textures.Any(x => x.ID == textureDef.EmbIndex)) continue;
                Textures.Add(embFile.GetEntry(textureDef.EmbIndex));
            }
        }

        #region Paste Methods
        public List<IUndoRedo> PasteTexturesAndMaterials(EMB_File embFile, EMM_File emmFile)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            if(EmdModel != null)
            {
                foreach(var model in EmdModel)
                {
                    foreach(var mesh in model.Meshes)
                    {
                        PasteMaterials(mesh.Submeshes, emmFile, undos);

                        foreach(var submesh in mesh.Submeshes)
                        {
                            PasteTextures(submesh.TextureSamplerDefs, embFile, undos);
                        }
                    }
                }
            }
            else if(EmdMesh != null)
            {
                foreach (var mesh in EmdMesh)
                {
                    PasteMaterials(mesh.Submeshes, emmFile, undos);

                    foreach (var submesh in mesh.Submeshes)
                    {
                        PasteTextures(submesh.TextureSamplerDefs, embFile, undos);
                    }
                }
            }
            else if (EmdSubmesh != null)
            {
                PasteMaterials(EmdSubmesh, emmFile, undos);

                foreach (var submesh in EmdSubmesh)
                {
                    PasteTextures(submesh.TextureSamplerDefs, embFile, undos);
                }
            }

            return undos;
        }

        private void PasteMaterials(IEnumerable<EMD_Submesh> submeshes, EMM_File emmFile, List<IUndoRedo> undos)
        {
            foreach(var submesh in  submeshes)
            {
                if(emmFile.Materials.FirstOrDefault(x => x.Name == submesh.Name) == null)
                {
                    EmmMaterial material = Materials.FirstOrDefault(x => x.Name == submesh.Name);

                    if(material != null)
                    {
                        material.DecompileParameters();
                        emmFile.Materials.Add(material);
                        undos.Add(new UndoableListAdd<EmmMaterial>(emmFile.Materials, material));
                    }
                }
            }
        }

        private void PasteTextures(IList<EMD_TextureSamplerDef> textures, EMB_File embFile, List<IUndoRedo> undos)
        {
            foreach(var texture in textures)
            {
                EmbEntry serializedEmb = Textures.FirstOrDefault(x => x.ID == texture.EmbIndex);

                if (serializedEmb != null)
                {
                    EmbEntry existingEntry = embFile.Compare(serializedEmb);

                    if(existingEntry != null)
                    {
                        texture.EmbIndex = (byte)embFile.Entry.IndexOf(existingEntry);
                    }
                    else
                    {
                        if(embFile.Entry.Count < EMB_File.MAX_EFFECT_TEXTURES)
                        {
                            texture.EmbIndex = (byte)embFile.Entry.Count;
                            embFile.Add(serializedEmb);
                            undos.Add(new UndoableListAdd<EmbEntry>(embFile.Entry, serializedEmb));
                        }
                        else
                        {
                            Log.Add("Exceeded texture limit on paste operation, additional textures were NOT added.", LogType.Warning);
                        }
                    }
                }
            }
        }
        #endregion
    }
}