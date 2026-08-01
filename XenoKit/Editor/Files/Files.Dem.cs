using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows.Media;
using XenoKit.Engine;
using XenoKit.Engine.Character;
using XenoKit.Engine.Stage;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.BAI;
using Xv2CoreLib.BCS;
using Xv2CoreLib.DEM;
using Xv2CoreLib.DML;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.ESK;
using Xv2CoreLib.MSG;
using Xv2CoreLib.Resource.App;
using xv2 = Xv2CoreLib.Xenoverse2;
using file = Xv2CoreLib.FileManager;

namespace XenoKit.Editor
{
    public partial class Files
    {
        private const string DemoFolderPath = "demo";
        private const string DemoMovieListPath = "system/demo_movie_list.dml";
        private const string DemoNameMsgPath = "msg/menu_quest_reception_en.msg";
        private const string DemoPreviewEmbPath = "ui/texture/DEMO.emb";
        private const string DemoXv1PreviewEmbPath = "ui/texture/DEMO_XV1.emb";
        private readonly Dictionary<DEM_File, Dictionary<string, Xv2Stage>> demStageCache = new Dictionary<DEM_File, Dictionary<string, Xv2Stage>>();
        private DEM_File activeDemStageFile;
        private int activeDemStageIndex = -1;
        private string activeDemStageCode;

        public async void AsyncLoadDemo()
        {
            List<DemoItem> demos = GetDemoList();
            EntitySelector selector = new EntitySelector(demos, "Demo");
            selector.ShowImageColumn("Preview");
            selector.SetBooleanParameter("Only Load From CPK", "Ignore loose files and load directly from CPK.");
            selector.ShowDialog();

            if (selector.SelectedItem is DemoItem demo)
            {
                await AsyncLoadDemo(demo, selector.BooleanParameter);
            }
        }

        public async Task AsyncLoadDemo(DemoItem demo, bool onlyLoadFromCpk, int replaceItemIndex = -1)
        {
            string message = $"Loading demo \"{demo.Name}\"";
            ProgressDialogController progressBarController = await window.ShowProgressAsync("Loading", message, false, DialogSettings.Default);
            progressBarController.SetIndeterminate();

            try
            {
                await Task.Run(async () =>
                {
                    if (GetCmnMove() == null)
                    {
                        await AsyncLoadCmnFiles(progressBarController);
                        progressBarController.SetMessage(message);
                    }

                    ManualFiles manualFiles = ManualFiles.LoadDemFromGame(demo.RelativePath, demo.Name, onlyLoadFromCpk);
                    AddOutlinerItem(new OutlinerItem(manualFiles, OutlinerItem.OutlinerItemType.DEM, onlyLoadFromCpk), replaceItemIndex);
                });
            }
            catch (Exception ex)
            {
                Log.Add($"Load Error: {ex.Message}", LogType.Error);
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            }
            finally
            {
                await progressBarController.CloseAsync();
            }
        }

        private List<DemoItem> GetDemoList()
        {
            DML_File demoList = file.Instance.GetParsedFileFromGame(DemoMovieListPath, false, true) as DML_File;
            MSG_File names = file.Instance.GetParsedFileFromGame(DemoNameMsgPath, false, true) as MSG_File;

            if (demoList == null)
            {
                throw new InvalidDataException($"Demo list \"{DemoMovieListPath}\" could not be loaded.");
            }

            if (names == null)
            {
                throw new InvalidDataException($"Demo name file \"{DemoNameMsgPath}\" could not be loaded.");
            }

            Dictionary<string, EmbEntry> previewEntries = GetDemoPreviewEntries();

            List<DemoItem> demos = demoList.DML_Entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.DemoID) && !string.IsNullOrWhiteSpace(entry.DemoNameMsgID))
                .Select(entry =>
                {
                    string name = names.GetEntryText(entry.DemoNameMsgID);
                    string displayName = string.IsNullOrWhiteSpace(name) ? $"{entry.DemoID} ({entry.DemoNameMsgID})" : name;
                    string relativePath = $"demo/{entry.DemoID}/{entry.DemoID}.dem";
                    previewEntries.TryGetValue(entry.DemoID, out EmbEntry previewEntry);

                    return new DemoItem(entry, entry.DemoID, displayName, relativePath, previewEntry);
                })
                .ToList();

            AddUnlistedDemos(demos, previewEntries);

            return demos.OrderBy(demo => demo.ID).ThenBy(demo => demo.DemoID, StringComparer.OrdinalIgnoreCase).ToList();
        }

        //demo_movie_list only covers a fraction of the .dem files the game ships, so pick up everything else under the demo folder.
        private void AddUnlistedDemos(List<DemoItem> demos, Dictionary<string, EmbEntry> previewEntries)
        {
            HashSet<string> knownPaths = new HashSet<string>(demos.Select(demo => demo.RelativePath), StringComparer.OrdinalIgnoreCase);

            foreach (string demPath in file.Instance.fileIO.GetFilesInDirectory(DemoFolderPath, ".dem", true))
            {
                string relativePath = Utils.SanitizePath(demPath);

                if (knownPaths.Contains(relativePath)) continue;

                knownPaths.Add(relativePath);

                string demoId = Path.GetFileNameWithoutExtension(relativePath);
                previewEntries.TryGetValue(demoId, out EmbEntry previewEntry);

                demos.Add(new DemoItem(null, demoId, demoId, relativePath, previewEntry));
            }
        }

        private Dictionary<string, EmbEntry> GetDemoPreviewEntries()
        {
            Dictionary<string, EmbEntry> previewEntries = new Dictionary<string, EmbEntry>(StringComparer.OrdinalIgnoreCase);
            AddDemoPreviewEntries(previewEntries, DemoPreviewEmbPath);
            AddDemoPreviewEntries(previewEntries, DemoXv1PreviewEmbPath);
            return previewEntries;
        }

        private void AddDemoPreviewEntries(Dictionary<string, EmbEntry> previewEntries, string path)
        {
            EMB_File embFile = file.Instance.GetParsedFileFromGame(path, false, true) as EMB_File;

            if (embFile == null)
            {
                throw new InvalidDataException($"Demo preview file \"{path}\" could not be loaded.");
            }

            foreach (EmbEntry entry in embFile.Entry)
            {
                string id = Path.GetFileNameWithoutExtension(entry.Name);

                if (!string.IsNullOrWhiteSpace(id) && !previewEntries.ContainsKey(id))
                {
                    previewEntries.Add(id, entry);
                }
            }
        }

        public Actor LoadPreviewCharacter(int id, int partSetId, bool onlyLoadFromCpk = false)
        {
            Xv2Character xv2Character = LoadPreviewCharacterData(id, onlyLoadFromCpk);
            int resolvedPartSetId = ResolvePreviewPartSetId(xv2Character, id, partSetId);

            if (resolvedPartSetId == -1)
            {
                throw new InvalidOperationException($"partset {partSetId} does not exist for character {id}. Available partsets: {GetPreviewPartSetIds(xv2Character)}.");
            }

            Actor chara = new Actor(xv2Character, resolvedPartSetId);

            VerifyValues(chara.Moveset.Files);

            return chara;
        }

        private static int ResolvePreviewPartSetId(Xv2Character character, int characterId, int requestedPartSetId)
        {
            if (character.BcsFile?.File?.PartSets == null)
            {
                return -1;
            }

            if (character.BcsFile.File.PartSets.Any(partSet => partSet.ID == requestedPartSetId))
            {
                return requestedPartSetId;
            }

            if (requestedPartSetId >= 0 && requestedPartSetId < character.BcsFile.File.PartSets.Count)
            {
                int resolvedPartSetId = character.BcsFile.File.PartSets[requestedPartSetId].ID;
                Log.Add($"Preview character load: mapped partset index {requestedPartSetId} to BCS partset ID {resolvedPartSetId} for character {characterId}.", LogType.Info);
                return resolvedPartSetId;
            }

            return -1;
        }

        private static string GetPreviewPartSetIds(Xv2Character character)
        {
            if (character.BcsFile?.File?.PartSets == null || character.BcsFile.File.PartSets.Count == 0)
            {
                return "none";
            }

            return string.Join(", ", character.BcsFile.File.PartSets.Select(partSet => partSet.ID));
        }

        private Xv2Character LoadPreviewCharacterData(int id, bool onlyLoadFromCpk)
        {
            try
            {
                return xv2.Instance.GetCharacter(id, true, onlyLoadFromCpk);
            }
            catch (FileNotFoundException ex)
            {
                Log.Add($"Preview character load: ignored missing move file. {ex.Message}", LogType.Warning);
                Xv2Character xv2Character = xv2.Instance.GetCharacter(id, false, onlyLoadFromCpk);
                LoadPreviewCharacterBaseFiles(xv2Character, onlyLoadFromCpk);
                xv2Character.CreateDefaultFiles();
                return xv2Character;
            }
        }

        private static void LoadPreviewCharacterBaseFiles(Xv2Character character, bool onlyLoadFromCpk)
        {
            if (character?.CmsEntry == null)
            {
                throw new InvalidOperationException("Preview character load: CMS entry was missing.");
            }

            string bcsPath = Xv2CoreLib.Utils.ResolveRelativePath($"chara/{character.CmsEntry.ShortName}/{character.CmsEntry.BcsPath}.bcs");
            string baiPath = Xv2CoreLib.Utils.ResolveRelativePath($"chara/{character.CmsEntry.ShortName}/{character.CmsEntry.BaiPath}.bai");
            string eskPath = Xv2CoreLib.Utils.ResolveRelativePath($"chara/{character.CmsEntry.ShortName}/{character.CmsEntry.BcsPath}_000.esk");

            BCS_File bcsFile = file.Instance.GetParsedFileFromGame(bcsPath, onlyLoadFromCpk) as BCS_File;
            BAI_File baiFile = file.Instance.GetParsedFileFromGame(baiPath, onlyLoadFromCpk, false) as BAI_File;
            ESK_File eskFile = file.Instance.GetParsedFileFromGame(eskPath, onlyLoadFromCpk) as ESK_File;

            character.BcsFile = new Xv2File<BCS_File>(bcsFile, file.Instance.GetAbsolutePath(bcsPath), false);
            character.BaiFile = new Xv2File<BAI_File>(baiFile ?? new BAI_File(), file.Instance.GetAbsolutePath(baiPath), !character.CmsEntry.IsSelfReference(character.CmsEntry.BaiPath));
            character.EskFile = new Xv2File<ESK_File>(eskFile, file.Instance.GetAbsolutePath(eskPath), false);
            character.LoadPartSets();
        }

        public void SetActiveDemStage(DEM_File demFile, int stageIndex, bool forceSet = false)
        {
            if (demFile?.Settings == null)
            {
                return;
            }

            string stageCode = GetDemStageCode(demFile, stageIndex);

            if (IsEmptyDemoReference(stageCode))
            {
                if (stageIndex == 0 && TryGetFirstDemStage(demFile, out int firstStageIndex, out string firstStageCode))
                {
                    stageIndex = firstStageIndex;
                    stageCode = firstStageCode;
                }
                else
                {
                    Log.Add($"DEM preview: stage index {stageIndex} is empty.", LogType.Warning);
                    return;
                }
            }

            stageCode = stageCode?.Trim();

            if (!forceSet && activeDemStageFile == demFile && activeDemStageIndex == stageIndex && string.Equals(activeDemStageCode, stageCode, StringComparison.OrdinalIgnoreCase))
            {
                Xv2Stage activeStage = GetCachedDemStage(demFile, stageCode);

                if (activeStage != null && Viewport.Instance?.CurrentStage == activeStage)
                {
                    return;
                }
            }

            Xv2Stage stage = GetOrLoadDemStage(demFile, stageCode);

            if (stage != null)
            {
                SetActiveStage(stage);
                activeDemStageFile = demFile;
                activeDemStageIndex = stageIndex;
                activeDemStageCode = stageCode;
            }
            else
            {
                Log.Add($"DEM preview: failed to load stage \"{stageCode}\".", LogType.Warning);
            }
        }

        private bool TryGetFirstDemStage(DEM_File demFile, out int stageIndex, out string stageCode)
        {
            string[] stageCodes = GetDemStageCodesArray(demFile);

            for (int i = 0; i < stageCodes.Length; i++)
            {
                if (!IsEmptyDemoReference(stageCodes[i]))
                {
                    stageIndex = i;
                    stageCode = stageCodes[i];
                    return true;
                }
            }

            stageIndex = -1;
            stageCode = null;
            return false;
        }

        private Xv2Stage GetCachedDemStage(DEM_File demFile, string stageCode)
        {
            if (demFile == null || stageCode == null)
            {
                return null;
            }

            if (demStageCache.TryGetValue(demFile, out Dictionary<string, Xv2Stage> stages) && stages.TryGetValue(stageCode, out Xv2Stage stage))
            {
                return stage;
            }

            return null;
        }

        private Xv2Stage GetOrLoadDemStage(DEM_File demFile, string stageCode)
        {
            Xv2Stage cachedStage = GetCachedDemStage(demFile, stageCode);

            if (cachedStage != null)
            {
                return cachedStage;
            }

            Xv2Stage stage = LoadDemStage(stageCode);

            if (stage == null || demFile == null)
            {
                return stage;
            }

            if (!demStageCache.TryGetValue(demFile, out Dictionary<string, Xv2Stage> stages))
            {
                stages = new Dictionary<string, Xv2Stage>(StringComparer.OrdinalIgnoreCase);
                demStageCache[demFile] = stages;
            }

            stages[stageCode] = stage;
            return stage;
        }

        private Xv2Stage LoadDemStage(string stageCode)
        {
            if (xv2.Instance.StageDefFile.GetStage(stageCode) == null)
            {
                Log.Add($"DEM load: could not find referenced stage \"{stageCode}\".", LogType.Warning);
                return null;
            }

            Xv2Stage stage = new Xv2Stage(stageCode);

            if (stage.FmpFile == null || stage.SpmFile == null)
            {
                return null;
            }

            return stage;
        }

        private string GetDemStageCode(DEM_File demFile, int stageIndex)
        {
            string[] stageCodes = GetDemStageCodesArray(demFile);

            if (stageIndex < 0 || stageIndex >= stageCodes.Length)
            {
                Log.Add($"DEM preview: stage index {stageIndex} is outside the DEM stage list.", LogType.Warning);
                return null;
            }

            return stageCodes[stageIndex];
        }

        private string[] GetDemStageCodesArray(DEM_File demFile)
        {
            if (demFile?.Settings == null)
            {
                return new string[0];
            }

            return new[]
            {
                demFile.Settings.Str_08,
                demFile.Settings.Str_72,
                demFile.Settings.Str_80,
                demFile.Settings.Str_88,
                demFile.Settings.Str_96
            };
        }

        private void SetActiveStage(Xv2Stage stage)
        {
            if (stage != null)
            {
                window.Dispatcher.Invoke(() => Viewport.Instance?.SetActiveStage(stage));
            }
        }

        private static bool IsEmptyDemoReference(string reference)
        {
            return string.IsNullOrWhiteSpace(reference) ||
                   reference.Equals("NULL", StringComparison.OrdinalIgnoreCase) ||
                   reference.Equals("-noload", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class DemoItem : Xv2Item
    {
        private readonly EmbEntry previewEntry;

        //Null for demos found by scanning the demo folder, which have no demo_movie_list entry.
        public DML_Entry DmlEntry { get; }
        public string DemoID { get; }
        public string RelativePath { get; }
        public ImageSource PreviewImage => previewEntry?.Texture;
        public override string DisplayID => DemoID;

        public DemoItem(DML_Entry dmlEntry, string demoId, string name, string relativePath, EmbEntry previewEntry) : base(GetId(dmlEntry, demoId), name)
        {
            DmlEntry = dmlEntry;
            DemoID = demoId;
            RelativePath = relativePath;
            this.previewEntry = previewEntry;
        }

        private static int GetId(DML_Entry dmlEntry, string demoId)
        {
            if (dmlEntry != null && int.TryParse(dmlEntry.Index, out int index)) return index;

            //Folder-scanned demos sort by the digits in their code, e.g. DEM0042 -> 42.
            string digits = new string(demoId.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int id) ? id : -1;
        }
    }
}
