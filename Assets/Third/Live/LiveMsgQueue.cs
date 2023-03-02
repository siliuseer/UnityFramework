using System.Collections.Generic;
using UnityEngine;

namespace siliu
{
    public abstract class LiveMsg
    {
        /// <summary>
        /// 用户唯一id
        /// </summary>
        public string uid;

        /// <summary>
        /// 用户昵称
        /// </summary>
        public string name;

        /// <summary>
        /// 用户头像
        /// </summary>
        public string icon;

        /// <summary>
        /// 消息id,抖音官方特有
        /// </summary>
        public string msgId;
    }

    public class LiveMsgDanMu : LiveMsg
    {
        public string msg;
    }

    public class LiveMsgGift : LiveMsg
    {
        public string gift;
        public long num;
    }

    public class LiveMsgLike : LiveMsg
    {
        public long num;
    }

    public interface ILiveMsgHandler
    {
        /// <summary>
        /// 弹幕处理
        /// </summary>
        /// <param name="msg"></param>
        void OnDanMu(LiveMsgDanMu msg);

        /// <summary>
        /// 礼物处理
        /// </summary>
        /// <param name="msg"></param>
        void OnGift(LiveMsgGift msg);

        /// <summary>
        /// 点赞处理
        /// </summary>
        /// <param name="msg"></param>
        void OnLike(LiveMsgLike msg);
    }

    public class LiveMsgQueue : MonoBehaviour
    {
        public ILiveMsgHandler Handler;
        private Queue<LiveMsg> queue = new Queue<LiveMsg>();

        public void Enqueue(LiveMsg msg)
        {
            lock (queue)
            {
                queue.Enqueue(msg);
            }
        }

        private void Update()
        {
            lock (queue)
            {
                while (queue.TryDequeue(out var msg))
                {
                    switch (msg)
                    {
                        case LiveMsgDanMu danMu:
                            Handler?.OnDanMu(danMu);
                            break;
                        case LiveMsgGift gift:
                            Handler?.OnGift(gift);
                            break;
                        case LiveMsgLike like:
                            Handler?.OnLike(like);
                            break;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            lock (queue)
            {
                queue.Clear();
            }
        }
    }
}