using FairyGUI;

namespace siliu
{
    public abstract class IView
    {
        public object[] args;
        public bool loaded;
        public GObject popup;
        
        public abstract string uid { get; }
        public abstract string resUid { get; }

        public abstract void Create();
        public abstract void Show();
        public abstract void Refresh();
        public abstract void Close();
    }
}