using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using XenoKit.Editor;
using XenoKit.Views.TimeLines;
using Xv2CoreLib.AnimationFramework;

namespace XenoKit.Views.AnimationViews
{
    public class AnimationNodeRow<T> : Canvas where T : IAnimationNode
    {
        private readonly AnimationEditor ParentEditor;
        public ItemsControl ParentItemsControl { get; set; }

        private double _bumpThreshold = 1.5;
        private Line _seperator;

        #region DPs
        private DataTemplate _template;
        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(AnimationNodeRow<T>), new UIPropertyMetadata(null, new PropertyChangedCallback(OnItemTemplateChanged)));
        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AnimationNodeRow<T> animNodeRow = d as AnimationNodeRow<T>;
            if (animNodeRow != null)
            {
                animNodeRow.SetTemplate(e.NewValue as DataTemplate);
            }
        }

        public TimeLineManipulationMode ManipulationMode
        {
            get => (TimeLineManipulationMode)GetValue(ManipulationModeProperty);
            set => SetValue(ManipulationModeProperty, value);
        }

        public static readonly DependencyProperty ManipulationModeProperty = DependencyProperty.Register(nameof(ManipulationMode), typeof(TimeLineManipulationMode), typeof(AnimationNodeRow<T>), new UIPropertyMetadata(TimeLineManipulationMode.Free));
        #endregion

        public IAnimationNode Node { get; private set; }
        private IEnumerable<int> Keyframes;
        private List<int> keyframeIndices = new List<int>();

        public AnimationNodeRow(AnimationEditor parent, IAnimationNode node)
        {
            ParentEditor = parent;
            _seperator = new Line();
            Children.Add(_seperator);
            Focusable = true;
            Height = parent.RowSize;
            Node = node;
            Keyframes = node.GetAllKeyframes();
            InitializeItems();
            //KeyDown += OnKeyDown;
            //KeyUp += OnKeyUp;
        }

        public void UpdateUnitSize()
        {
            if (Keyframes == null || Node == null)
                return;

            int i = 1;
            foreach(var frame in Keyframes)
            {
                AnimationNodeRowItem ctrl = GetNodeRowItemAt(i);

                if (ctrl != null)
                {
                    if(ctrl.Frame != frame)
                    {
                        Log.Add("AnimationNodeRow: Frame does not match frame on NodeRowItem!", LogType.Debug);
                    }
                    ctrl.PlaceOnCanvas();
                }

                i++;
            }

            DrawSeperatorLine();
        }

        private void DrawSeperatorLine()
        {
            if (!Children.Contains(_seperator))
                Children.Add(_seperator);

            _seperator.Stroke = Brushes.Gray;
            _seperator.StrokeThickness = 1;
            _seperator.Y1 = Height - 1;
            _seperator.Y2 = Height - 1;
            _seperator.X1 = 0;
            _seperator.X2 = ParentEditor.CurrentWidth;

            Canvas.SetLeft(_seperator, 0);
        }

        private void InitializeItems()
        {
            if (Keyframes == null || Node == null) return;

            Children.Clear();
            Children.Add(_seperator);

            foreach (int frame in Keyframes)
            {
                AnimationNodeRowItem adder = CreateRowItemControl(frame, Node);

                Children.Add(adder);
            }
        }

        private AnimationNodeRowItem CreateRowItemControl(int frame, IAnimationNode node)
        {
            AnimationNodeRowItem adder = new AnimationNodeRowItem(ParentEditor, frame, node);
            adder.Opacity = 1f;
            adder.DataContext = node;

            if (_template != null)
            {
                adder.ApplyContentTemplate(_template);
            }

            //adder.PreviewMouseLeftButtonDown += Item_PreviewEditButtonDown;
            //adder.MouseMove += Item_MouseMove;
            //adder.PreviewMouseLeftButtonUp += Item_PreviewEditButtonUp;

            //adder.PreviewMouseRightButtonUp += Item_PreviewDragButtonUp;
            //adder.PreviewMouseRightButtonDown += Item_PreviewDragButtonDown;
            return adder;
        }

        #region Helpers
        private void SetTemplate(DataTemplate dataTemplate)
        {
            _template = dataTemplate;
            for (int i = 0; i < Children.Count; i++)
            {
                AnimationNodeRowItem titem = Children[i] as AnimationNodeRowItem;
                if (titem != null)
                    titem.ContentTemplate = dataTemplate;
            }
        }

        private AnimationNodeRowItem GetNodeRowItemAt(int i)
        {
            if (i >= Children.Count) return null;
            return Children[i] as AnimationNodeRowItem;
        }

        #endregion
    }
}
