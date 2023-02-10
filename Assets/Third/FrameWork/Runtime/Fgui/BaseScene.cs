using FairyGUI;

namespace siliu
{
    /// <summary>
    /// 场景UI基类, 打开时会关闭其他所有UI
    /// </summary>
    /// <typeparam name="T">fgui组件</typeparam>
    public abstract class BaseScene<T> : BaseView<T> where T : GComponent
    {
        protected override void AddToRoot(GObject popup)
        {
            UIMgr.CloseAll();
            base.AddToRoot(popup);
        }
    }
}
