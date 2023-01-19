using FairyGUI;

namespace siliu
{
    public class BaseTopView<T> : BaseView<T> where T : GComponent
    {
        protected override void AddToRoot()
        {
            view.sortingOrder = 100;
            base.AddToRoot();
        }
    }
}
