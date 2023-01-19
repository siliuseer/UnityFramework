using UnityEngine;
using FairyGUI;

namespace siliu
{
    /// <summary>
    /// Extend the ability of GLoader
    /// </summary>
    public class IconLoader : GLoader
    {
        protected override void LoadExternal()
        {
            IconManager.inst.LoadIcon(this.url, OnLoadSuccess, OnLoadFail);
        }

        protected override void FreeExternal(NTexture texture)
        {
            texture.refCount--;
        }

        void OnLoadSuccess(NTexture texture)
        {
            if (string.IsNullOrEmpty(this.url))
                return;

            this.onExternalLoadSuccess(texture);
        }

        void OnLoadFail(string error)
        {
            Debug.Log("load " + this.url + " failed: " + error);
            this.onExternalLoadFailed();
        }
    }
}
