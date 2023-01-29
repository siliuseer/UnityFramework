using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace siliu
{
    public class AssetLoader
    {
        public static string root { get; private set; }

        public static void Init(string dir)
        {
            root = dir;
        }
        // 资源句柄列表
        private List<AssetOperationHandle> _handles = new List<AssetOperationHandle>(100);
        
        private T Load<T>(string address) where T : Object
        {
            var handle = YooAssets.LoadAssetSync<T>($"{root}/{address}");
            _handles.Add(handle);
            return handle.GetAssetObject<T>();
        }
        
        private async Task<T> LoadAsync<T>(string address) where T : Object
        {
            var handle = YooAssets.LoadAssetAsync<T>($"{root}/{address}");
            _handles.Add(handle);
            
            await handle.Task;
            
            return handle.GetAssetObject<T>();
        }

        public static async Task<Scene> LoadSceneAsync(string address)
        {
            var handle = YooAssets.LoadSceneAsync($"{root}/scene/{address}");
            await handle.Task;
            return handle.SceneObject;
        }

        public Sprite LoadSprite(string address)
        {
            return Load<Sprite>(address);
        }

        public Task<Sprite> LoadSpriteAsync(string address)
        {
            return LoadAsync<Sprite>(address);
        }

        public GameObject LoadPrefabSync(string address)
        {
            return Load<GameObject>(address);

        }
        public async Task<GameObject> LoadPrefabAsync(string address)
        {
            var go = await LoadAsync<GameObject>(address);
            return go;
        }

        public async Task<FairyGUI.GoWrapper> LoadWrapperAsync(string address)
        {
            var go = await LoadPrefabAsync(address);
            return new FairyGUI.GoWrapper(go);
        }

        public async Task<FairyGUI.GGraph> CreateGraphAsync(string address)
        {
            var go = await LoadPrefabAsync(address);
            var wrapper = new FairyGUI.GoWrapper(go);
            var graph = new FairyGUI.GGraph();
            graph.SetSize(1,1);
            graph.SetNativeObject(wrapper);
            return graph;
        }
        
        // 释放资源句柄列表
        public void Release()
        {
            foreach(var handle in _handles)
            {
                handle.Release();
            }
            _handles.Clear();
        }
    }
}