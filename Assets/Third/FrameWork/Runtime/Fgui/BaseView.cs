using FairyGUI;

namespace siliu
{
    /// <summary>
    /// UI界面基类
    /// </summary>
    /// <typeparam name="T">fgui组件</typeparam>
    public abstract class BaseView<T> : IView where T : GComponent
    {
        private readonly string _resUid;
        private readonly string _pkgName;
        private readonly string _resName;

        protected BaseView()
        {
            _resUid = typeof(T).FullName;
            if (string.IsNullOrEmpty(_resUid))
            {
                return;
            }

            var split = _resUid.Split('.');
            _pkgName = split[^2];
            _resName = split[^1];
            assetLoader = new AssetLoader();
            eventMesh = new EventMesh();
        }

        protected T view { get; private set; }
        public string uid => GetType().FullName;
        public string resUid => _resUid;
        protected readonly AssetLoader assetLoader;
        protected readonly EventMesh eventMesh;

        public void Create(GObject popup)
        {
            view = (T)UIPackage.CreateObject(_pkgName, _resName);
            view.fairyBatching = true;
            AddToRoot(popup);
        }

        /// <summary>
        /// 添加到场景
        /// </summary>
        protected virtual void AddToRoot(GObject popup)
        {
            var root = GRoot.inst;
            if (popup != null)
            {
                view.displayObject.onRemovedFromStage.Add(() => { UIMgr.Close(uid); });
                root.ShowPopup(view, popup);
                return;
            }

            view.SetSize(root.width, root.height);
            view.AddRelation(root, RelationType.Size);
            root.AddChild(view);
        }

        public void Show(params object[] args)
        {
            OnShow(args);
        }

        public virtual void Close()
        {
            eventMesh.Dispose();
            OnClose();
            assetLoader.Release();
            view.Dispose();
        }

        public void Refresh(params object[] args)
        {
            OnRefresh(args);
        }

        protected void CloseMySelf()
        {
            UIMgr.Close(uid);
        }

        /// <summary>
        /// 添加到场景之后调用
        /// </summary>
        protected virtual void OnShow(params object[] args)
        {
        }

        /// <summary>
        /// 刷新调用
        /// </summary>
        protected virtual void OnRefresh(params object[] args)
        {
        }

        /// <summary>
        /// 关闭之前调用
        /// </summary>
        protected virtual void OnClose()
        {
        }
    }
}