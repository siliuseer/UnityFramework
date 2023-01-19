using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace siliu.tb
{
    [Serializable]
    public abstract class IBaseRow
    {
        public int id;
    }
    public abstract class IBaseTb : ScriptableObject
    {
    }

    public class BaseTb<T, TRow> : IBaseTb where T : BaseTb<T, TRow> where TRow : IBaseRow
    {
        private static T _inst;
        public static T Inst
        {
            get
            {
                if (_inst != null) return _inst;

                var h = YooAssets.LoadAssetSync<T>("tb/"+typeof(T).Name);
                if (!h.IsDone || !h.IsValid)
                {
                    Debug.LogWarning("Not Found TbData: " + typeof(T).FullName);
                }
                else
                {
                    _inst = h.GetAssetObject<T>();
                }

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