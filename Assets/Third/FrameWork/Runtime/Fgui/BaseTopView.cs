using FairyGUI;

namespace siliu
{
    public class BaseTopView<T> : BaseView<T> where T : GComponent
    {
        protected override void AddToRoot(GObject popup)
        {
            view.sortingOrder = 100;
            base.AddToRoot(popup);
        }
    }
}
