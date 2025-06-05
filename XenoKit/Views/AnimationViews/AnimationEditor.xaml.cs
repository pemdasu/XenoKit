using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using XenoKit.Editor;
using Xv2CoreLib.AnimationFramework;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight.CommandWpf;
using System.Linq;

namespace XenoKit.Views.AnimationViews
{
    /// <summary>
    /// Interaction logic for AnimationEditor.xaml
    /// </summary>
    public partial class AnimationEditor : UserControl, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        #region DP
        public Animation CurrentAnimation
        {
            get => (Animation)GetValue(CurrentAnimationProperty);
            set => SetValue(CurrentAnimationProperty, value);
        }

        public static readonly DependencyProperty CurrentAnimationProperty = DependencyProperty.Register(nameof(CurrentAnimation), typeof(Animation), typeof(AnimationEditor), new UIPropertyMetadata(null, new PropertyChangedCallback(OnAnimationChanged)));

        private static void OnAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimationEditor animEditor)
            {
                animEditor.AnimationChanged();
            }
        }

        #endregion

        private readonly Line currentFrameLine;
        private int _length = 0;
        private double _unitSize = 10.0;
        private bool isFullyZoomedOut = false;
        private string _searchFilter = string.Empty;
        private AnimationEditorMode _editorMode = AnimationEditorMode.Timeline;

        public AnimationEditorMode EditorMode
        {
            get => _editorMode;
            set
            {
                if(_editorMode != value)
                {
                    _editorMode = value;
                    Rows.Clear();
                    InitControl();
                    NotifyPropertyChanged(nameof(EditorMode));
                    NotifyPropertyChanged(nameof(RowSize));
                }
            }
        }
        public int RowSize => EditorMode == AnimationEditorMode.Curve ? 160 : 40; //Height of each row in pixels
        public int KeyframeSize => 5; //Size of keyframe diamond (UnitSize cant be less than this)
        public double UnitSize
        {
            get => _unitSize;
            set
            {
                UpdateUnitSize(value);
            }
        }
        public int Length
        {
            get => CurrentAnimation?.EndFrame ?? 0;
            set
            {
                if (CurrentAnimation != null && _length != CurrentAnimation.EndFrame)
                {
                    UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(CurrentAnimation.EndFrame), CurrentAnimation, CurrentAnimation.EndFrame, value, "Animation Duration"));
                    CurrentAnimation.EndFrame = value;
                    NotifyPropertyChanged(nameof(Length));
                    NotifyPropertyChanged(nameof(CurrentWidth));
                }
            }
        }
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (_searchFilter != value)
                {
                    _searchFilter = value;
                    NotifyPropertyChanged(nameof(SearchFilter));
                }
            }
        }

        public double CurrentWidth { get; set; } = 100;
        public double CurrentHeight => validNodes.Count * RowSize;
        public int SelectedFrame { get; set; } //Hook this up to the actual animation context

        private AnimationBone _selectedBone = null;
        private AnimationComponent _selectedComponent = null;
        public AnimationBone SelectedBone
        {
            get => _selectedBone;
            set
            {
                if (_selectedBone != value)
                {
                    _selectedBone = value;
                    NotifyPropertyChanged(nameof(SelectedBone));

                    _selectedComponent = null;
                    NotifyPropertyChanged(nameof(SelectedComponent));
                }
            }
        }
        public AnimationComponent SelectedComponent
        {
            get => _selectedComponent;
            set
            {
                if (_selectedComponent != value)
                {
                    _selectedComponent = value;
                    NotifyPropertyChanged(nameof(SelectedComponent));

                    //Update selected bone to components parent
                    if(value != null && value.ParentNode != SelectedBone)
                    {
                        _selectedBone = value.ParentNode as AnimationBone;
                        NotifyPropertyChanged(nameof(SelectedBone));
                    }
                }
            }
        }

        public List<IAnimationNode> validNodes { get; private set; } = new List<IAnimationNode>();
        public ObservableCollection<AnimationBone> Bones { get; private set; } = new ObservableCollection<AnimationBone>();
        public ObservableCollection<AnimationNodeRow<IAnimationNode>> Rows { get; private set; } = new ObservableCollection<AnimationNodeRow<IAnimationNode>>();
        public ObservableCollection<Grid> RowsTest { get; private set; } = new ObservableCollection<Grid>();

        private double scrollHeight = double.MinValue;

        public AnimationEditor()
        {
            InitializeComponent();
            currentFrameLine = new Line();
            currentFrameLine.Stroke = Brushes.White;
            currentFrameLine.StrokeThickness = 4;
            currentFrameLine.Y1 = -20;
            currentFrameLine.Y2 = 20;

            for (int i = 0; i < 50; i++)
            {
                Brush color = (i % 2 == 0) ? Brushes.Blue : Brushes.Red;
                Grid grid = new Grid();
                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                grid.Height = RowSize;
                grid.Background = color;
                RowsTest.Add(grid);

            }

            InitControl();

        }

        private void AnimationChanged()
        {
            SelectedBone = null;
            SelectedComponent = null;
            Rows.Clear();
            InitControl();
        }

        #region Rows
        private void UpdateRows()
        {
            if(CurrentAnimation == null)
            {
                validNodes.Clear();
                Rows.Clear();
                return;
            }

            GetValidRows();

            //Remove rows that are no longer valid
            for (int i = Rows.Count - 1; i >= 0; i--)
            {
                if (!validNodes.Contains(Rows[i].Node))
                    Rows.Remove(Rows[i]);
            }

            //Add/Insert rows that are valid but not in the current list
            for (int i = 0; i < validNodes.Count; i++)
            {
                if(Rows.Count <= i)
                {
                    Rows.Add(CreateNodeRow(validNodes[i]));
                }
                else if (Rows[i].Node != validNodes[i])
                {
                    Rows.Insert(i, CreateNodeRow(validNodes[i]));
                }
            }

            NotifyPropertyChanged(nameof(CurrentHeight));
        }

        private void GetValidRows()
        {
            validNodes.Clear();
            if (CurrentAnimation == null) return;
            string[] searchTerms = _searchFilter.ToLower().Split(';');

            foreach (var bone in CurrentAnimation.Bones)
            {
                //if (!string.IsNullOrWhiteSpace(_searchFilter) && !bone.Name.ToLower().Contains(searchFilter)) continue;
                string boneName = bone.Name.ToLower();
                if (searchTerms.Any(x => boneName.Contains(x)) || searchTerms.Length == 0)
                {
                    validNodes.Add(bone);

                    if (bone.IsExpanded)
                    {
                        foreach (var component in bone.Components)
                        {
                            validNodes.Add(component);

                            if (component.IsExpanded)
                            {
                                foreach (var channel in component.Channels)
                                {
                                    validNodes.Add(channel);
                                }
                            }
                        }
                    }
                }

            }
        }

        private AnimationNodeRow<IAnimationNode> CreateNodeRow(IAnimationNode node)
        {
            AnimationNodeRow<IAnimationNode> nodeRow = new AnimationNodeRow<IAnimationNode>(this, node);
            nodeRow.ParentItemsControl = itemsControl;
            nodeRow.Height = RowSize;
            nodeRow.HorizontalAlignment = HorizontalAlignment.Left;
            nodeRow.ItemTemplate = Resources["UsedTemplateProperty"] as DataTemplate;
            nodeRow.Background = Brushes.Transparent;

            return nodeRow;
        }
        #endregion

        #region Control Updating / Visuals
        public void InitControl()
        {
            UpdateRows();
            UpdateBones();
            UpdateUnitSize(isFullyZoomedOut ? 1 : UnitSize, true);
            timeScroll.ScrollToHorizontalOffset(mainScroll.HorizontalOffset);
        }

        private void UpdateUnitSize(double value, bool forceUpdate = false)
        {
            double minUnitSize = MathHelpers.Clamp(EditorMode == AnimationEditorMode.Curve ? 20 : 1, 100, mainScroll.ActualWidth / Length);
            double newUnitSize = MathHelpers.Clamp(minUnitSize, 100.0, value);

            if (_unitSize != newUnitSize || forceUpdate)
            {
                _unitSize = newUnitSize;
                CurrentWidth = (Length + 1) * newUnitSize;
                NotifyPropertyChanged(nameof(UnitSize));
                NotifyPropertyChanged(nameof(CurrentWidth));
                ResizeAllRows(); //must be AFTER the notify events for UnitSize/CurrentWidth; TODO figure out why
            }
            else
            {
                CurrentWidth = (Length + 1) * _unitSize;
                NotifyPropertyChanged(nameof(CurrentWidth));
            }

            isFullyZoomedOut = minUnitSize == _unitSize;

            //Log.Add($"UnitSize: {_unitSize}, Width: {CurrentWidth}, zoomedOut: {isFullyZoomedOut}");
        }

        private void ResizeAllRows()
        {
            foreach (var row in Rows)
            {
                row.UpdateUnitSize();
                row.Width = CurrentWidth;
            }

            DrawFrameMarkings();

            //Lines:
            const double FrameFadeOutStart = 40;
            const double FrameFadeOutEnd = 5;
            const double SecondFadeInStart = 30;
            const double SecondFadeInEnd = 0;

            //Calculate fade out/in oppacities for the frame and second lines
            if (UnitSize > FrameFadeOutStart && UnitSize <= 100)
            {
                canvasForFrameLines.Opacity = 1f;
            }
            else if (UnitSize <= FrameFadeOutStart && UnitSize > FrameFadeOutEnd)
            {
                double fadeLength = FrameFadeOutStart - FrameFadeOutEnd;
                double factor = 1.0 - (Math.Abs(UnitSize - FrameFadeOutStart) / fadeLength);
                canvasForFrameLines.Opacity = MathHelpers.Clamp(0.0001f, 1f, (1f * factor));
            }
            else
            {
                //DO NOT set to 0, or the scroll viewer will break... NO idea why
                canvasForFrameLines.Opacity = 0.0001f;
            }

            if (UnitSize > SecondFadeInStart)
            {
                canvasForSecondLines.Opacity = 0.0000001f;
            }
            else if (UnitSize <= SecondFadeInStart && UnitSize > SecondFadeInEnd)
            {
                double fadeLength = SecondFadeInStart - SecondFadeInEnd;
                double factor = Math.Abs(UnitSize - SecondFadeInStart) / fadeLength;
                canvasForSecondLines.Opacity = (0.7f * factor);
            }
            else
            {
                canvasForSecondLines.Opacity = 0.7f;
            }

            DrawFrameLines(canvasForFrameLines, 1, UnitSize, 1, UnitSize, Brushes.Gray, 1);
            DrawFrameLines(canvasForSecondLines, 60, UnitSize * 60, 60, UnitSize * 60, Brushes.LightGray, 2);
        }

        private void DrawFrameLines(Canvas canvas, int firstLineTime, double firstLineDistance, int timeStep, double unitSize, Brush brush, int lineThickness)
        {
            if (Rows.Count == 0 || canvas.Opacity == 0)
            {
                canvas.Children.Clear();
                return;
            }

            canvas.Width = CurrentWidth;
            canvas.Children.Clear();

            double curX = firstLineDistance;
            int curDate = firstLineTime;
            int curLine = 0;

            while (curX < canvas.Width)
            {
                Line l = new Line();
                l.StrokeThickness = lineThickness;
                l.Stroke = brush;
                l.X1 = 0;
                l.X2 = 0;
                l.Y1 = 0;
                l.Y2 = canvas.Height;
                canvas.Children.Add(l);
                Canvas.SetLeft(l, curX);
                curX += unitSize;
                curDate += timeStep;
                curLine++;
            }
        }

        private void DrawFrameMarkings()
        {
            Canvas canvas = canvasForFrameMarkings;
            canvas.Children.Clear();
            canvas.Children.Add(currentFrameLine);

            if (Rows.Count == 0)
            {
                return;
            }

            double curX = UnitSize;
            int currentFrame = 1;
            int curLine = 0;

            while (curX < canvas.Width)
            {
                //Conditionally write numbers based on the current zoom level
                bool writeNumber = false;
                bool isBold = (currentFrame / 60f == (int)(currentFrame / 60));
                Log.Add("UnitSize " + UnitSize);
                if (UnitSize > 22)
                {
                    //Show frame number each frame
                    writeNumber = true;
                }
                else if (UnitSize >= 5)
                {
                    //Show frame number each 5 frames
                    if (currentFrame / 5f == (int)(currentFrame / 5))
                    {
                        writeNumber = true;
                    }
                }
                else
                {
                    //Show frame number each 60 frames
                    if (currentFrame / 60f == (int)(currentFrame / 60))
                    {
                        writeNumber = true;
                    }
                }

                if (writeNumber)
                {
                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = currentFrame.ToString();
                    textBlock.FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal;
                    canvas.Children.Add(textBlock);
                    Canvas.SetLeft(textBlock, curX);

                }

                curX += UnitSize;
                currentFrame++;
                curLine++;
            }

            UpdateCurrentFrame();
        }

        private void UpdateCurrentFrame()
        {
            Canvas.SetLeft(currentFrameLine, SelectedFrame * UnitSize);
            NotifyPropertyChanged(nameof(SelectedFrame));
        }

        #endregion

        #region Events
        //Scrolling
        private void ExternalScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            timeScroll.ScrollToHorizontalOffset(mainScroll.HorizontalOffset);

            if(!MathHelpers.FloatEquals(e.VerticalChange, 0.0))
            {
                mainScroll.ScrollToVerticalOffset(e.VerticalOffset);
                dataGridScroll.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }

        private void dataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            //Handle when scroll happens over the DataGrid, making sure both the DataGrid and main scrollviewer scroll
            double newOffset = mainScroll.VerticalOffset - e.Delta / 3;
            mainScroll.ScrollToVerticalOffset(newOffset);
            dataGridScroll.ScrollToVerticalOffset(newOffset);
            e.Handled = true;
        }

        //Row Details visibility
        private void dataGrid_RowDetailsVisibilityChanged(object sender, DataGridRowDetailsEventArgs e)
        {
            if(e.Row.Item is IAnimationNode node)
            {
                node.IsExpanded = e.Row.DetailsVisibility == Visibility.Visible;
                UpdateRows();
                UpdateUnitSize(_unitSize, true);
            }
        }

        private void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if(e.Row.DataContext is IAnimationNode node)
            {
                e.Row.DetailsVisibility = node.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void mainScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                //Zoom
                int factor = UnitSize <= 10 ? 100 : 50;
                UnitSize += e.Delta / factor;
                timeScroll.ScrollToHorizontalOffset(mainScroll.HorizontalOffset);

                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                //Horizontal scrolling
                mainScroll.ScrollToHorizontalOffset(mainScroll.HorizontalOffset + (-e.Delta * 4f));
                timeScroll.ScrollToHorizontalOffset(mainScroll.HorizontalOffset);
                e.Handled = true;
            }
        }

        #endregion

        #region DragSelection
        private bool leftMouseDown = false;
        private Point mouseDownPos;

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Capture and track the mouse.
                leftMouseDown = true;
                mouseDownPos = e.GetPosition(mainScollGrid);
                mainScollGrid.CaptureMouse();

                // Initial placement of the drag selection box.         
                Canvas.SetLeft(selectionBox, mouseDownPos.X);
                Canvas.SetTop(selectionBox, mouseDownPos.Y);
                selectionBox.Width = 0;
                selectionBox.Height = 0;

                // Make the drag selection box visible.
                selectionBox.Visibility = Visibility.Visible;
            }
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (leftMouseDown)
            {
                // Release the mouse capture and stop tracking it.
                leftMouseDown = false;
                mainScollGrid.ReleaseMouseCapture();

                // Hide the drag selection box.
                selectionBox.Visibility = Visibility.Collapsed;

                Point mouseUpPos = e.GetPosition(mainScollGrid);
                Rect rect = new Rect(mouseDownPos, mouseUpPos);

                /*
                //Check if any items in any layers should be selected
                List<ITimeLineItem> newSelectedItems = new List<ITimeLineItem>();

                foreach (TimeLineLayer<IBacType> layer in Layers)
                {
                    foreach (object item in layer.Children)
                    {
                        if (item is TimeLineItemControl timelineItem)
                        {
                            var point = timelineItem.TransformToAncestor(itemsControl).Transform(default);

                            if (rect.Contains(point))
                            {
                                //if(timelineItem.DataContext is ITimeLineItem itm)
                                //{
                                //    Log.Add($"Selected Type {itm.LayerGroup}, with StartTime: {itm.TimeLine_StartTime}");
                                //}
                                if (timelineItem.DataContext is ITimeLineItem itm)
                                {
                                    newSelectedItems.Add(itm);
                                }
                            }
                        }
                    }
                }

                SetSelectedItems(newSelectedItems, Keyboard.IsKeyDown(Key.LeftCtrl));
                */
            }
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (leftMouseDown)
            {
                // When the mouse is held down, reposition the drag selection box.

                Point mousePos = e.GetPosition(mainScollGrid);

                if (mouseDownPos.X < mousePos.X)
                {
                    Canvas.SetLeft(selectionBox, mouseDownPos.X);
                    selectionBox.Width = mousePos.X - mouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(selectionBox, mousePos.X);
                    selectionBox.Width = mouseDownPos.X - mousePos.X;
                }

                if (mouseDownPos.Y < mousePos.Y)
                {
                    Canvas.SetTop(selectionBox, mouseDownPos.Y);
                    selectionBox.Height = mousePos.Y - mouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(selectionBox, mousePos.Y);
                    selectionBox.Height = mouseDownPos.Y - mousePos.Y;
                }
            }
        }
        #endregion

        #region BoneFilter
        public RelayCommand SearchCommand => new RelayCommand(ApplyBoneFilter);
        public RelayCommand ClearSearchCommand => new RelayCommand(ClearSearch);
        private void ClearSearch()
        {
            SearchFilter = string.Empty;
            UpdateRows();
            UpdateBones();
            UpdateUnitSize(_unitSize, true);
        }

        private void ApplyBoneFilter()
        {
            UpdateRows();
            UpdateBones();
            UpdateUnitSize(_unitSize, true);
        }

        private void UpdateBones()
        {
            //Update bone list
            Bones.Clear();

            foreach (var node in validNodes)
            {
                if (node is AnimationBone bone)
                    Bones.Add(bone);
            }
        }
        #endregion

    }

    public enum AnimationEditorMode
    {
        Timeline,
        Curve
    }
}
