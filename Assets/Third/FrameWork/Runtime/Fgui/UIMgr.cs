using System.Collections.Generic;
using System.Linq;
using FairyGUI;
using fui;

namespace siliu
{
    public class UIMgr
    {
        private static readonly List<IView> Stack = new List<IView>();
        private static readonly List<ViewLoadData> Loadings = new List<ViewLoadData>();
        private static readonly Dictionary<string, PkgLoader> LoadedPkgs = new Dictionary<string, PkgLoader>();

        private class ViewLoadData
        {
            public IView view;
            public bool loaded;
            public GObject popup;
            public object[] args;
        }

        public static void Init(int designX, int designY)
        {
            // UIConfig.defaultFont = "GameFont";
            UIObjectFactory.SetLoaderExtension(typeof(IconLoader));
            GRoot.inst.SetContentScaleFactor(designX, designY);
            foreach (var pair in FuiCfg.Binders)
            {
                UIObjectFactory.SetPackageItemExtension(pair.Key, pair.Value);
            }
        }

        public static void Show<T>(params object[] args) where T : IView, new()
        {
            Popup<T>(null, args);
        }

        public static void Popup<T>(GObject target, params object[] args) where T : IView, new()
        {
            if (Refresh<T>(args))
            {
                return;
            }

            GRoot.inst.touchable = false;

            var data = new ViewLoadData
            {
                args = args,
                popup = target,
                loaded = false,
                view = new T(),
            };
            Loadings.Add(data);
            LoadPkg(data);
            for (var i = Loadings.Count - 1; i >= 0; i--)
            {
                var v = Loadings[i];
                if (v.view.uid == data.view.uid)
                {
                    v.loaded = true;
                }
            }

            ShowNext();
        }

        private static void LoadPkg(ViewLoadData data)
        {
            var pkg = data.view.resUid;
            var pkgs = new List<string>();
            if (FuiCfg.Depends.TryGetValue(pkg, out var depends))
            {
                pkgs.AddRange(depends);
            }

            foreach (var p in pkgs)
            {
                if (!LoadedPkgs.TryGetValue(p, out var loader))
                {
                    loader = new PkgLoader(p);
                    LoadedPkgs.Add(p, loader);
                }

                loader.Load();
            }
        }

        private static void ShowNext()
        {
            if (Loadings.Count <= 0)
            {
                GRoot.inst.touchable = true;
                return;
            }

            var data = Loadings[0];
            if (!data.loaded)
            {
                return;
            }

            Loadings.RemoveAt(0);
            var view = data.view;
            if (Refresh(view.uid, data.args))
            {
                return;
            }

            view.Create(data.popup);
            Stack.Add(view);
            view.Show(data.args);

            ShowNext();
        }

        public static bool Refresh<T>(params object[] args) where T : IView
        {
            var view = Find<T>();
            if (view == null)
            {
                return false;
            }

            view.Refresh(args);
            return true;
        }

        public static bool Refresh(string uid, params object[] args)
        {
            var view = Find(uid);
            if (view == null)
            {
                return false;
            }

            view.Refresh(args);
            return true;
        }

        public static IView Find(string uid)
        {
            foreach (var view in Stack)
            {
                if (view.uid == uid)
                {
                    return view;
                }
            }

            return null;
        }

        public static T Find<T>() where T : IView
        {
            var name = typeof(T).FullName;
            return (T)Find(name);
        }

        public static void Close<T>() where T : IView
        {
            Close(typeof(T).FullName);
        }

        public static void Close(string uid)
        {
            var ui = Find(uid);
            if (ui == null)
            {
                for (var i = Loadings.Count - 1; i >= 0; i--)
                {
                    var e = Loadings[i];
                    if (e.view.uid != uid) continue;

                    Loadings.RemoveAt(i);
                    return;
                }
            }

            for (var i = Stack.Count - 1; i >= 0; i--)
            {
                var e = Stack[i];
                if (e.uid != uid) continue;

                ui = e;
                Stack.RemoveAt(i);
                break;
            }

            if (ui == null)
            {
                return;
            }

            ui.Close();
            if (FuiCfg.Depends.TryGetValue(ui.resUid, out var depends))
            {
                foreach (var depend in depends)
                {
                    if (LoadedPkgs.TryGetValue(depend, out var loader))
                    {
                        loader.UnLoad();
                    }
                }
            }

            RemoveUnusedPkg();
        }

        public static void CloseAll(params string[] excepts)
        {
            var list = new List<string>();
            for (var i = Loadings.Count - 1; i >= 0; i--)
            {
                var e = Loadings[i];
                if (excepts != null && excepts.Contains(e.view.uid))
                {
                    continue;
                }

                list.Add(e.view.uid);
            }

            for (var i = Stack.Count - 1; i >= 0; i--)
            {
                var e = Stack[i];
                if (excepts != null && excepts.Contains(e.uid))
                {
                    continue;
                }

                list.Add(e.uid);
            }

            foreach (var uid in list)
            {
                Close(uid);
            }
        }

        public static void RemoveUnusedPkg()
        {
            if (Loadings.Count > 0)
            {
                return;
            }

            var list = new List<string>(LoadedPkgs.Keys);
            foreach (var key in list)
            {
                if (!LoadedPkgs.TryGetValue(key, out var loader))
                {
                    continue;
                }

                if (loader.TryRelease())
                {
                    LoadedPkgs.Remove(key);
                }
            }
        }
    }
}