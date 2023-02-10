using System;
using System.Collections.Generic;
using UnityEngine;

namespace siliu.tb
{
    [Serializable]
    public abstract class IBaseRow
    {
        public int id;
    }
    public abstract class IBaseTb : ScriptableObject
    {
        protected static AssetLoader Loader;
    }

    public class BaseTb<T, TRow> : IBaseTb where T : BaseTb<T, TRow> where TRow : IBaseRow
    {
        private static T _inst;
        public static T Inst
        {
            get
            {
                if (_inst != null) return _inst;
                
                Loader ??= new AssetLoader();
                _inst = Loader.Load<T>("tb/"+typeof(T).Name);
                return _inst;
            }
        }

        public List<TRow> Rows = new List<TRow>();
        
        public TRow Find(int id)
        {
            foreach (var row in Rows)
            {
                if (row.id == id)
                {
                    return row;
                }
            }

            return null;
        }
    }
}