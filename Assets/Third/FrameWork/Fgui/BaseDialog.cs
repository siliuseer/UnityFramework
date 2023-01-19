using FairyGUI;

namespace siliu
{
    /// <summary>
    /// 弹窗基类
    /// </summary>
    /// <typeparam name="T">fgui组件</typeparam>
    public abstract class BaseDialog<T> : BaseView<T> where T : GComponent
    {
        private Window _window;
        protected override void AddToRoot()
        {
            var root = GRoot.inst;
            _window = new Window {contentPane = view, modal = true};
            _window.CenterOn(root, true);
            view.displayObject.onRemovedFromStage.Add(() => UIMgr.Close(uid));
            _window.Show();
        }

        public override void Close()
        {
            _window.Hide();
            _window.Dispose();
            _window = null;
            base.Close();
        }
    }
}
