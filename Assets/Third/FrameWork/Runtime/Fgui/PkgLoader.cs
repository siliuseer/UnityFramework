using FairyGUI;

namespace siliu
{
    public class PkgLoader
    {
        private readonly AssetLoader _loader;

        private int count;
        private string pkg;

        public PkgLoader(string pkg)
        {
            _loader = new AssetLoader();
            this.pkg = pkg;
        }

        /// <summary>
        /// 加载包资源
        /// </summary>
        public void Load()
        {
            count++;
            UIPackage.AddPackage(pkg, LoadFunc);
        }

        /// <summary>
        /// 卸载包资源
        /// </summary>
        public void UnLoad()
        {
            count--;
        }

        /// <summary>
        /// 释放资源句柄列表
        /// </summary>
        public bool TryRelease()
        {
            if (count > 0)
            {
                return false;
            }
            UIPackage.RemovePackage(pkg);
            
            _loader.Release();
            return true;
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="name"></param>
        /// <param name="extension"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        private object LoadFunc(string name, string extension, System.Type type, out DestroyMethod method)
        {
            method = DestroyMethod.None; //注意：这里一定要设置为None
            return _loader.LoadByType($"fgui/{name}{extension}", type);
        }
    }
}