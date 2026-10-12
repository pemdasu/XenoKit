using XenoKit.Editor;
using XenoKit.Views;

namespace XenoKit
{
    public partial class MainWindow
    {
        public BpeView PostEffectsEditor => postEffectsContent.Content as BpeView;

        internal void LoadPostEffects()
        {
            PostEffectsEditor?.Preview.Clear();
            postEffectsContent.Content = new BpeView(Files.Instance.CommonBpeFile,
                Xv2CoreLib.FileManager.Instance.FileIO.PathInGameDir("pe/cmn.bpe"));
        }
    }
}
