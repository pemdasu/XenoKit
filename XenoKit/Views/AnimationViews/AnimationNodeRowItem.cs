using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xv2CoreLib.AnimationFramework;

namespace XenoKit.Views.AnimationViews
{
    public class AnimationNodeRowItem : Button
    {
        private AnimationEditor parentEditor;

        public int Frame { get; private set; }
        public IAnimationNode Node { get; private set; }

        private ContentPresenter _MouseOver;

        public AnimationNodeRowItem (AnimationEditor parentEditor, int frame, IAnimationNode node)
        {
            this.parentEditor = parentEditor;
            Node = node;
            Frame = frame;
            PlaceOnCanvas();
        }

        public void SetFrame(int frame)
        {
            Frame = frame;
            PlaceOnCanvas();
        }

        public override void OnApplyTemplate()
        {
            _MouseOver = Template.FindName("PART_MouseOver", this) as ContentPresenter;
            if (_MouseOver != null)
                _MouseOver.Visibility = Visibility.Collapsed;

            base.OnApplyTemplate();

        }

        public void ApplyContentTemplate(DataTemplate template)
        {
            ContentTemplate = template;
        }

        internal void PlaceOnCanvas()
        {
            double p = ConvertFrameToDistance();
            if (p >= 0)
            {
                Canvas.SetLeft(this, (int)p);

                if(parentEditor.EditorMode == AnimationEditorMode.Timeline)
                {
                    Canvas.SetTop(this, 0);
                }
                else if(parentEditor.EditorMode == AnimationEditorMode.Curve)
                {
                    Canvas.SetBottom(this, parentEditor.RowSize);
                }
            }
        }

        private double ConvertFrameToDistance()
        {
            return (double)(Frame * parentEditor.UnitSize);
        }

        #region MouseEvents
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            _MouseOver.Visibility = Visibility.Visible;
            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            _MouseOver.Visibility = Visibility.Visible;
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            _MouseOver.Visibility = Visibility.Collapsed;
            base.OnMouseLeave(e);
        }

        #endregion
    }
}
