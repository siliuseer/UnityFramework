using System;
using System.Text;
using Google.Protobuf;
using Newtonsoft.Json;
using UnityEngine;

namespace siliu.net
{
    public class BaseDownEntry
    {
        public string proto { get; set; }
        public int flag { get; set; }
        protected int _result { get; set; }

        protected object _data;

        public void DealReceive(int result, byte[] bytes)
        {
            _result = result;
            try
            {
                _data = Decode(result, bytes);
                if (AppCfg.debug && proto != "100")
                {
                    Debug.Log("receive["+proto+"] data: "+JsonConvert.SerializeObject(_data));
                }
            }
            catch (Exception e)
            {
                Debug.LogError("协议[" + proto + "]解析出错: " + e);
            }
        }

        protected virtual object Decode(int result, byte[] bytes)
        {
            return null;
        }

        public virtual void DealResult()
        {
        }
    }

    public abstract class DownEntry<T> : BaseDownEntry
    {
        public sealed override void DealResult()
        {
            if (_result < 0)
            {
                OnError((int)_data);
            }
            else
            {
                OnSuccess((T)_data);
            }
        }

        protected virtual void OnSuccess(T data)
        {
        }

        protected virtual void OnError(int msg)
        {
        }
    }

    public class DownJsonEntry<T> : DownEntry<T>
    {
        protected sealed override object Decode(int result, byte[] bytes)
        {
            if (result < 0)
            {
                return BitConverter.ToInt32(bytes);
            }

            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
        }
    }

    public class DownProtoEntry<T> : DownEntry<T> where T : IMessage, new()
    {
        protected sealed override object Decode(int result, byte[] bytes)
        {
            if (result < 0)
            {
                return BitConverter.ToInt32(bytes);
            }

            var msg = new T();
            msg.MergeFrom(bytes);
            return msg;
        }
    }
}