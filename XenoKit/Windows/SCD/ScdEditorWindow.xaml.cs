using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using System.Windows;
using XenoKit.Engine;
using XenoKit.Views;
using Xv2CoreLib;

namespace XenoKit.Windows
{
    public partial class ScdEditorWindow : MetroWindow
    {
        public ScdView Editor { get; }
        public RelayCommand SaveCommand => new RelayCommand(Editor.Save, Editor.CanSave);

        public ScdEditorWindow(Xv2PartSetFile file, Actor actor)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            Title = $"SCD Editor ({file.Name})";
            Editor = new ScdView(file, actor);
            editorContent.Content = Editor;
            DataContext = this;
            Activated += (sender, args) => Editor.ActivatePreview();
            Closed += (sender, args) =>
            {
                if (SceneManager.ScdEditor == Editor) SceneManager.ScdEditor = null;
            };
        }
    }
}
