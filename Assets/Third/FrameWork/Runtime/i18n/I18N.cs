using UnityEngine;

namespace siliu.i18n
{
    public static class I18N
    {
        private static I18NTb _tb;
        private static AssetLoader loader;

        public static void Init()
        {
            loader = new AssetLoader();
            var i18n = (I18NType)PlayerPrefsUtil.GetInt("i18n");
            Load(i18n);
        }

        public static void Load(I18NType type)
        {
            loader.Release();
            if (_tb != null)
            {
                Object.DestroyImmediate(_tb);
            }
            var i18NTb = loader.Load<I18NTb>($"i18n/{type}");
            _tb = i18NTb == null ? ScriptableObject.CreateInstance<I18NTb>() : i18NTb;
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