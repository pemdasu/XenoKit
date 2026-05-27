using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using Xv2CoreLib;
using Xv2CoreLib.ACB;
using Xv2CoreLib.EAN;
using xv2 = Xv2CoreLib.Xenoverse2;
using file = Xv2CoreLib.FileManager;
using XenoKit.Engine;
using Xv2CoreLib.EffectContainer;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Engine.Model;
using XenoKit.Editor.Data;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BCM;
using Xv2CoreLib.BSA;
using XenoKit.Engine.Stage;
using Xv2CoreLib.DEM;

namespace XenoKit.Editor
{
    public partial class OutlinerItem : INotifyPropertyChanged
    {
        #region INotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public enum OutlinerItemType
        {
            CaC,
            Character,
            Skill,
            Moveset,
            CMN,
            Stage,
            Inspector,

            //Manual Loads Only:
            ACB,
            EEPK,
            EAN,
            CAM,
            DEM
        }

        public string ID => GetUniqueID();
        public OutlinerItemType Type { get; set; }
        public bool ReadOnly { get; set; }
        public bool IsManualLoaded { get; private set; } = false;
        public bool OnlyLoadFromCPK { get; private set; } = false;
        public bool CanDelete
        {
            get => Type != OutlinerItemType.CMN && Type != OutlinerItemType.Inspector;
        }

        //Data
        public Move move { get; set; }
        public Actor character { get; set; }
        public ManualFiles ManualFiles { get; set; }
        public CustomAvatar CustomAvatar { get; set; }
        public Xv2Stage Stage { get; set; }

        //UI
        public string DisplayName
        {
            get
            {
                if (Type == OutlinerItemType.Inspector) return "";
                if (Type == OutlinerItemType.CMN) return "Common";
                if (IsManualLoaded) return ManualFiles.Name;
                if (Type == OutlinerItemType.CaC) return $"{CustomAvatar.CaC.Name}";
                if (Type == OutlinerItemType.Stage) return Stage.StageName;
                return (Type == OutlinerItemType.Character) ? character.Name : move.Name;
            }
        }
        public string DisplayType
        {
            get
            {
                switch (Type)
                {
                    case OutlinerItemType.Inspector:
                        return "Viewer Mode";
                    default:
                        return Type.ToString().ToUpper();
                }
            }
        }
        public EditorVisibility Visibilities { get; set; }

        //Editor Helpers
        /// <summary>
        /// Can files (vox, ean, cam) be added in the editor?
        /// </summary>
        public bool CanAddFiles { get { return (IsManualLoaded || GetMove().MoveType == Move.Type.CMN || GetMove().MoveType == Move.Type.Moveset) ? false : true; } }
        public bool CanUseSystemTab { get { return (GetMove().MoveType == Move.Type.Skill && !IsManualLoaded); } }

        #region SelectedItems


        #endregion

        /// <summary>
        /// Load a file manually (directly from disk).
        /// </summary>
        /// <param name="path">The path to the file.</param>
        public OutlinerItem(string path)
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".ean":
                    if (path.Contains(".cam"))
                    {
                        Type = OutlinerItemType.CAM;
                        ManualFiles = ManualFiles.LoadCam(path);
                    }
                    else
                    {
                        Type = OutlinerItemType.EAN;
                        ManualFiles = ManualFiles.LoadEan(path);
                    }
                    break;
                case ".acb":
                    Type = OutlinerItemType.ACB;
                    ManualFiles = ManualFiles.LoadAcb(path);
                    break;
                case ".eepk":
                case ".vfxpackage":
                    Type = OutlinerItemType.EEPK;
                    ManualFiles = ManualFiles.LoadEepk(path);
                    break;
                case ".dem":
                    Type = OutlinerItemType.DEM;
                    ManualFiles = ManualFiles.LoadDem(path);
                    break;
                default:
                    throw new InvalidDataException($"OutlinerItem: The filetype of \"{path}\" is unsupported.");
            }

            IsManualLoaded = true;
            SetSelectedItems();
            Visibilities = new EditorVisibility(Type);
        }

        public OutlinerItem(Move move, bool readOnly, OutlinerItemType type, bool onlyLoadFromCpk) : this(readOnly, type, onlyLoadFromCpk)
        {
            this.move = move;
            SetSelectedItems();
        }

        public OutlinerItem(ManualFiles manualFiles, OutlinerItemType type, bool onlyLoadFromCpk)
        {
            Type = type;
            ManualFiles = manualFiles;
            IsManualLoaded = true;
            OnlyLoadFromCPK = onlyLoadFromCpk;
            SetSelectedItems();
            Visibilities = new EditorVisibility(type);
        }

        public OutlinerItem(Actor chara, bool readOnly, OutlinerItemType type) : this(readOnly, type, chara?.CharacterData?.OnlyLoadFromCPK == true)
        {
            character = chara;
            SetSelectedItems();
        }

        public OutlinerItem(bool readOnly, OutlinerItemType type, bool onlyLoadFromCpk)
        {
            Type = type;
            ReadOnly = readOnly;
            OnlyLoadFromCPK = onlyLoadFromCpk;
            Visibilities = new EditorVisibility(type);
        }

        public OutlinerItem(int cacIndex, Xv2CoreLib.SAV.CaC cac)
        {
            CustomAvatar = new CustomAvatar(cacIndex, cac, this);
            Visibilities = new EditorVisibility(OutlinerItemType.CaC);
        }

        public OutlinerItem(Xv2Stage stage)
        {
            Stage = stage;
            Type = OutlinerItemType.Stage;
            Visibilities = new EditorVisibility(OutlinerItemType.Stage);
        }

        /// <summary>
        /// Fix references after pasting from clipboard.
        /// </summary>
        public void FixReferences()
        {
            if (move != null)
            {
                if (move.Files.SeAcbFile != null)
                {
                    foreach (var file in move.Files.SeAcbFile)
                    {
                        file.File.AcbFile.SetCommandTableVersion();
                        file.File = new ACB_Wrapper(file.File.AcbFile);
                    }
                }

                if (move.Files.VoxAcbFile != null)
                {
                    foreach (var file in move.Files.VoxAcbFile)
                    {
                        file.File.AcbFile.SetCommandTableVersion();
                        file.File = new ACB_Wrapper(file.File.AcbFile);
                    }
                }
            }
        }

        public Move GetMove()
        {
            if (IsManualLoaded) return ManualFiles.Move;

            return Type == OutlinerItemType.Character ? character.Moveset : move;
        }

        public bool SaveValidate(MetroWindow window)
        {
            Move move = GetMove();

            if(move != null)
                if (!move.SaveValidate(window)) return false;

            return true;
        }

        /// <summary>
        /// Call after loading - will set default selected items.
        /// </summary>
        private void SetSelectedItems()
        {
            SelectedBacFile = (GetMove().Files.BacFiles.Count > 0) ? GetMove().Files.BacFiles[0] : null;
            SelectedBsaFile = GetMove().Files.BsaFile;
            SelectedBcmFile = GetMove().Files.BcmFile ?? GetMove().Files.AfterBcmFile;
            SelectedEanFile = (GetMove().Files.EanFile.Count > 0) ? GetMove().Files.EanFile[0] : null;
            SelectedCamFile = (GetMove().Files.CamEanFile.Count > 0) ? GetMove().Files.CamEanFile[0] : null;
            SelectedSeAcbFile = (GetMove().Files.SeAcbFile.Count > 0) ? GetMove().Files.SeAcbFile[0] : null;
            SelectedVoxAcbFile = (GetMove().Files.VoxAcbFile.Count > 0) ? GetMove().Files.VoxAcbFile[0] : null;
            SelectedEepk = Type == OutlinerItemType.CMN ? GetMove().Files.EepkFile : null;
        }

        public xv2.MoveType GetMoveType()
        {
            switch (Type)
            {
                case OutlinerItemType.Character:
                case OutlinerItemType.Moveset:
                    return xv2.MoveType.Character;
                case OutlinerItemType.Skill:
                    return xv2.MoveType.Character;
                case OutlinerItemType.CMN:
                    return xv2.MoveType.Common;
                default:
                    return 0;
            }
        }

        private string GetUniqueID()
        {
            if (IsManualLoaded) return null;

            switch (Type)
            {
                case OutlinerItemType.Moveset:
                    return $"CHARA_{move.CmsEntry.ShortName}";
                case OutlinerItemType.Character:
                    return $"CHARA_{character.ShortName}";
                case OutlinerItemType.Skill:
                    return $"SKILL_{move.CusEntry.ID1}";
                case OutlinerItemType.CaC:
                    return $"CAC_{CustomAvatar.CaC.Name}";
                case OutlinerItemType.Stage:
                    return $"STAGE_{Stage.StageDefEntry.CODE}_{Stage.StageDefEntry.Index}";
            }

            return null;
        }

        public void Update()
        {
            if(Type == OutlinerItemType.CaC)
            {
                CustomAvatar.Update();
            }
        }

        #region Save Context File
        //The Save Context feature will save whatever the currently active file is (e.g: on Anim tab, this would be the selected EAN file)
        //This is an alternative way to save files without having to save the entire item (character/skill/cmn), which can take some time depending on how big it is.

        /// <summary>
        /// Gets the file name (with extension) of the current context file (file on current tab). If there is no context, or the current tab/file is not supported, then null will be returned.
        /// </summary>
        public string GetSaveContextFileName()
        {
            switch (SceneManager.CurrentSceneState)
            {
                case EditorTabs.Animation:
                    return SelectedEanFile != null ? Path.GetFileName(SelectedEanFile.Path) : null;
                case EditorTabs.Camera:
                    return SelectedCamFile != null ? Path.GetFileName(SelectedCamFile.Path) : null;
                case EditorTabs.Effect:
                case EditorTabs.Effect_LIGHT:
                case EditorTabs.Effect_CBIND:
                case EditorTabs.Effect_TBIND:
                case EditorTabs.Effect_PBIND:
                case EditorTabs.Effect_EMO:
                    return SelectedEepk != null ? Path.GetFileName(SelectedEepk.Path) : null;
                case EditorTabs.Audio_SE:
                    return SelectedSeAcbFile != null ? Path.GetFileName(SelectedSeAcbFile.Path) : null;
                case EditorTabs.Audio_VOX:
                    return SelectedVoxAcbFile != null ? Path.GetFileName(SelectedVoxAcbFile.Path) : null;
                case EditorTabs.Action:
                    return SelectedBacFile != null ? Path.GetFileName(SelectedBacFile.Path) : null;
                case EditorTabs.Projectile:
                    return SelectedBsaFile != null ? Path.GetFileName(SelectedBsaFile.Path) : null;
                case EditorTabs.State:
                    return SelectedBcmFile != null ? Path.GetFileName(SelectedBcmFile.Path) : null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Saves the current context file (file on current tab). If there is no context or the editor is on a tab/file that is unsupported by this method, then nothing will be saved. For determining if the current context can be saved, see <see cref="GetSaveContextFileName"/>.
        /// </summary>
        /// <returns>A bool indicating if the save was successful.</returns>
        public bool SaveContextFile()
        {
            string pathSaved = null;

            switch (SceneManager.CurrentSceneState)
            {
                case EditorTabs.Animation:
                    if(SelectedEanFile.Path != null)
                    {
                        SelectedEanFile.File.Save(SelectedEanFile.Path);
                        pathSaved = SelectedEanFile.Path;
                    }
                    break;
                case EditorTabs.Camera:
                    if (SelectedCamFile.Path != null)
                    {
                        SelectedCamFile.File.Save(SelectedCamFile.Path);
                        pathSaved = SelectedCamFile.Path;
                    }
                    break;
                case EditorTabs.Effect:
                case EditorTabs.Effect_LIGHT:
                case EditorTabs.Effect_CBIND:
                case EditorTabs.Effect_TBIND:
                case EditorTabs.Effect_PBIND:
                case EditorTabs.Effect_EMO:
                    if (SelectedEepk.Path != null)
                    {
                        SelectedEepk.File.ChangeFilePath(SelectedEepk.Path);
                        SelectedEepk.File.Save();
                        CustomEntryNames.SaveNames(SelectedEepk.RelativePath, SelectedEepk.File);
                        pathSaved = SelectedEepk.Path;
                    }
                    break;
                case EditorTabs.Audio_SE:
                    if (SelectedSeAcbFile.Path != null)
                    {
                        SelectedSeAcbFile.File.AcbFile.Save(SelectedSeAcbFile.Path);
                        pathSaved = SelectedSeAcbFile.Path;
                    }
                    break;
                case EditorTabs.Audio_VOX:
                    if (SelectedVoxAcbFile.Path != null)
                    {
                        SelectedVoxAcbFile.File.AcbFile.Save(SelectedVoxAcbFile.Path);
                        pathSaved = SelectedVoxAcbFile.Path;
                    }
                    break;
                case EditorTabs.Action:
                    if (SelectedBacFile.Path != null)
                    {
                        SelectedBacFile.File.SaveIBacTypes();
                        SelectedBacFile.File.Save(SelectedBacFile.Path);
                        CustomEntryNames.SaveNames(SelectedBacFile.RelativePath, SelectedBacFile.File);
                        pathSaved = SelectedBacFile.Path;
                    }
                    break;
                case EditorTabs.Projectile:
                    if (SelectedBsaFile?.Path != null)
                    {
                        SelectedBsaFile.File.SaveIBsaTypes();
                        SelectedBsaFile.File.Save(SelectedBsaFile.Path);
                        CustomEntryNames.SaveNames(SelectedBsaFile.RelativePath, SelectedBsaFile.File);
                        pathSaved = SelectedBsaFile.Path;
                    }
                    break;
                case EditorTabs.State:
                    if (SelectedBcmFile?.Path != null)
                    {
                        SelectedBcmFile.File.Save(SelectedBcmFile.Path);
                        pathSaved = SelectedBcmFile.Path;
                    }
                    break;
                default:
                    return false;
            }

            if(pathSaved != null)
            {
                Log.Add($"\"{pathSaved}\" saved!", LogType.Info);
                return true;
            }
            else
            {
                Log.Add($"Unable to save item as it has no path! This is likely because the file was not originally loaded with this item, and was generated by XenoKit. A full save is required in this case.", LogType.Warning);
            }

            return false;
        }
        #endregion
    }

    public class ManualFiles
    {
        public string Name;

        //Just used for manual loaded BCS files right now
        public Xv2Character CharacterFiles { get; set; }

        //All moveset/skill related files:
        public Move Move { get; set; }
        public DEM_File DemFile { get; set; }
        public string DemPath { get; set; }
        private string DemLooseRoot { get; set; }
        private bool DemOnlyLoadFromCpk { get; set; }


        private ManualFiles(Xv2Character chara, string name)
        {
            Name = name;
            CharacterFiles = chara;
        }

        private ManualFiles(Xv2MoveFiles move, string name)
        {
            Name = name;
            Move = new Move(move);
        }

        #region Load
        public static ManualFiles LoadEan(string path)
        {
            Xv2File<EAN_File> file = new Xv2File<EAN_File>(EAN_File.Load(path), path, true);
            Xv2MoveFiles move = new Xv2MoveFiles();
            move.EanFile.Add(file);

            return new ManualFiles(move, Path.GetFileName(path));
        }

        public static ManualFiles LoadCam(string path)
        {
            Xv2File<EAN_File> file = new Xv2File<EAN_File>(EAN_File.Load(path), path, true);
            Xv2MoveFiles move = new Xv2MoveFiles();
            move.CamEanFile.Add(file);

            return new ManualFiles(move, Path.GetFileName(path));
        }

        public static ManualFiles LoadEepk(string path)
        {
            EffectContainerFile eepk = Path.GetExtension(path) == EffectContainerFile.ZipExtension ? EffectContainerFile.LoadVfxPackage(path) : EffectContainerFile.Load(path);

            Xv2File <EffectContainerFile> file = new Xv2File<EffectContainerFile>(eepk, path, true);
            Xv2MoveFiles move = new Xv2MoveFiles();
            move.EepkFile = file;

            return new ManualFiles(move, Path.GetFileName(path));
        }

        public static ManualFiles LoadAcb(string path)
        {
            Xv2File<ACB_Wrapper> file = new Xv2File<ACB_Wrapper>(new ACB_Wrapper(ACB_File.Load(path)), path, true);
            Xv2MoveFiles move = new Xv2MoveFiles();
            move.SeAcbFile.Add(file);

            return new ManualFiles(move, Path.GetFileName(path));
        }

        public static ManualFiles LoadDem(string path)
        {
            DEM_File demFile = new Xv2CoreLib.DEM.Parser(path, false).demFile;
            string looseRoot = GetLooseRoot(path);

            return LoadDemFiles(demFile, Path.GetFileName(path), path, looseRoot, false);
        }

        public static ManualFiles LoadDemFromGame(string relativePath, string name, bool onlyLoadFromCpk)
        {
            DEM_File demFile = file.Instance.GetParsedFileFromGame(relativePath, onlyLoadFromCpk, true, true) as DEM_File;

            if (demFile == null)
            {
                throw new InvalidDataException($"DEM load: could not parse \"{relativePath}\".");
            }

            return LoadDemFiles(demFile, name, file.Instance.GetAbsolutePath(relativePath), null, onlyLoadFromCpk);
        }

        private static ManualFiles LoadDemFiles(DEM_File demFile, string name, string demPath, string looseRoot, bool onlyLoadFromCpk)
        {
            Xv2MoveFiles move = new Xv2MoveFiles();

            if (!IsEmptyReference(demFile.Settings?.Str_00))
            {
                Xv2File<EAN_File> cameraFile = LoadEanReference(demFile.Settings.Str_00, looseRoot, "CAM", true, onlyLoadFromCpk);

                if (cameraFile != null)
                {
                    move.CamEanFile.Add(cameraFile);
                }
            }

            if (demFile.Settings?.Characters != null)
            {
                foreach (Xv2CoreLib.DEM.Character actor in demFile.Settings.Characters)
                {
                    if (IsEmptyReference(actor.Str_16))
                    {
                        continue;
                    }

                    Xv2File<EAN_File> eanFile = LoadEanReference(actor.Str_16, looseRoot, actor.Str_00, false, onlyLoadFromCpk);

                    if (eanFile != null)
                    {
                        move.EanFile.Add(eanFile);
                    }
                }
            }

            string effectReference = GetDemEffectReference(demFile, demPath);

            if (!IsEmptyReference(effectReference))
            {
                Xv2File<EffectContainerFile> eepkFile = LoadEepkReference(effectReference, looseRoot, onlyLoadFromCpk, HasDemEffects(demFile));

                if (eepkFile != null)
                {
                    move.EepkFile = eepkFile;
                }
            }

            ManualFiles files = new ManualFiles(move, name);
            files.DemFile = demFile;
            files.DemPath = demPath;
            files.DemLooseRoot = looseRoot;
            files.DemOnlyLoadFromCpk = onlyLoadFromCpk;
            Log.Add($"Loaded DEM \"{name}\" with {move.EanFile.Count} animation file(s), {move.CamEanFile.Count} camera file(s), and {move.EepkFiles.Count} effect file(s).");
            return files;
        }

        public Xv2File<EffectContainerFile> GetOrLoadDemEffectEepk()
        {
            if (Move?.Files?.EepkFile != null)
            {
                return Move.Files.EepkFile;
            }

            string effectReference = GetDemEffectReference(DemFile, DemPath);

            if (IsEmptyReference(effectReference))
            {
                return null;
            }

            Xv2File<EffectContainerFile> eepkFile = LoadEepkReference(effectReference, DemLooseRoot, DemOnlyLoadFromCpk, HasDemEffects(DemFile));

            if (eepkFile != null)
            {
                Move.Files.EepkFile = eepkFile;
            }

            return eepkFile;
        }

        public Xv2File<EAN_File> GetOrLoadDemActorEan(int actorIndex)
        {
            Xv2CoreLib.DEM.Character actor = DemFile?.Settings?.Characters?.ElementAtOrDefault(actorIndex);

            if (actor == null || IsEmptyReference(actor.Str_16))
            {
                return null;
            }

            string relativePath = GetReferencePath(actor.Str_16, ".ean").Replace('/', Path.DirectorySeparatorChar);
            Xv2File<EAN_File> eanFile = Move?.Files?.EanFile?.FirstOrDefault(file =>
                file.CharaCode == actor.Str_00 &&
                file.Path?.Replace('/', Path.DirectorySeparatorChar).EndsWith(relativePath, StringComparison.OrdinalIgnoreCase) == true);

            if (eanFile != null)
            {
                return eanFile;
            }

            eanFile = LoadEanReference(actor.Str_16, DemLooseRoot, actor.Str_00, false, DemOnlyLoadFromCpk);

            if (eanFile != null)
            {
                Move.Files.EanFile.Add(eanFile);
            }

            return eanFile;
        }

        private static Xv2File<EAN_File> LoadEanReference(string reference, string looseRoot, string charaCode, bool isCamera, bool onlyLoadFromCpk)
        {
            string relativePath = GetReferencePath(reference, ".ean");
            string loosePath = looseRoot != null ? GetLoosePath(looseRoot, relativePath) : null;
            EAN_File eanFile = null;
            string filePath = null;

            if (loosePath != null && File.Exists(loosePath))
            {
                eanFile = EAN_File.Load(loosePath, true);
                filePath = loosePath;
            }
            else
            {
                eanFile = file.Instance.GetParsedFileFromGame(relativePath, onlyLoadFromCpk, false) as EAN_File;
                filePath = file.Instance.GetAbsolutePath(relativePath);
            }

            if (eanFile == null)
            {
                Log.Add($"DEM load: could not find referenced EAN \"{relativePath}\".", LogType.Warning);
                return null;
            }

            return new Xv2File<EAN_File>(eanFile, filePath, true, charaCode, false, isCamera ? xv2.MoveFileTypes.CAM_EAN : xv2.MoveFileTypes.EAN, 0, false, xv2.MoveType.Skill);
        }

        private static Xv2File<EffectContainerFile> LoadEepkReference(string reference, string looseRoot, bool onlyLoadFromCpk, bool logMissingFile = true)
        {
            string fileName = Path.GetFileNameWithoutExtension(reference);
            string relativePath = reference.Contains("/") || reference.Contains("\\")
                ? GetReferencePath(reference, ".eepk")
                : GetReferencePath($"vfx/demo/{fileName}/{fileName}", ".eepk");
            string loosePath = looseRoot != null ? GetLoosePath(looseRoot, relativePath) : null;
            EffectContainerFile eepkFile = null;
            string filePath = null;

            if (loosePath != null && File.Exists(loosePath))
            {
                try
                {
                    eepkFile = EffectContainerFile.Load(loosePath);
                    filePath = loosePath;
                }
                catch (Exception ex)
                {
                    Log.Add($"DEM load: could not load referenced EEPK \"{loosePath}\". {ex.Message}", LogType.Warning);
                    return null;
                }
            }
            else
            {
                try
                {
                    eepkFile = file.Instance.GetParsedFileFromGame(relativePath, onlyLoadFromCpk, false) as EffectContainerFile;
                    filePath = file.Instance.GetAbsolutePath(relativePath);
                }
                catch (Exception ex)
                {
                    Log.Add($"DEM load: could not load referenced EEPK \"{relativePath}\". {ex.Message}", LogType.Warning);
                    return null;
                }
            }

            if (eepkFile == null)
            {
                if (logMissingFile)
                {
                    Log.Add($"DEM load: could not find referenced EEPK \"{relativePath}\".", LogType.Warning);
                }

                return null;
            }

            return new Xv2File<EffectContainerFile>(eepkFile, filePath, true, null, false, xv2.MoveFileTypes.EEPK, 0, false, xv2.MoveType.Skill);
        }

        private static string GetDemEffectReference(DEM_File demFile, string demPath)
        {
            if (!IsEmptyReference(demFile?.Settings?.Str_48))
            {
                return demFile.Settings.Str_48;
            }

            return Path.GetFileNameWithoutExtension(demPath);
        }

        private static bool HasDemEffects(DEM_File demFile)
        {
            return demFile?.Section2Entries?.Any(cut =>
                cut?.SubEntries?.Any(demEvent => demEvent?.I_04 == DEM_Type.DemoDataTypes.Effect) == true) == true;
        }

        private static string GetReferencePath(string reference, string extension)
        {
            string path = reference.Replace('\\', '/');
            return path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? path : $"{path}{extension}";
        }

        private static string GetLoosePath(string looseRoot, string relativePath)
        {
            return Path.Combine(looseRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string GetLooseRoot(string path)
        {
            DirectoryInfo directory = new DirectoryInfo(Path.GetDirectoryName(path));

            while (directory != null)
            {
                if (directory.Name.Equals("demo", StringComparison.OrdinalIgnoreCase))
                {
                    return directory.Parent?.FullName ?? Path.GetDirectoryName(path);
                }

                directory = directory.Parent;
            }

            return Path.GetDirectoryName(path);
        }

        private static bool IsEmptyReference(string reference)
        {
            return string.IsNullOrWhiteSpace(reference) ||
                   reference.Equals("NULL", StringComparison.OrdinalIgnoreCase) ||
                   reference.Equals("-noload", StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region Save
        public void Save()
        {
            if (DemFile != null)
            {
                if (DemPath == null)
                {
                    Log.Add($"Unable to save DEM \"{Name}\" because it has no file path.", LogType.Warning);
                    return;
                }

                DemFile.Save(DemPath);
                Log.Add($"\"{DemPath}\" saved!", LogType.Info);
            }

            if(Move?.Files?.EanFile?.Count > 0)
            {
                foreach (var file in Move.Files.EanFile)
                    file.File.Save(file.Path);
            }

            if (Move?.Files?.CamEanFile?.Count > 0)
            {
                foreach (var file in Move.Files.CamEanFile)
                    file.File.Save(file.Path);
            }

            if (Move?.Files?.SeAcbFile?.Count > 0)
            {
                foreach (var file in Move.Files.SeAcbFile)
                    file.File.AcbFile.Save(file.Path);
            }

            if(Move?.Files?.EepkFile != null)
            {
                Move.Files.EepkFile.File.Save();
            }
        }

        #endregion

        /*
        public static ManualFiles LoadBcs(string path)
        {
            //need to modify partset loading to load from a specific folder before the game
            Xv2File<BCS_File> bcsFile = new Xv2File<BCS_File>(BCS_File.Load(path), path, true);
            Xv2Character chara = new Xv2Character();
            chara.BcsFile = bcsFile;
        }
        */
    }
}
