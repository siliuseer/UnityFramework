using System.Collections.Generic;
using System.Threading.Tasks;
using FairyGUI;
using Unity.VisualScripting;
using UnityEngine;
using YooAsset;

namespace siliu
{
    public class PkgLoader
    {
        /// <summary>
        /// 资源句柄列表
        /// </summary>
        private readonly List<AssetOperationHandle> _handles = new List<AssetOperationHandle>(100);

        private int count;
        private string pkg;

        public PkgLoader(string pkg)
        {
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
            
            foreach (var handle in _handles)
            {
                handle.Release();
            }

            _handles.Clear();
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
            var location = $"{AssetLoader.root}/fgui/{name}{extension}";
            if (!YooAssets.CheckLocationValid(location))
            {
                return null;
            }
            var handle = YooAssets.LoadAssetSync(location, type);
            _handles.Add(handle);
            return handle.AssetObject;
        }
    }
}