using UnityEngine;
using YooAsset;

namespace siliu.i18n
{
    public static class I18N
    {
        private static I18NTb _tb;

        public static void Init()
        {
            var i18n = (I18NType)PlayerPrefsUtil.GetInt("i18n");
            Load(i18n);
        }

        public static void Load(I18NType type)
        {
            var handle = YooAssets.LoadAssetSync<I18NTb>($"{AssetLoader.root}/i18n/{type}");
            if (handle.IsDone && handle.IsValid)
            {
                _tb = handle.GetAssetObject<I18NTb>();
            }
            else
            {
                _tb = ScriptableObject.CreateInstance<I18NTb>();
            }
        }
        public static string Find(string lang, string key)
        {
            return _tb.Find(lang, key);
        }

        public static string Find(string lang, int key)
        {
            return _tb.Find(lang, key);
        }

        public static int Count(string lang, bool key){
            return _tb.Count(lang, key);
        }
    }
}