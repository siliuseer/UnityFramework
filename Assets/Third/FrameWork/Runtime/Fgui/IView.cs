using FairyGUI;

namespace siliu
{
    public interface IView
    {
        public string uid { get; }
        public string resUid { get; }

        public void Create(GObject popup);
        public void Show(params object[] args);
        public void Refresh(params object[] args);
        public void Close();
    }
}