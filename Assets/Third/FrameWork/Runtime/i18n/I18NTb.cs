using System;
using System.Collections.Generic;
using UnityEngine;

namespace siliu.i18n
{
    [Serializable]
    public class I18NKey
    {
        public string tb;
        public string key;

        public string i18n => I18N.Find(tb, key);

        public override string ToString()
        {
            return i18n;
        }
    }
    [Serializable]
    public class I18NId
    {
        public string tb;
        public int id;

        public string i18n => I18N.Find(tb, id);

        public override string ToString()
        {
            return i18n;
        }
    }

    [Serializable]
    public class I18NEntry<T>
    {
        public string tb;
        public List<T> keys = new List<T>();
        public List<string> valus = new List<string>();

        public bool TryAdd(T key, string msg)
        {
            if (keys.Contains(key))
            {
                return false;
            }
            keys.Add(key);
            valus.Add(msg);
            return true;
        }

        public bool TryGetValue(T key, out string value)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                if (!keys[i].Equals(key)) continue;

                value = valus[i];
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
    public class I18NTb : ScriptableObject
    {
        public List<I18NEntry<string>> keys = new List<I18NEntry<string>>();
        public List<I18NEntry<int>> ids = new List<I18NEntry<int>>();

        public string Find(string tb, string key)
        {
            foreach (var entry in keys)
            {
                if (entry.tb != tb)
                {
                    continue;
                }

                return entry.TryGetValue(key, out var str) ? str : string.Empty;
            }

            return string.Empty;
        }

        public string Find(string tb, int key)
        {
            foreach (var entry in ids)
            {
                if (entry.tb != tb)
                {
                    continue;
                }

                return entry.TryGetValue(key, out var str) ? str : string.Empty;
            }

            return string.Empty;
        }

        public int Count(string tb, bool key)
        {
            if (key)
            {
                foreach (var entry in keys)
                {
                    if (entry.tb != tb)
                    {
                        continue;
                    }

                    return entry.keys.Count;
                }
            }
            else
            {
                foreach (var entry in ids)
                {
                    if (entry.tb != tb)
                    {
                        continue;
                    }

                    return entry.keys.Count;
                }
            }
            return 0;
        }

        public bool TryAdd(string tb, int key, string value)
        {
            I18NEntry<int> _entry = null;
            foreach (var entry in ids)
            {
                if (entry.tb != tb)
                {
                    continue;
                }

                _entry = entry;
                break;
            }

            if (_entry == null)
            {
                _entry = new I18NEntry<int>()
                {
                    tb = tb
                };
                ids.Add(_entry);
            }
            return _entry.TryAdd(key, value);
        }

        public bool TryAdd(string tb, string key, string value)
        {
            I18NEntry<string> _entry = null;
            foreach (var entry in keys)
            {
                if (entry.tb != tb)
                {
                    continue;
                }

                _entry = entry;
                break;
            }

            if (_entry == null)
            {
                _entry = new I18NEntry<string>
                {
                    tb = tb
                };
                keys.Add(_entry);
            }
            return _entry.TryAdd(key, value);
        }
    }
}