using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FairyGUI;
using YooAsset;

namespace siliu
{
    public delegate void LoadCompleteCallback(NTexture texture);
    public delegate void LoadErrorCallback(string error);

    /// <summary>
    /// Use to load icons from asset bundle, and pool them
    /// </summary>
    public class IconManager : MonoBehaviour
    {
        static IconManager _instance;
        public static IconManager inst
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("IconManager");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<IconManager>();
                }
                return _instance;
            }
        }

        public const int POOL_CHECK_TIME = 30;
        public const int MAX_POOL_SIZE = 10;

        List<LoadItem> _items;
        bool _started;
        Hashtable _pool;

        void Awake()
        {
            _items = new List<LoadItem>();
            _pool = new Hashtable();

            // StartCoroutine(FreeIdleIcons());
        }

        public void LoadIcon(string url, LoadCompleteCallback onSuccess, LoadErrorCallback onFail)
        {
            var item = new LoadItem
            {
                url = url,
                onSuccess = onSuccess,
                onFail = onFail
            };
            _items.Add(item);
            if (!_started)
                StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            _started = true;

            LoadItem item;
            while (true)
            {
                if (_items.Count == 0)
                {
                    break;
                }
                
                try
                {
                    item = _items[0];
                    _items.RemoveAt(0);

                    if (_pool.ContainsKey(item.url))
                    {
                        var temp = (NTexture)_pool[item.url];
                        temp.refCount++;
                        item.onSuccess?.Invoke(temp);
                        continue;
                    }

                    var texture2D = YooAssets.LoadAssetSync<Sprite>(item.url);
                    if (texture2D.Status != EOperationStatus.Succeed)
                    {
                        item.onFail?.Invoke(""+texture2D.Status);
                        continue;
                    }

                    var sprite = texture2D.GetAssetObject<Sprite>();
                    if (sprite == null)
                    {
                        item.onFail?.Invoke($"{item.url} sprite is null");
                        continue;
                    }

                    var texture = new NTexture(sprite);
                    texture.refCount++;

                    _pool[item.url] = texture;

                    item.onSuccess?.Invoke(texture);
                }
                catch (Exception e)
                {
                    Debug.LogError("[IconManager] error: "+e);
                }
                yield return 0;
            }

            _started = false;
        }

        IEnumerator FreeIdleIcons()
        {
            yield return new WaitForSeconds(POOL_CHECK_TIME); //check the pool every 30 seconds

            int cnt = _pool.Count;
            if (cnt > MAX_POOL_SIZE)
            {
                ArrayList toRemove = null;
                foreach (DictionaryEntry de in _pool)
                {
                    string key = (string)de.Key;
                    NTexture texture = (NTexture)de.Value;
                    if (texture.refCount == 0)
                    {
                        toRemove ??= new ArrayList();
                        toRemove.Add(key);
                        texture.Dispose();

                        cnt--;
                        if (cnt <= 8)
                            break;
                    }
                }
                if (toRemove != null)
                {
                    foreach (string key in toRemove)
                        _pool.Remove(key);
                }
            }
        }
    }

    class LoadItem
    {
        public string url;
        public LoadCompleteCallback onSuccess;
        public LoadErrorCallback onFail;
    }
}
