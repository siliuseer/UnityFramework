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
        }
        protected T view { get; private set; }
        public override string uid => GetType().FullName;
        public override string resUid => _resUid;

        public override void Create()
        {
            view = (T)UIPackage.CreateObject(_pkgName, _resName);
            view.fairyBatching = true;
            AddToRoot();
        }

        /// <summary>
        /// 添加到场景
        /// </summary>
        protected virtual void AddToRoot()
        {
            var root = GRoot.inst;
            if (popup != null)
            {
                view.displayObject.onRemovedFromStage.Add(() =>
                {
                    UIMgr.Close(uid);
                });
                root.ShowPopup(view, popup);
                return;
            }
            
            view.SetSize(root.width, root.height);
            view.AddRelation(root, RelationType.Size);
            root.AddChild(view);
        }

        public override void Show()
        {
            OnShow();
        }

        public override void Close()
        {
            OnClose();
            view.Dispose();
        }
        public override void Refresh()
        {
            OnRefresh();
        }

        protected void CloseMySelf()
        {
            UIMgr.Close(uid);
        }

        /// <summary>
        /// 添加到场景之后调用
        /// </summary>
        protected virtual void OnShow() {}
        /// <summary>
        /// 刷新调用
        /// </summary>
        protected virtual void OnRefresh() {}
        /// <summary>
        /// 关闭之前调用
        /// </summary>
        protected virtual void OnClose() {}
    }
}
