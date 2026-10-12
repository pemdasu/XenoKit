using System;
using System.IO;
using System.Linq;
using XenoKit.Editor;
using Xv2CoreLib;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMD;
using Xv2CoreLib.EMM;
using Xv2CoreLib.ESK;

namespace XenoKit.Engine
{
    public partial class CharaPart
    {
        #region SourceLoad
        private void LoadSources()
        {
            if (!LoadEmd())
            {
                nullPartSet = true;
                return;
            }
            LoadEmb();
            LoadDyt();
            LoadEmm();
            LoadEan();

            if (IsPhysicsPart)
            {
                LoadEsk();
            }

            nullPartSet = false;
        }

        private bool LoadEmd()
        {
            string path;
            string owner;

            if (IsPhysicsPart)
            {
                path = GetFilePath(physicsPart.GetModelPath());
                owner = physicsPart.CharaCode;
            }
            else
            {
                path = GetFilePath(part.GetModelPath(partType));
                owner = part.CharaCode;
            }

            if (path != null)
            {
                //If the file belongs to this character, then we pull the files from the Xv2Character object itself. 
                if (chara.ShortName == owner && Utils.CompareCharaFolder(path, chara.ShortName))
                {
                    var file = GetFileFromOwner(path);

                    if (file != null)
                        EmdFile = (EMD_File)file;
                }
                else
                {
                    //This character doesn't own these files so we just load them directly.
                    EmdFile = FileManager.Instance.LoadFile<EMD_File>(path, chara.CharacterData.OnlyLoadFromCPK);
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private void LoadEmb()
        {
            string path;
            string owner;

            if(EmbFile != null)
                EmbFile.TexturesChanged -= EmbFile_TexturesChanged;

            if (IsPhysicsPart)
            {
                path = GetFilePath(physicsPart.GetEmbPath(), Parent.part.GetEmbPath(partType));
                owner = physicsPart.CharaCode;
            }
            else
            {
                path = GetFilePath(part.GetEmbPath(partType));
                owner = part.CharaCode;
            }

            if (path != null)
            {
                //If the file belongs to this character, then we pull the files from the Xv2Character object itself. 
                if (chara.ShortName == owner && Utils.CompareCharaFolder(path, chara.ShortName))
                {
                    var file = GetFileFromOwner(path);

                    if (file != null)
                        EmbFile = (EMB_TextureFile)file;
                }
                else
                {
                    //This character doesn't own these files so we just load them directly.
                    EmbFile = FileManager.Instance.LoadFile<EMB_TextureFile>(path, chara.CharacterData.OnlyLoadFromCPK);
                }

                if (EmbFile != null)
                    EmbFile.TexturesChanged += EmbFile_TexturesChanged;
            }
            else
            {
                Log.Add($"EMB not found: {path}.", LogType.Error);
            }
        }

        private void LoadDyt()
        {
            string path;
            string owner = IsPhysicsPart ? physicsPart.CharaCode : part.CharaCode;

            if(DytFile != null)
                DytFile.TexturesChanged -= DytFile_TexturesChanged;

            if (IsPhysicsPart)
            {
                path = GetFilePath(physicsPart.GetDytPath(), Parent.part.GetDytPath(partType));
            }
            else
            {
                path = GetFilePath(part.GetDytPath(partType));
            }

            if (path != null)
            {
                //If the file belongs to this character, then we pull the files from the Xv2Character object itself. 
                if (chara.ShortName == owner && Utils.CompareCharaFolder(path, chara.ShortName))
                {
                    var file = GetFileFromOwner(path);

                    if (file != null)
                        DytFile = (EMB_TextureFile)file;
                }
                else
                {
                    //This character doesn't own these files so we just load them directly.
                    DytFile = FileManager.Instance.LoadFile<EMB_TextureFile>(path, chara.CharacterData.OnlyLoadFromCPK);
                }

                if (DytFile != null)
                    DytFile.TexturesChanged += DytFile_TexturesChanged;
            }
            else
            {
                Log.Add($"DYT not found: {path}.", LogType.Error);
            }
        }

        private void LoadEmm()
        {
            string path;
            string owner = IsPhysicsPart ? physicsPart.CharaCode : part.CharaCode;

            if (EmmFile != null)
                EmmFile.MaterialsChanged -= RefreshMaterialsOnEdit;

            if (IsPhysicsPart)
            {
                path = GetFilePath(physicsPart.GetEmmPath(), Parent.part.GetEmmPath(partType));

            }
            else
            {
                path = GetFilePath(part.GetEmmPath(partType));
            }

            if (path != null)
            {
                //If the file belongs to this character, then we pull the files from the Xv2Character object itself. 
                if (chara.ShortName == owner && Utils.CompareCharaFolder(path, chara.ShortName))
                {
                    var file = GetFileFromOwner(path);

                    if (file != null)
                        EmmFile = (EMM_File)file;
                }
                else
                {
                    //This character doesn't own these files so we just load them directly.
                    EmmFile = FileManager.Instance.LoadFile<EMM_File>(path, chara.CharacterData.OnlyLoadFromCPK);
                }

                if(EmmFile != null)
                    EmmFile.MaterialsChanged += RefreshMaterialsOnEdit;
            }
            else
            {
                Log.Add($"EMM not found: {path}.", LogType.Error);
            }
        }

        private void LoadEan()
        {
            string path;
            string declaredPath;
            string owner = IsPhysicsPart ? physicsPart.CharaCode : part.CharaCode;

            if (IsPhysicsPart)
            {
                declaredPath = physicsPart.EanPath;
                path = GetFilePath(physicsPart.GetEanPath());

            }
            else
            {
                declaredPath = part.EanPath;
                path = GetFilePath(part.GetEanPath());
            }

            if (path != null)
            {
                //If the file belongs to this character, then we pull the files from the Xv2Character object itself. 
                if (chara.ShortName == owner && Utils.CompareCharaFolder(path, chara.ShortName))
                {
                    var file = GetFileFromOwner(path);

                    if (file != null)
                        EanFile = (EAN_File)file;
                }
                else
                {
                    //This character doesn't own these files so we just load them directly.
                    EanFile = FileManager.Instance.LoadFile<EAN_File>(path, chara.CharacterData.OnlyLoadFromCPK);
                }
            }

            if (!string.IsNullOrWhiteSpace(declaredPath) && string.IsNullOrWhiteSpace(path))
            {
                Log.Add($"EanPath was declared but no file could be found: {declaredPath}.", LogType.Error);
            }
        }

        private void LoadEsk()
        {
            string path;

            if (IsPhysicsPart)
            {
                path = GetFilePath(physicsPart.GetEskPath());

            }
            else
            {
                Log.Add($"LoadEsk: Only possible on PhysicsParts!", LogType.Error);
                return;
            }

            if (path != null)
            {
                //If the file belongs to this character, then we pull the files from the Xv2Character object itself. 
                if (chara.ShortName == physicsPart.CharaCode && Utils.CompareCharaFolder(path, chara.ShortName))
                {
                    var file = GetFileFromOwner(path);

                    if (file != null)
                        EskFile = (ESK_File)file;
                }
                else
                {
                    //This character doesn't own these files so we just load them directly.
                    EskFile = FileManager.Instance.LoadFile<ESK_File>(path, chara.CharacterData.OnlyLoadFromCPK);
                }
            }
        }

        private string GetFilePath(string path, string pathOnParent = null)
        {
            if (chara.CharacterData.GetPartSetFile(Path.GetFileName(path)) != null)
                return path;

            if (FileExists(path))
                return path;

            if (pathOnParent != null)
            {
                if (FileExists(pathOnParent))
                    return pathOnParent;
            }

            return null;
        }

        private bool FileExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            if (chara.CharacterData.OnlyLoadFromCPK)
                return FileManager.Instance.FileIO.FileExistsInCpk(path);

            return FileManager.Instance.FileIO.FileExists(path);
        }

        private object GetFileFromOwner(string path)
        {
            var file = chara.CharacterData.GetPartSetFile(Path.GetFileName(path));

            if (file == null)
            {
                Log.Add($"\"{path}\" failed to load.", LogType.Error);
                return null;
            }

            return file;
        }

        public EAN_File GetEan()
        {
            return EanFile;
        }
        #endregion

    }
}
