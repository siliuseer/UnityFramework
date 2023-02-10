using System.Collections.Generic;
using UnityEngine;
using FairyGUI;
using YooAsset;

namespace siliu
{
    /// <summary>
    /// Extend the ability of GLoader
    /// </summary>
    public class IconLoader : GLoader
    {
        private static Dictionary<string, AssetOperationHandle> _handles = new Dictionary<string, AssetOperationHandle>();
        private static Dictionary<string, NTexture> _nTextures = new Dictionary<string, NTexture>();

        protected override void LoadExternal()
        {
            if (_nTextures.TryGetValue(url, out var nTexture))
            {
                nTexture.refCount++;
                onExternalLoadSuccess(nTexture);
                return;
            }

            var handle = YooAssets.LoadAssetSync<Sprite>($"{AssetLoader.root}/{url}");
            if (!handle.IsDone || !handle.IsValid)
            {
                handle.Release();
                onExternalLoadFailed();
                return;
            }

            var sprite = handle.GetAssetObject<Sprite>();
            _handles.Add(url, handle);
            nTexture = new NTexture(sprite);
            nTexture.refCount++;
            _nTextures.Add(url, nTexture);
            onExternalLoadSuccess(nTexture);
        }

        protected override void FreeExternal(NTexture nTexture)
        {
            nTexture.refCount--;
            if (nTexture.refCount > 0)
            {
                return;
            }

            _nTextures.Remove(url);
            
            if (!_handles.TryGetValue(url, out var handle))
            {
                return;
            }
            handle.Release();
            _handles.Remove(url);
        }
    }
}