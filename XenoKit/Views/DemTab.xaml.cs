using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Engine.Animation;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.CUS;
using Xv2CoreLib.DEM;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource.App;
using YAXLib;
using file = Xv2CoreLib.FileManager;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdQuaternion = System.Numerics.Quaternion;
using SimdVector3 = System.Numerics.Vector3;
using xv2 = Xv2CoreLib.Xenoverse2;

namespace XenoKit.Controls
{
    public partial class DemTab : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Files files = Files.Instance;
        private readonly Dictionary<int, Actor> previewActors = new Dictionary<int, Actor>();
        private readonly Dictionary<int, string> previewActorKeys = new Dictionary<int, string>();
        private readonly Dictionary<string, EffectContainerFile> previewEffectFiles = new Dictionary<string, EffectContainerFile>();
        private readonly HashSet<string> previewWarnings = new HashSet<string>();
        private bool previewTimerRunning;
        private const float DemPreviewNearClip = 0.01f;
        private DEM_File currentDemFile;
        private DemCutRow selectedCut;
        private DemEventRow selectedEvent;
        private List<DemPlaybackEvent> previewEvents = new List<DemPlaybackEvent>();
        private List<DemPlaybackEvent> previewLightEvents = new List<DemPlaybackEvent>();
        private readonly Dictionary<int, DemPlaybackEvent> activeActorLightEvents = new Dictionary<int, DemPlaybackEvent>();
        private int previewEventIndex;
        private int previewStartFrame;
        private int previewEndFrame;
        private int previewCurrentFrame;
        private bool suppressEventPreview;
        private bool suppressEffectPreview;

        public static DemTab ActiveDemTab { get; private set; }

        public ObservableCollection<DemCutRow> Cuts { get; } = new ObservableCollection<DemCutRow>();
        public ObservableCollection<DemEventRow> Events { get; } = new ObservableCollection<DemEventRow>();
        public ObservableCollection<DemPropertyRow> SelectedEventProperties { get; } = new ObservableCollection<DemPropertyRow>();
        public ObservableCollection<DemHeaderRow> HeaderRows { get; } = new ObservableCollection<DemHeaderRow>();
        public ObservableCollection<DemActorRow> ActorRows { get; } = new ObservableCollection<DemActorRow>();

        public string DemName => currentDemFile?.Name ?? "";
        public string DemDurationText => currentDemFile != null ? $"Duration: {currentDemFile.I_08}" : "";
        public int CurrentPreviewFrame => previewCurrentFrame;
        public int MaxPreviewFrame => previewEndFrame;

        public DemCutRow SelectedCut
        {
            get => selectedCut;
            set
            {
                if (selectedCut != value)
                {
                    selectedCut = value;
                    NotifyPropertyChanged(nameof(SelectedCut));
                    LoadEvents();

                    if (SceneManager.IsOnTab(EditorTabs.Action))
                    {
                        AutoPlayDemCut();
                    }
                }
            }
        }

        public DemEventRow SelectedEvent
        {
            get => selectedEvent;
            set
            {
                if (selectedEvent != value)
                {
                    selectedEvent = value;
                    NotifyPropertyChanged(nameof(SelectedEvent));
                    LoadSelectedEventProperties();
                    PreviewSelectedEvent();
                }
            }
        }

        public DemTab()
        {
            InitializeComponent();
            ActiveDemTab = this;
            DataContext = this;
            Viewport.UpdateEvent += PreviewUpdate;
            Loaded += DemTab_Loaded;
            Unloaded += DemTab_Unloaded;
            Files.SelectedItemChanged += Files_SelectedItemChanged;
            LoadDem();
        }

        public void AutoPlayDem()
        {
            if (!SceneManager.IsOnTab(EditorTabs.Action) || currentDemFile?.Section2Entries == null)
            {
                return;
            }

            StopCutPreviewTimer();
            HashSet<int> actorSlots = LoadDemoActors();
            ResetPreviewScene();
            ShowDemoActors(actorSlots);
            StartPreview(GetFullDemEvents(), SceneManager.AutoPlay);
        }

        public void AutoPlayDemCut()
        {
            if (!SceneManager.IsOnTab(EditorTabs.Action) || SelectedCut?.Cut?.SubEntries == null)
            {
                return;
            }

            StopCutPreviewTimer();
            HashSet<int> actorSlots = LoadDemoActors();
            ResetPreviewScene();
            ShowDemoActors(actorSlots);
            StartPreview(GetCutEvents(SelectedCut.Cut), SceneManager.AutoPlay);
        }

        public void PlayTimeline()
        {
            if (!SceneManager.IsOnTab(EditorTabs.Action) || currentDemFile?.Section2Entries == null)
            {
                return;
            }

            if (previewEvents.Count == 0 || previewCurrentFrame >= previewEndFrame)
            {
                StopCutPreviewTimer();
                HashSet<int> actorSlots = LoadDemoActors();
                ResetPreviewScene();
                ShowDemoActors(actorSlots);
                StartPreview(GetFullDemEvents(), true);
                return;
            }

            Viewport.Instance.IsPlaying = true;
            StartPreviewTimer();
        }

        public void StopTimeline()
        {
            if (!SceneManager.IsOnTab(EditorTabs.Action) || currentDemFile == null)
            {
                return;
            }

            SeekTimeline(0);
            Viewport.Instance.IsPlaying = false;
        }

        public void SeekPrevFrame()
        {
            SeekTimeline(previewCurrentFrame - 1);
        }

        public void SeekNextFrame()
        {
            SeekTimeline(previewCurrentFrame + 1);
        }

        public void SeekTimeline(int frame)
        {
            if (!SceneManager.IsOnTab(EditorTabs.Action) || currentDemFile?.Section2Entries == null)
            {
                return;
            }

            EnsurePreviewEvents();
            StopCutPreviewTimer(false);
            HashSet<int> actorSlots = LoadDemoActors();
            ResetPreviewScene();
            ShowDemoActors(actorSlots);
            previewEventIndex = 0;
            previewCurrentFrame = Math.Max(previewStartFrame, Math.Min(frame, previewEndFrame));
            try
            {
                suppressEffectPreview = true;
                PlayEventsThroughFrame(previewCurrentFrame);
            }
            finally
            {
                suppressEffectPreview = false;
            }
            Viewport.Instance.IsPlaying = false;
        }

        private void StartPreview(List<DemPlaybackEvent> events, bool play)
        {
            SetPreviewEvents(events);
            previewEventIndex = 0;
            previewStartFrame = previewEvents.Count > 0 ? previewEvents.Min(demEvent => demEvent.Time) : 0;
            previewEndFrame = GetPreviewEndFrame(previewEvents);
            previewCurrentFrame = previewStartFrame;
            NotifyPropertyChanged(nameof(MaxPreviewFrame));

            PlayEventsThroughFrame(previewStartFrame);
            Viewport.Instance.IsPlaying = play;

            if (play && previewCurrentFrame < previewEndFrame)
            {
                StartPreviewTimer();
            }
        }

        private void SetPreviewEvents(List<DemPlaybackEvent> events)
        {
            previewEvents = events ?? new List<DemPlaybackEvent>();
            previewLightEvents = previewEvents
                .Where(playbackEvent => playbackEvent.Event?.I_04 == DEM_Type.DemoDataTypes.LightDir && playbackEvent.Event.Type0_3_8 != null)
                .ToList();
        }

        public void PreviewSelectedEvent()
        {
            if (suppressEventPreview || !SceneManager.IsOnTab(EditorTabs.Action) || SelectedEvent?.Event == null)
            {
                return;
            }

            StopCutPreviewTimer();
            HashSet<int> actorSlots = LoadDemoActors();
            ResetPreviewScene();
            ShowDemoActors(actorSlots);
            PlayDemEvent(SelectedEvent.Event);
            Viewport.Instance.IsPlaying = SceneManager.AutoPlay;
        }

        private void Files_SelectedItemChanged(object sender, EventArgs e)
        {
            LoadDem();
        }

        private void PlayDemButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AutoPlayDem();
        }

        private void PlayCutButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AutoPlayDemCut();
        }

        private void LoadDem()
        {
            DEM_File demFile = files.SelectedItem?.ManualFiles?.DemFile;

            if (currentDemFile == demFile)
            {
                return;
            }

            StopCutPreviewTimer();
            ClearDemLighting();
            ClearDemCameraClip();
            ClearDemActorDamage();
            ClearDemEffects();
            currentDemFile = demFile;
            previewActors.Clear();
            previewActorKeys.Clear();
            previewEffectFiles.Clear();
            previewWarnings.Clear();
            Cuts.Clear();
            HeaderRows.Clear();
            ActorRows.Clear();
            Events.Clear();
            SelectedEventProperties.Clear();
            previewEvents.Clear();
            previewLightEvents.Clear();
            previewEventIndex = 0;
            previewCurrentFrame = 0;
            previewStartFrame = 0;
            previewEndFrame = 0;

            if (currentDemFile?.Section2Entries != null)
            {
                for (int index = 0; index < currentDemFile.Section2Entries.Count; index++)
                {
                    Cuts.Add(new DemCutRow(index, currentDemFile.Section2Entries[index]));
                }
            }

            LoadEditableRows();
            SelectedCut = Cuts.FirstOrDefault();
            PreviewStage(0, true);
            NotifyPropertyChanged(nameof(DemName));
            NotifyPropertyChanged(nameof(DemDurationText));
            NotifyPropertyChanged(nameof(CurrentPreviewFrame));
            NotifyPropertyChanged(nameof(MaxPreviewFrame));
        }

        private void LoadEditableRows()
        {
            HeaderRows.Clear();
            ActorRows.Clear();

            if (currentDemFile == null)
            {
                return;
            }

            currentDemFile.Settings = currentDemFile.Settings ?? new DemoSettings();
            currentDemFile.Settings.Characters = currentDemFile.Settings.Characters ?? new List<Xv2CoreLib.DEM.Character>();

            HeaderRows.Add(new DemHeaderRow("Name", () => currentDemFile.Name, value => currentDemFile.Name = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("Duration", () => currentDemFile.I_08.ToString(), value => SetInt(value, number => currentDemFile.I_08 = number), NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("Camera", () => currentDemFile.Settings.Str_00, value => currentDemFile.Settings.Str_00 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("SE", () => currentDemFile.Settings.Str_16, value => currentDemFile.Settings.Str_16 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("VOX", () => currentDemFile.Settings.Str_24, value => currentDemFile.Settings.Str_24 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("BGM", () => currentDemFile.Settings.Str_32, value => currentDemFile.Settings.Str_32 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("Str_40", () => currentDemFile.Settings.Str_40, value => currentDemFile.Settings.Str_40 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("EEPK", () => currentDemFile.Settings.Str_48, value => currentDemFile.Settings.Str_48 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("EMB", () => currentDemFile.Settings.Str_56, value => currentDemFile.Settings.Str_56 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("Movies", () => currentDemFile.Settings.Str_64, value => currentDemFile.Settings.Str_64 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("Stage0", () => currentDemFile.Settings.Str_08, value => currentDemFile.Settings.Str_08 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("Stage1", () => currentDemFile.Settings.Str_72, value => currentDemFile.Settings.Str_72 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("Stage2", () => currentDemFile.Settings.Str_80, value => currentDemFile.Settings.Str_80 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("Stage3", () => currentDemFile.Settings.Str_88, value => currentDemFile.Settings.Str_88 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("Stage4", () => currentDemFile.Settings.Str_96, value => currentDemFile.Settings.Str_96 = value, NotifyDemHeaderChanged));
            HeaderRows.Add(new DemHeaderRow("EMS", () => currentDemFile.Settings.Str_104, value => currentDemFile.Settings.Str_104 = value, NotifyDemHeaderChanged));

            for (int actorIndex = 0; actorIndex < currentDemFile.Settings.Characters.Count; actorIndex++)
            {
                ActorRows.Add(new DemActorRow(actorIndex, currentDemFile.Settings.Characters[actorIndex], ClearPreviewActorCache));
            }
        }

        private void NotifyDemHeaderChanged()
        {
            NotifyPropertyChanged(nameof(DemName));
            NotifyPropertyChanged(nameof(DemDurationText));
        }

        private void LoadEvents()
        {
            Events.Clear();
            SelectedEventProperties.Clear();

            if (SelectedCut?.Cut?.SubEntries != null)
            {
                foreach (DEM_Type demEvent in SelectedCut.Cut.SubEntries.OrderBy(x => x.I_00))
                {
                    Events.Add(new DemEventRow(demEvent));
                }
            }

            suppressEventPreview = true;
            SelectedEvent = Events.FirstOrDefault();
            suppressEventPreview = false;
        }

        private void LoadSelectedEventProperties()
        {
            SelectedEventProperties.Clear();

            if (SelectedEvent?.Event == null)
            {
                return;
            }

            SelectedEventProperties.Add(new DemPropertyRow("Time", SelectedEvent.Event.I_00.ToString()));
            SelectedEventProperties.Add(new DemPropertyRow("Type", SelectedEvent.TypeName));

            object payload = SelectedEvent.Payload;

            if (payload == null)
            {
                return;
            }

            foreach (PropertyInfo property in payload.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                object value = property.GetValue(payload);
                SelectedEventProperties.Add(new DemPropertyRow(GetXmlFieldName(property), FormatValue(value)));
            }
        }

        private void DemTab_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ActiveDemTab = this;
        }

        private void DemTab_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            StopCutPreviewTimer();
            ClearDemLighting();
            ClearDemCameraClip();
            ClearDemActorDamage();
        }

        private void PreviewUpdate(object sender, EventArgs e)
        {
            if (previewTimerRunning)
            {
                PreviewTimer_Tick(sender, e);
            }
        }

        private void PreviewTimer_Tick(object sender, EventArgs e)
        {
            if (!SceneManager.IsOnTab(EditorTabs.Action))
            {
                StopCutPreviewTimer();
                return;
            }

            if (!Viewport.Instance.IsPlaying)
            {
                return;
            }

            previewCurrentFrame = Math.Min(previewCurrentFrame + 1, previewEndFrame);
            PlayEventsThroughFrame(previewCurrentFrame);

            if (previewCurrentFrame >= previewEndFrame)
            {
                if (SceneManager.Loop)
                {
                    SeekTimeline(previewStartFrame);
                    Viewport.Instance.IsPlaying = true;
                    StartPreviewTimer();
                }
                else
                {
                    StopCutPreviewTimer(false);
                    Viewport.Instance.IsPlaying = false;
                }
            }
        }

        private void StartPreviewTimer()
        {
            previewTimerRunning = true;
        }

        private void StopCutPreviewTimer(bool clearEvents = true)
        {
            previewTimerRunning = false;
            previewEventIndex = 0;

            if (clearEvents)
            {
                previewEvents.Clear();
                previewLightEvents.Clear();
            }
        }

        private void PlayEventsThroughFrame(int currentFrame)
        {
            previewCurrentFrame = currentFrame;

            while (previewEventIndex < previewEvents.Count && previewEvents[previewEventIndex].Time <= currentFrame)
            {
                DemPlaybackEvent playbackEvent = previewEvents[previewEventIndex];
                DEM_Type demEvent = playbackEvent.Event;
                previewEventIndex++;

                try
                {
                    PlayDemEvent(demEvent, currentFrame - playbackEvent.Time);
                }
                catch (Exception ex)
                {
                    AddPreviewWarning($"event:{demEvent?.I_04}:{currentFrame}:{ex.Message}", $"DEM preview: skipped {demEvent?.I_04} event at frame {currentFrame}. {ex.Message}");
                }
            }

            UpdateActiveLightDirection(currentFrame);
            NotifyPropertyChanged(nameof(CurrentPreviewFrame));
        }

        private void UpdateActiveLightDirection(int currentFrame)
        {
            ClearActorLightDirectionOverrides();
            activeActorLightEvents.Clear();
            DemPlaybackEvent defaultLightEvent = null;

            for (int i = 0; i < previewLightEvents.Count; i++)
            {
                DemPlaybackEvent playbackEvent = previewLightEvents[i];

                if (playbackEvent.Time > currentFrame)
                {
                    break;
                }

                int actorIndex = playbackEvent.Event.Type0_3_8.I_1;

                if (actorIndex == 0)
                {
                    defaultLightEvent = playbackEvent;
                }
                else
                {
                    activeActorLightEvents[actorIndex] = playbackEvent;
                }
            }

            if (defaultLightEvent != null)
            {
                PreviewLightDirection(defaultLightEvent.Event.Type0_3_8, currentFrame - defaultLightEvent.Time);
            }

            foreach (DemPlaybackEvent lightEvent in activeActorLightEvents.Values)
            {
                PreviewLightDirection(lightEvent.Event.Type0_3_8, currentFrame - lightEvent.Time);
            }
        }

        private void EnsurePreviewEvents()
        {
            if (previewEvents.Count > 0)
            {
                return;
            }

            SetPreviewEvents(GetFullDemEvents());
            previewStartFrame = previewEvents.Count > 0 ? previewEvents.Min(demEvent => demEvent.Time) : 0;
            previewEndFrame = GetPreviewEndFrame(previewEvents);
            NotifyPropertyChanged(nameof(MaxPreviewFrame));
        }

        private int GetPreviewEndFrame(List<DemPlaybackEvent> events)
        {
            if (currentDemFile?.I_08 > 0)
            {
                return currentDemFile.I_08;
            }

            return events.Count > 0 ? events.Max(demEvent => demEvent.Time) : 0;
        }

        private List<DemPlaybackEvent> GetFullDemEvents()
        {
            return currentDemFile.Section2Entries
                .Where(cut => cut?.SubEntries != null)
                .SelectMany(cut => GetCutEvents(cut))
                .OrderBy(demEvent => demEvent.Time)
                .ToList();
        }

        private static List<DemPlaybackEvent> GetCutEvents(Section2Entry cut)
        {
            int cutStartTime = cut?.I_00 ?? 0;

            return cut?.SubEntries?
                .Select(demEvent => new DemPlaybackEvent(cutStartTime + demEvent.I_00, demEvent))
                .OrderBy(demEvent => demEvent.Time)
                .ToList() ?? new List<DemPlaybackEvent>();
        }

        private void PlayDemEvent(DEM_Type demEvent, int elapsedFrames = 0)
        {
            if (demEvent == null)
            {
                return;
            }

            switch (demEvent.I_04)
            {
                case DEM_Type.DemoDataTypes.LightDir:
                    if (demEvent.Type0_3_8 == null) return;
                    PreviewLightDirection(demEvent.Type0_3_8, elapsedFrames);
                    break;
                case DEM_Type.DemoDataTypes.Animation:
                    if (demEvent.Type1_0_10 == null) return;
                    PreviewAnimation(demEvent.Type1_0_10.I_1, demEvent.Type1_0_10.I_2.ToString(), demEvent.Type1_0_10.Str_3, demEvent.Type1_0_10.F_5, demEvent.Type1_0_10.F_8, demEvent.Type1_0_10.F_9, elapsedFrames);
                    break;
                case DEM_Type.DemoDataTypes.AnimationSmall:
                    if (demEvent.Type1_0_9 == null) return;
                    PreviewAnimation(demEvent.Type1_0_9.I_1, demEvent.Type1_0_9.I_2.ToString(), demEvent.Type1_0_9.Str_3, demEvent.Type1_0_9.F_5, demEvent.Type1_0_9.F_8, demEvent.Type1_0_9.F_9, elapsedFrames);
                    break;
                case DEM_Type.DemoDataTypes.ActorVisibility:
                    if (demEvent.Type1_3_2 == null) return;
                    PreviewActorVisibility(demEvent.Type1_3_2);
                    break;
                case DEM_Type.DemoDataTypes.Transformation:
                    if (demEvent.Type1_4_2 == null) return;
                    PreviewActorTransformation(demEvent.Type1_4_2);
                    break;
                //case DEM_Type.DemoDataTypes.ActorDamage:
                //    if (demEvent.Type1_9_5 == null) return;
                //    PreviewActorDamage(demEvent.Type1_9_5);
                //    break;
                case DEM_Type.DemoDataTypes.Position1:
                    if (demEvent.Type1_1_5 == null) return;
                    PreviewActorPosition(demEvent.Type1_1_5);
                    break;
                case DEM_Type.DemoDataTypes.Position2:
                    if (demEvent.Type1_1_9 == null) return;
                    PreviewActorPosition(demEvent.Type1_1_9);
                    break;
                case DEM_Type.DemoDataTypes.RotateY_1:
                    if (demEvent.Type1_2_3 == null) return;
                    PreviewActorRotation(demEvent.Type1_2_3.I_1, demEvent.Type1_2_3.F_2);
                    break;
                case DEM_Type.DemoDataTypes.RotateY_2:
                    if (demEvent.Type1_2_5 == null) return;
                    PreviewActorRotation(demEvent.Type1_2_5.I_1, demEvent.Type1_2_5.F_2);
                    break;
                case DEM_Type.DemoDataTypes.Camera:
                    if (demEvent.Type2_0_1 == null) return;
                    PreviewCamera(demEvent.Type2_0_1.Str_1, elapsedFrames);
                    break;
                case DEM_Type.DemoDataTypes.SetNearClip:
                    if (demEvent.Type2_9_2 == null) return;
                    SetDemCameraClip(demEvent.Type2_9_2.F_2, null);
                    break;
                case DEM_Type.DemoDataTypes.SetFarClip:
                    if (demEvent.Type2_10_2 == null) return;
                    SetDemCameraClip(null, demEvent.Type2_10_2.F_2);
                    break;
                case DEM_Type.DemoDataTypes.Effect:
                    if (demEvent.Type4_0_12 == null) return;
                    PreviewEffect(demEvent.Type4_0_12);
                    break;
                case DEM_Type.DemoDataTypes.ChangeMap:
                    if (demEvent.Type3_1_1 == null) return;
                    PreviewStage(demEvent.Type3_1_1.I_1);
                    break;
            }
        }

        private void ResetPreviewScene()
        {
            SceneManager.Stop();
            ClearDemLighting();
            ClearDemActorDamage();
            ClearDemEffects();
            PreviewStage(0);
            SetDemCameraClip(DemPreviewNearClip, null);

            foreach (Actor actor in SceneManager.Actors)
            {
                if (actor == null)
                {
                    continue;
                }

                actor.ResetState();
                actor.PartSet?.ResetTransformation();
                actor.PartSet?.ResetBacPartSetSwap(true);
                actor.IsVisible = true;
                actor.ShowModel = true;
                actor.Controller.State = XenoKit.Engine.Character.ActorState.Null;
                actor.ActionControl.ClearBacPlayer();
            }
        }

        private static void ClearDemLighting()
        {
            if (Viewport.Instance == null)
            {
                return;
            }

            Viewport.Instance.SunLight.ClearDirectionOverride();
            Viewport.Instance.LightSource.ClearDirectionOverride();
            ClearActorLightDirectionOverrides();
        }

        private static void ClearActorLightDirectionOverrides()
        {
            foreach (Actor actor in SceneManager.Actors)
            {
                if (actor != null)
                {
                    actor.LightDirectionOverride = null;
                }
            }
        }

        private static void ClearDemActorDamage()
        {
            SceneManager.BattleDamageScratches = 0f;
            SceneManager.BattleDamageBlood = 0f;
        }

        private static void ClearDemEffects()
        {
            Viewport.Instance?.VfxManager.StopEffects();
        }

        private static void ClearDemCameraClip()
        {
            Viewport.Instance?.Camera.ClearClipOverride();
        }

        private static void SetDemCameraClip(float? nearClip, float? farClip)
        {
            if (Viewport.Instance == null)
            {
                return;
            }

            float? activeNearClip = nearClip.HasValue ? Math.Max(0.001f, nearClip.Value) : Viewport.Instance.Camera.NearClipOverride ?? DemPreviewNearClip;
            float? activeFarClip = farClip ?? Viewport.Instance.Camera.FarClipOverride;
            Viewport.Instance.Camera.SetClipOverride(activeNearClip, activeFarClip);
        }

        private HashSet<int> LoadDemoActors()
        {
            int actorCount = currentDemFile?.Settings?.Characters?.Count ?? 0;
            HashSet<int> actorSlots = new HashSet<int>();

            if (actorCount > SceneManager.NumActors)
            {
                AddPreviewWarning($"actorCount:{actorCount}:{SceneManager.NumActors}", $"DEM preview: this DEM has {actorCount} actors, but only {SceneManager.NumActors} actor slots are available.");
            }

            for (int actorIndex = 0; actorIndex < Math.Min(actorCount, SceneManager.NumActors); actorIndex++)
            {
                Actor actor = LoadDemoActor(actorIndex);

                if (actor == null)
                {
                    continue;
                }

                if (SceneManager.Actors[actorIndex] != actor)
                {
                    SceneManager.SetActor(actor, actorIndex);
                }

                actorSlots.Add(actorIndex);
            }

            return actorSlots;
        }

        private void ShowDemoActors(HashSet<int> actorSlots)
        {
            for (int actorIndex = 0; actorIndex < SceneManager.ActorsEnable.Length; actorIndex++)
            {
                SceneManager.ActorsEnable[actorIndex] = actorSlots.Contains(actorIndex) && SceneManager.Actors[actorIndex] != null;
            }

            SceneManager.VictimEnabled = false;
        }

        private void PreviewAnimation(int actorIndex, string eanType, string animationName, float timeScale, float blendWeight, float blendWeightStep, int elapsedFrames)
        {
            Actor actor = GetDemoActor(actorIndex);
            EAN_File eanFile = GetAnimationFile(actor, actorIndex, eanType);

            if (actor == null || eanFile?.Animations == null || string.IsNullOrWhiteSpace(animationName))
            {
                return;
            }

            if (actor.AnimationPlayer == null || actor.Skeleton == null)
            {
                AddPreviewWarning($"animationActor:{actorIndex}:{eanType}:{animationName}", $"DEM preview: actor {actorIndex} is missing animation state for \"{animationName}\".");
                return;
            }

            EAN_Animation animation = FindDemAnimation(eanFile, eanType, animationName);

            if (animation == null)
            {
                EAN_File alternateFaceFile = GetAlternateFaceAnimationFile(actor, eanFile, eanType);
                animation = FindDemAnimation(alternateFaceFile, eanType, animationName);

                if (animation == null)
                {
                    AddPreviewWarning($"animation:{actorIndex}:{eanType}:{animationName}", $"DEM preview: could not find {eanType} animation \"{animationName}\" for actor {actorIndex}.");
                    return;
                }

                eanFile = alternateFaceFile;
            }

            float playbackSpeed = timeScale == 0f ? 1f : Math.Abs(timeScale);
            int animationFrame = Math.Max(0, (int)Math.Floor(elapsedFrames * playbackSpeed));

            switch (eanType)
            {
                case "FaceBase":
                case "FaceForehead":
                    try
                    {
                        ClearDemFaceAnimations(actor, eanFile);
                        actor.AnimationPlayer.PlaySecondaryAnimation(eanFile, animation.ID_UShort, 0, ushort.MaxValue, blendWeight, blendWeightStep, playbackSpeed, true);
                        AnimationInstance secondaryAnimation = actor.AnimationPlayer.SecondaryAnimations.LastOrDefault();
                        SeekAnimationInstance(secondaryAnimation, animationFrame);
                    }
                    catch (Exception ex)
                    {
                        AddPreviewWarning($"animationPlay:{actorIndex}:{eanType}:{animationName}:{ex.Message}", $"DEM preview: skipped {eanType} animation \"{animationName}\" for actor {actorIndex}. {ex.Message}");
                        return;
                    }
                    break;
                default:
                    try
                    {
                        actor.AnimationPlayer.PlayPrimaryAnimation(eanFile, animation.ID_UShort, 0, ushort.MaxValue, blendWeight, blendWeightStep, 0, false, playbackSpeed, false);
                        SeekAnimationInstance(actor.AnimationPlayer.PrimaryAnimation, animationFrame);
                    }
                    catch (Exception ex)
                    {
                        AddPreviewWarning($"animationPlay:{actorIndex}:{eanType}:{animationName}:{ex.Message}", $"DEM preview: skipped {eanType} animation \"{animationName}\" for actor {actorIndex}. {ex.Message}");
                        return;
                    }
                    break;
            }

            try
            {
                UpdateDemoActor(actor);
            }
            catch (Exception ex)
            {
                AddPreviewWarning($"animationUpdate:{actorIndex}:{eanType}:{animationName}:{ex.Message}", $"DEM preview: skipped update for {eanType} animation \"{animationName}\" on actor {actorIndex}. {ex.Message}");
            }
        }

        private static void ClearDemFaceAnimations(Actor actor, EAN_File eanFile)
        {
            if (actor?.AnimationPlayer?.SecondaryAnimations == null)
            {
                return;
            }

            actor.AnimationPlayer.SecondaryAnimations.RemoveAll(animation =>
                animation?.EanFile == eanFile ||
                animation?.EanFile == actor.FceEanFile ||
                animation?.EanFile == actor.FceEyeEanFile);
        }

        private static void UpdateDemoActor(Actor actor)
        {
            if (actor == null)
            {
                return;
            }

            try
            {
                actor.AnimationPlayer?.Update(Matrix4x4.Identity);
            }
            catch (NullReferenceException)
            {
            }

            try
            {
                FpfPosePreview.Apply(actor);
            }
            catch (NullReferenceException)
            {
            }

            try
            {
                actor.PartSet?.Update();
            }
            catch (NullReferenceException)
            {
            }

            try
            {
                FpfPosePreview.ApplyAdditionalSkeletons(actor);
            }
            catch (NullReferenceException)
            {
            }
        }

        private static void SeekAnimationInstance(AnimationInstance animation, int frame)
        {
            if (animation == null)
            {
                return;
            }

            animation.SkipToFrame(Math.Min(frame, animation.EndFrame));
            animation.ResetFrameIndex();
        }

        private static EAN_Animation FindDemAnimation(EAN_File eanFile, string eanType, string animationName)
        {
            if (eanFile?.Animations == null)
            {
                return null;
            }

            EAN_Animation animation = eanFile.Animations.FirstOrDefault(x => x.Name == animationName);

            if (animation != null)
            {
                return animation;
            }

            string faceSuffix = GetFaceAnimationSuffix(eanType, animationName);

            if (faceSuffix == null)
            {
                return null;
            }

            return eanFile.Animations.FirstOrDefault(x => x.Name?.EndsWith(faceSuffix, StringComparison.OrdinalIgnoreCase) == true);
        }

        private static EAN_File GetAlternateFaceAnimationFile(Actor actor, EAN_File currentFile, string eanType)
        {
            switch (eanType)
            {
                case "FaceBase":
                    return actor?.FceEyeEanFile != currentFile ? actor?.FceEyeEanFile : null;
                case "FaceForehead":
                    return actor?.FceEanFile != currentFile ? actor?.FceEanFile : null;
                default:
                    return null;
            }
        }

        private static string GetFaceAnimationSuffix(string eanType, string animationName)
        {
            if (string.IsNullOrWhiteSpace(animationName))
            {
                return null;
            }

            string faceHalf;

            switch (eanType)
            {
                case "FaceForehead":
                    faceHalf = "U";
                    break;
                case "FaceBase":
                    faceHalf = "L";
                    break;
                default:
                    return null;
            }

            int markerIndex = animationName.IndexOf("_FCE_", StringComparison.OrdinalIgnoreCase);

            if (markerIndex < 1)
            {
                return null;
            }

            return $"{faceHalf}{animationName.Substring(markerIndex)}";
        }

        private void PreviewActorVisibility(Type1_3_2 visibilityEvent)
        {
            Actor actor = GetDemoActor(visibilityEvent.I_1);

            if (actor == null)
            {
                return;
            }

            actor.IsVisible = true;
            actor.ShowModel = visibilityEvent.I_2 == Type1_3_2.Visibility.Visible;
        }

        private void PreviewActorTransformation(Type1_4_2 transformationEvent)
        {
            Actor actor = GetDemoActor(transformationEvent.I_1);

            if (actor == null)
            {
                return;
            }

            actor.PartSet?.ResetTransformation();
            actor.PartSet?.ResetBacPartSetSwap(true);

            if (transformationEvent.I_2 == -1)
            {
                return;
            }

            actor.PartSet?.ApplyTransformation(transformationEvent.I_2);
        }

        private void PreviewActorDamage(Type1_9_5 damageEvent)
        {
            Actor actor = GetDemoActor(damageEvent.I_1);

            if (actor == null)
            {
                return;
            }

            SceneManager.BattleDamageScratches = GetDamageAmount(damageEvent.F_3);
            SceneManager.BattleDamageBlood = GetDamageAmount(damageEvent.F_4);
        }

        private static float GetDamageAmount(float value)
        {
            if (Math.Abs(value) < 0.0001f)
            {
                return 1f;
            }

            return Math.Max(0f, Math.Min(1f, value));
        }

        private void PreviewActorPosition(Type1_1_5 positionEvent)
        {
            PreviewActorPosition(positionEvent.I_1, ToSingle(positionEvent.I_2), ToSingle(positionEvent.I_3), ToSingle(positionEvent.I_4));
        }

        private void PreviewActorPosition(Type1_1_9 positionEvent)
        {
            PreviewActorPosition(positionEvent.I_1, positionEvent.F_2, positionEvent.F_4, ToSingle(positionEvent.I_3));
        }

        private void PreviewActorPosition(int actorIndex, float x, float y, float z)
        {
            Actor actor = GetDemoActor(actorIndex);

            if (actor == null)
            {
                return;
            }

            Matrix4x4.Decompose(actor.BaseTransform, out SimdVector3 scale, out SimdQuaternion rotation, out _);
            actor.BaseTransform = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(new SimdVector3(x, y, z));
            actor.ActionMovementTransform = Matrix4x4.Identity;
            actor.RootMotionTransform = Matrix4x4.Identity;
        }

        private void PreviewActorRotation(int actorIndex, float angle)
        {
            Actor actor = GetDemoActor(actorIndex);

            if (actor == null)
            {
                return;
            }

            Matrix4x4.Decompose(actor.BaseTransform, out SimdVector3 scale, out _, out SimdVector3 translation);
            actor.BaseTransform = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY((float)(Math.PI / 180d * angle)) * Matrix4x4.CreateTranslation(translation);
            actor.ActionMovementTransform = Matrix4x4.Identity;
            actor.RootMotionTransform = Matrix4x4.Identity;
        }

        private void PreviewLightDirection(Type0_3_8 lightEvent, int elapsedFrames)
        {
            SimdVector3 direction = GetLightDirection(lightEvent, elapsedFrames);

            if (lightEvent.I_1 == 0)
            {
                foreach (Actor actor in SceneManager.Actors)
                {
                    if (actor != null)
                    {
                        actor.LightDirectionOverride = direction;
                    }
                }

                return;
            }

            Actor targetActor = GetDemoActor(lightEvent.I_1);

            if (targetActor != null)
            {
                targetActor.LightDirectionOverride = direction;
            }
        }

        private static SimdVector3 GetLightDirection(Type0_3_8 lightEvent, int elapsedFrames)
        {
            SimdVector3 from = new SimdVector3(lightEvent.F_2, lightEvent.F_3, lightEvent.F_4);
            SimdVector3 to = new SimdVector3(lightEvent.F_5, lightEvent.F_6, lightEvent.F_7);
            int duration = Math.Max(0, lightEvent.I_8);
            float amount = duration > 0 ? Math.Max(0f, Math.Min(1f, elapsedFrames / (float)duration)) : 1f;
            SimdVector3 direction = SimdVector3.Lerp(from, to, amount);
            Matrix4x4 rotation =
                Matrix4x4.CreateRotationX(ToRadians(30d)) *
                Matrix4x4.CreateRotationY(ToRadians(180d));
            return SimdVector3.TransformNormal(direction, rotation);
        }

        private static float ToRadians(double degrees)
        {
            return (float)(Math.PI / 180d * degrees);
        }

        private static float ToSingle(int value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }

        private void PreviewCamera(string cameraName, int elapsedFrames)
        {
            if (!SceneManager.UseCameras)
            {
                return;
            }

            Xv2File<EAN_File> cameraFile = files.SelectedMove?.Files.CamEanFile.FirstOrDefault();

            if (cameraFile == null || string.IsNullOrWhiteSpace(cameraName))
            {
                return;
            }

            EAN_Animation camera = cameraFile.File.Animations.FirstOrDefault(x => x.Name == cameraName);

            if (camera == null)
            {
                AddPreviewWarning($"camera:{cameraName}", $"DEM preview: could not find camera \"{cameraName}\".");
                return;
            }

            Viewport.Instance.Camera.PlayRawCameraAnimation(cameraFile.File, camera, false);
            Viewport.Instance.Camera.SkipToFrame(Math.Max(0, elapsedFrames));
        }

        private void PreviewEffect(Type4_0_12 effectEvent)
        {
            if (suppressEffectPreview || !SettingsManager.Instance.Settings.XenoKit_VfxSimulation)
            {
                return;
            }

            Actor actor = GetDemoActor(effectEvent.I_1);

            if (actor == null)
            {
                return;
            }

            EffectContainerFile eepkFile = GetDemEffectFile(effectEvent, actor);

            if (eepkFile == null)
            {
                return;
            }

            Effect effect = eepkFile.GetEffect(effectEvent.I_5);

            if (effect == null)
            {
                AddPreviewWarning($"effect:{effectEvent.I_2}:{effectEvent.I_4}:{effectEvent.I_5}", $"DEM preview: could not find effect {effectEvent.I_5}.");
                return;
            }

            if (effectEvent.I_12 != 0)
            {
                Viewport.Instance.VfxManager.StopEffect(effect);
                return;
            }

            Matrix4x4 spawnPosition = actor.Transform * Matrix4x4.CreateTranslation(new SimdVector3(effectEvent.F_6, effectEvent.F_7, effectEvent.F_8));
            Viewport.Instance.VfxManager.PlayEffect(effect, actor, spawnPosition);
        }

        private EffectContainerFile GetDemEffectFile(Type4_0_12 effectEvent, Actor actor)
        {
            if (effectEvent.I_2 == 0 && effectEvent.I_4 == -1)
            {
                return GetBattleCommonEffectFile();
            }

            if (effectEvent.I_2 == 4)
            {
                if (effectEvent.I_4 == 0)
                {
                    return GetDemoCommonEffectFile();
                }

                return GetDemoHeaderEffectFile();
            }

            BAC_Type8.EepkTypeEnum eepkType = (BAC_Type8.EepkTypeEnum)effectEvent.I_2;

            switch (eepkType)
            {
                case BAC_Type8.EepkTypeEnum.StageBG:
                    return GetStageBgEffectFile(effectEvent.I_4);
                case BAC_Type8.EepkTypeEnum.Common:
                case BAC_Type8.EepkTypeEnum.Stage:
                    return GetErsEffectFile(effectEvent.I_2, effectEvent.I_4);
                case BAC_Type8.EepkTypeEnum.Character:
                    return GetCharacterEffectFile(actor);
                case BAC_Type8.EepkTypeEnum.AwokenSkill:
                case BAC_Type8.EepkTypeEnum.SuperSkill:
                case BAC_Type8.EepkTypeEnum.UltimateSkill:
                case BAC_Type8.EepkTypeEnum.EvasiveSkill:
                case BAC_Type8.EepkTypeEnum.KiBlastSkill:
                case BAC_Type8.EepkTypeEnum.NEW_AwokenSkill:
                    return GetSkillEffectFile(eepkType, effectEvent.I_4);
                default:
                    AddPreviewWarning($"eepkUnsupported:{effectEvent.I_2}:{effectEvent.I_5}", $"DEM preview: unsupported EEPK_Type {effectEvent.I_2} for effect {effectEvent.I_5}.");
                    return null;
            }
        }

        private EffectContainerFile GetDemoHeaderEffectFile()
        {
            const string cacheKey = "demo";

            if (previewEffectFiles.TryGetValue(cacheKey, out EffectContainerFile eepkFile))
            {
                return eepkFile;
            }

            try
            {
                eepkFile = files.SelectedItem?.ManualFiles?.GetOrLoadDemEffectEepk()?.File;
            }
            catch (Exception ex)
            {
                AddPreviewWarning($"eepk:demo:{ex.Message}", $"DEM preview: could not load DEM EEPK. {ex.Message}");
                return null;
            }

            if (eepkFile != null)
            {
                previewEffectFiles[cacheKey] = eepkFile;
            }

            return eepkFile;
        }

        private EffectContainerFile GetStageBgEffectFile(int entryId)
        {
            if (entryId > 0)
            {
                return GetErsEffectFile((int)BAC_Type8.EepkTypeEnum.StageBG, entryId);
            }

            Xv2CoreLib.Eternity.StageDef stageDef = Viewport.Instance?.CurrentStage?.StageDefEntry;
            string stageCode = stageDef?.CODE;

            if (string.IsNullOrWhiteSpace(stageCode))
            {
                AddPreviewWarning("eepk:stageBg:noStage", "DEM preview: could not load Stage BG EEPK because no stage is active.");
                return null;
            }

            stageCode = stageCode.Trim();
            Xv2CoreLib.ERS.ERS_MainTableEntry stageBgEntry = FindStageBgErsEntry(stageDef);

            if (stageBgEntry != null)
            {
                string ersPath = $"vfx/{stageBgEntry.FILE_PATH}";
                EffectContainerFile ersEepkFile = GetEffectFileFromGame($"stageBg:ers:{stageBgEntry.ID}", ersPath, false);

                if (ersEepkFile != null)
                {
                    return ersEepkFile;
                }
            }

            string cacheKey = $"stageBg:{stageCode}";

            foreach (string bgCode in GetStageBgCodes(stageDef))
            {
                string bgPath = $"vfx/bg/{bgCode}/BG_{bgCode}.eepk";
                EffectContainerFile eepkFile = GetEffectFileFromGame($"{cacheKey}:bg:{bgCode}", bgPath, false);

                if (eepkFile != null)
                {
                    return eepkFile;
                }

                string stagePath = $"vfx/stage/{bgCode}/BG_{bgCode}.eepk";
                eepkFile = GetEffectFileFromGame($"{cacheKey}:stage:{bgCode}", stagePath, false);

                if (eepkFile != null)
                {
                    return eepkFile;
                }
            }

            AddPreviewWarning($"eepk:stageBg:{stageCode}", $"DEM preview: could not load Stage BG EEPK for stage \"{stageCode}\".");

            return null;
        }

        private Xv2CoreLib.ERS.ERS_MainTableEntry FindStageBgErsEntry(Xv2CoreLib.Eternity.StageDef stageDef)
        {
            HashSet<string> names = new HashSet<string>(GetStageBgCodes(stageDef), StringComparer.OrdinalIgnoreCase);
            List<Xv2CoreLib.ERS.ERS_MainTableEntry> entries = xv2.Instance.ErsFile.GetSubentryList((int)BAC_Type8.EepkTypeEnum.StageBG);

            return entries?.FirstOrDefault(entry => names.Contains(entry.Str_04));
        }

        private static IEnumerable<string> GetStageBgCodes(Xv2CoreLib.Eternity.StageDef stageDef)
        {
            foreach (string code in new[] { stageDef?.CODE, stageDef?.EVE, stageDef?.DIR, stageDef?.STR4 })
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                string trimmedCode = code.Trim();
                yield return trimmedCode;

                if (trimmedCode.Length > 2)
                {
                    yield return trimmedCode.Substring(2).ToUpperInvariant();
                }
            }
        }

        private EffectContainerFile GetBattleCommonEffectFile()
        {
            const string cacheKey = "battleCommon";
            const string path = "vfx/cmn/BTL_CMN.eepk";

            return GetEffectFileFromGame(cacheKey, path);
        }

        private EffectContainerFile GetDemoCommonEffectFile()
        {
            const string cacheKey = "demoCommon";
            const string path = "vfx/demo/DM_CMN/DM_CMN.eepk";

            return GetEffectFileFromGame(cacheKey, path);
        }

        private EffectContainerFile GetEffectFileFromGame(string cacheKey, string path, bool logMissing = true)
        {
            if (previewEffectFiles.TryGetValue(cacheKey, out EffectContainerFile eepkFile))
            {
                return eepkFile;
            }

            try
            {
                eepkFile = file.Instance.GetParsedFileFromGame(path, files.SelectedItem?.OnlyLoadFromCPK == true, false) as EffectContainerFile;
            }
            catch (Exception ex)
            {
                previewEffectFiles[cacheKey] = null;

                if (logMissing)
                {
                    AddPreviewWarning($"eepk:{path}:{ex.Message}", $"DEM preview: could not load EEPK \"{path}\". {ex.Message}");
                }

                return null;
            }

            if (eepkFile == null)
            {
                previewEffectFiles[cacheKey] = null;

                if (logMissing)
                {
                    AddPreviewWarning($"eepk:{path}", $"DEM preview: could not find EEPK \"{path}\".");
                }

                return null;
            }

            previewEffectFiles[cacheKey] = eepkFile;
            return eepkFile;
        }

        private EffectContainerFile GetCharacterEffectFile(Actor actor)
        {
            EffectContainerFile eepkFile = actor?.Moveset?.Files?.EepkFile?.File;

            if (eepkFile == null)
            {
                AddPreviewWarning("eepk:character", "DEM preview: could not find the actor character EEPK.");
            }

            return eepkFile;
        }

        private EffectContainerFile GetSkillEffectFile(BAC_Type8.EepkTypeEnum eepkType, int skillId)
        {
            if (skillId < 0)
            {
                AddPreviewWarning($"eepkSkill:{eepkType}:invalid", $"DEM preview: EEPK_Type {eepkType} requires a valid Skill_ID.");
                return null;
            }

            if (!TryGetSkillType(eepkType, out CUS_File.SkillType skillType))
            {
                AddPreviewWarning($"eepkSkill:{eepkType}:unsupported", $"DEM preview: unsupported skill EEPK_Type {eepkType}.");
                return null;
            }

            string cacheKey = $"skill:{skillType}:{skillId}";

            if (previewEffectFiles.TryGetValue(cacheKey, out EffectContainerFile eepkFile))
            {
                return eepkFile;
            }

            Xv2Skill skill = xv2.Instance.GetSkill(skillType, CUS_File.ConvertToID1(skillId, skillType), true, files.SelectedItem?.OnlyLoadFromCPK == true);
            eepkFile = skill?.Files?.EepkFile?.File;

            if (eepkFile == null)
            {
                previewEffectFiles[cacheKey] = null;
                AddPreviewWarning($"eepkSkill:{skillType}:{skillId}", $"DEM preview: could not load {skillType} skill EEPK for Skill_ID {skillId}.");
                return null;
            }

            previewEffectFiles[cacheKey] = eepkFile;
            return eepkFile;
        }

        private EffectContainerFile GetErsEffectFile(int eepkType, int entryId)
        {
            if (entryId < 0)
            {
                AddPreviewWarning($"eepkErs:{eepkType}:invalid", $"DEM preview: EEPK_Type {eepkType} requires a valid EEPK entry ID.");
                return null;
            }

            string cacheKey = $"ers:{eepkType}:{entryId}";

            if (previewEffectFiles.TryGetValue(cacheKey, out EffectContainerFile eepkFile))
            {
                return eepkFile;
            }

            Xv2CoreLib.ERS.ERS_MainTableEntry ersEntry = xv2.Instance.ErsFile.GetEntry(eepkType, entryId);

            if (ersEntry == null || string.IsNullOrWhiteSpace(ersEntry.FILE_PATH))
            {
                AddPreviewWarning($"eepkErs:{eepkType}:{entryId}:missing", $"DEM preview: could not find ERS EEPK entry {entryId} for EEPK_Type {eepkType}.");
                return null;
            }

            string path = $"vfx/{ersEntry.FILE_PATH}";
            eepkFile = GetEffectFileFromGame(cacheKey, path);

            if (eepkFile == null)
            {
                return null;
            }

            previewEffectFiles[cacheKey] = eepkFile;
            return eepkFile;
        }

        private static bool TryGetSkillType(BAC_Type8.EepkTypeEnum eepkType, out CUS_File.SkillType skillType)
        {
            switch (eepkType)
            {
                case BAC_Type8.EepkTypeEnum.SuperSkill:
                    skillType = CUS_File.SkillType.Super;
                    return true;
                case BAC_Type8.EepkTypeEnum.UltimateSkill:
                    skillType = CUS_File.SkillType.Ultimate;
                    return true;
                case BAC_Type8.EepkTypeEnum.EvasiveSkill:
                    skillType = CUS_File.SkillType.Evasive;
                    return true;
                case BAC_Type8.EepkTypeEnum.KiBlastSkill:
                    skillType = CUS_File.SkillType.Blast;
                    return true;
                case BAC_Type8.EepkTypeEnum.AwokenSkill:
                case BAC_Type8.EepkTypeEnum.NEW_AwokenSkill:
                    skillType = CUS_File.SkillType.Awoken;
                    return true;
                default:
                    skillType = CUS_File.SkillType.NotSet;
                    return false;
            }
        }

        private void PreviewStage(int stageIndex, bool forceSet = false)
        {
            files.SetActiveDemStage(currentDemFile, stageIndex, forceSet);
        }

        private Actor GetDemoActor(int actorIndex)
        {
            if (actorIndex < 0 || actorIndex >= SceneManager.NumActors)
            {
                AddPreviewWarning($"actorIndex:{actorIndex}", $"DEM preview: actor index {actorIndex} is outside the available actor slots.");
                return null;
            }

            Actor actor = LoadDemoActor(actorIndex);

            if (actor == null)
            {
                return null;
            }

            if (SceneManager.Actors[actorIndex] != actor)
            {
                SceneManager.SetActor(actor, actorIndex);
            }

            return actor;
        }

        private Actor LoadDemoActor(int actorIndex)
        {
            Xv2CoreLib.DEM.Character demActor = currentDemFile?.Settings?.Characters?.ElementAtOrDefault(actorIndex);

            if (demActor == null || string.IsNullOrWhiteSpace(demActor.Str_00))
            {
                AddPreviewWarning($"actor:{actorIndex}:missingCode", $"DEM preview: actor {actorIndex} is missing a character code.");
                return null;
            }

            string actorKey = $"{demActor.Str_00}:{demActor.I_08}";

            if (previewActors.TryGetValue(actorIndex, out Actor actor) && previewActorKeys.TryGetValue(actorIndex, out string currentActorKey) && currentActorKey == actorKey)
            {
                return actor;
            }

            int cmsId = xv2.Instance.CmsFile.CharaCodeToCharaId(demActor.Str_00);

            if (cmsId == -1)
            {
                AddPreviewWarning($"actor:{demActor.Str_00}:cms", $"DEM preview: could not find character \"{demActor.Str_00}\" in CMS.");
                return null;
            }

            try
            {
                actor = files.LoadPreviewCharacter(cmsId, demActor.I_08, files.SelectedItem?.OnlyLoadFromCPK == true);
            }
            catch (Exception ex)
            {
                AddPreviewWarning($"actor:{actorIndex}:{demActor.Str_00}:{ex.Message}", $"DEM preview: skipped actor {actorIndex} ({demActor.Str_00}) because it could not load. {ex.Message}");
                return null;
            }

            previewActors[actorIndex] = actor;
            previewActorKeys[actorIndex] = actorKey;

            return actor;
        }

        private void ClearPreviewActorCache()
        {
            previewActors.Clear();
            previewActorKeys.Clear();
        }

        private static void SetInt(string value, Action<int> setValue)
        {
            if (int.TryParse(value, out int number))
            {
                setValue(number);
            }
        }

        private EAN_File GetAnimationFile(Actor actor, int actorIndex, string eanType)
        {
            switch (eanType)
            {
                case "Demo":
                    {
                        Xv2File<EAN_File> eanFile = files.SelectedItem?.ManualFiles?.GetOrLoadDemActorEan(actorIndex);

                        if (eanFile?.File == null)
                        {
                            string charaCode = currentDemFile?.Settings?.Characters?.ElementAtOrDefault(actorIndex)?.Str_00;
                            AddPreviewWarning($"ean:demo:{actorIndex}:{charaCode}", $"DEM preview: could not find demo EAN for actor {actorIndex} ({charaCode}).");
                        }

                        return eanFile?.File;
                    }
                case "Character":
                    return actor?.Moveset?.Files.GetEanFile(actor.ShortName, true);
                case "FaceBase":
                    return actor?.FceEanFile;
                case "FaceForehead":
                    return actor?.FceEyeEanFile;
                default:
                    AddPreviewWarning($"eanType:{eanType}", $"DEM preview: unsupported animation EAN type \"{eanType}\".");
                    return null;
            }
        }

        private static string FormatValue(object value)
        {
            if (value == null)
            {
                return "";
            }

            if (value is string text)
            {
                return text;
            }

            if (value is IEnumerable values)
            {
                return $"Count: {values.Cast<object>().Count()}";
            }

            return value.ToString();
        }

        private static string GetXmlFieldName(PropertyInfo property)
        {
            YAXAttributeForAttribute attributeFor = property.GetCustomAttribute<YAXAttributeForAttribute>();

            if (!string.IsNullOrWhiteSpace(attributeFor?.Parent))
            {
                return attributeFor.Parent;
            }

            YAXSerializeAsAttribute serializeAs = property.GetCustomAttribute<YAXSerializeAsAttribute>();

            if (!string.IsNullOrWhiteSpace(serializeAs?.SerializeAs) &&
                !serializeAs.SerializeAs.Equals("value", StringComparison.OrdinalIgnoreCase) &&
                !serializeAs.SerializeAs.Equals("values", StringComparison.OrdinalIgnoreCase))
            {
                return serializeAs.SerializeAs;
            }

            return property.Name;
        }

        private void AddPreviewWarning(string key, string message)
        {
            if (previewWarnings.Add(key))
            {
                Log.Add(message, LogType.Warning);
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DemPlaybackEvent
    {
        public int Time { get; }
        public DEM_Type Event { get; }

        public DemPlaybackEvent(int time, DEM_Type demEvent)
        {
            Time = time;
            Event = demEvent;
        }
    }

    public class DemHeaderRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Func<string> getValue;
        private readonly Action<string> setValue;
        private readonly Action valueChanged;

        public string Name { get; }
        public string Value
        {
            get => getValue() ?? "";
            set
            {
                setValue(value);
                valueChanged?.Invoke();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public DemHeaderRow(string name, Func<string> getValue, Action<string> setValue, Action valueChanged)
        {
            Name = name;
            this.getValue = getValue;
            this.setValue = setValue;
            this.valueChanged = valueChanged;
        }
    }

    public class DemActorRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Xv2CoreLib.DEM.Character actor;
        private readonly Action valueChanged;

        public int Index { get; }
        public string Character
        {
            get => actor.Str_00;
            set
            {
                actor.Str_00 = value;
                valueChanged?.Invoke();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Character)));
            }
        }
        public int Costume
        {
            get => actor.I_08;
            set
            {
                actor.I_08 = value;
                valueChanged?.Invoke();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Costume)));
            }
        }
        public string EanFile
        {
            get => actor.Str_16;
            set
            {
                actor.Str_16 = value;
                valueChanged?.Invoke();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EanFile)));
            }
        }

        public DemActorRow(int index, Xv2CoreLib.DEM.Character actor, Action valueChanged)
        {
            Index = index;
            this.actor = actor;
            this.valueChanged = valueChanged;
        }
    }

    public class DemCutRow
    {
        public int Index { get; }
        public int StartTime => Cut.I_00;
        public int EventCount => Cut.SubEntries?.Count ?? 0;
        public Section2Entry Cut { get; }

        public DemCutRow(int index, Section2Entry cut)
        {
            Index = index;
            Cut = cut;
        }
    }

    public class DemEventRow
    {
        public int Time => Event.I_00;
        public string TypeName => Event.I_04.ToString();
        public string Detail => GetDetail();
        public DEM_Type Event { get; }
        public object Payload => GetPayload();

        public DemEventRow(DEM_Type demEvent)
        {
            Event = demEvent;
        }

        private string GetDetail()
        {
            if (Event.Type1_0_10 != null) return $"{Event.Type1_0_10.Str_3} (Actor {Event.Type1_0_10.I_1})";
            if (Event.Type1_0_9 != null) return $"{Event.Type1_0_9.Str_3} (Actor {Event.Type1_0_9.I_1})";
            if (Event.Type1_4_2 != null) return $"Transform {Event.Type1_4_2.I_2} (Actor {Event.Type1_4_2.I_1})";
            if (Event.Type1_9_5 != null) return $"Damage {Event.Type1_9_5.F_3:0.###}/{Event.Type1_9_5.F_4:0.###} (Actor {Event.Type1_9_5.I_1})";
            if (Event.Type2_0_1 != null) return Event.Type2_0_1.Str_1;
            if (Event.Type3_1_1 != null) return $"Stage {Event.Type3_1_1.I_1}";
            if (Event.Type4_0_12 != null) return $"Effect {Event.Type4_0_12.I_5}";
            if (Event.Type5_0_3 != null) return $"Cue {Event.Type5_0_3.I_3}";
            if (Event.Type5_0_2 != null) return $"Cue {Event.Type5_0_2.I_2}";
            return "";
        }

        private object GetPayload()
        {
            return Event.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.Name.StartsWith("Type", StringComparison.Ordinal))
                .Select(x => x.GetValue(Event))
                .FirstOrDefault(x => x != null);
        }
    }

    public class DemPropertyRow
    {
        public string Name { get; }
        public string Value { get; }

        public DemPropertyRow(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
