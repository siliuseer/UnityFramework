using FairyGUI;

namespace siliu
{
    /// <summary>
    /// UI界面基类
    /// </summary>
    /// <typeparam name="T">fgui组件</typeparam>
    public abstract class BaseView<T> : IView where T : GComponent
    {
        protected BaseView()
        {
            uid = GetType().FullName;
            resUid = typeof(T).FullName;
            if (string.IsNullOrEmpty(resUid))
            {
                return;
            }

            assetLoader = new AssetLoader();
            eventMesh = new EventMesh();
        }

        ~BaseView()
        {
            assetLoader.Release();
            eventMesh.Dispose();
        }

        protected T view { get; private set; }

        protected readonly AssetLoader assetLoader;
        protected readonly EventMesh eventMesh;

        public string uid { get; }
        public string resUid { get; }
        public void Create(GObject popup)
        {
            var split = resUid.Split('.');
            var pkgName = split[^2];
            var resName = split[^1];
            view = (T)UIPackage.CreateObject(pkgName, resName);
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