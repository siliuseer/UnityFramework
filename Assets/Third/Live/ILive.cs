using UnityEngine;

namespace siliu
{
    public abstract class ILive
    {
        /// <summary>
        /// 主播信息
        /// </summary>
        public Anchor anchor { get; }
        /// <summary>
        /// 是否已经连接成功
        /// </summary>
        public bool IsLinked { get; protected set; }
        private LiveMsgQueue _queue;

        protected ILive()
        {
            anchor = new Anchor();
            
            var obj = new GameObject("[LiveMsgQueue]");
            Object.DontDestroyOnLoad(obj);
            _queue = obj.AddComponent<LiveMsgQueue>();
        }

        public void Start(string code, string pwd, string room = null)
        {
            anchor.uid = code;
            anchor.pwd = pwd;
            LinkStart();
        }

        public void SetMsgHandler(ILiveMsgHandler handler)
        {
            _queue.Handler = handler;
        }

        protected void EnqueueMsg(LiveMsg msg)
        {
            _queue.Enqueue(msg);
        }
        
        /// <summary>
        /// 销毁
        /// </summary>
        public void Dispose()
        {
            IsLinked = false;
            OnDispose();
        }
        /// <summary>
        /// 开始连接直播间
        /// </summary>
        protected abstract void LinkStart();
        protected abstract void OnDispose();
    }
}