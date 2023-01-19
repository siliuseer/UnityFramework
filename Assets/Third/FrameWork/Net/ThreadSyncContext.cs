using System;
using System.Collections.Concurrent;
using System.Threading;

namespace siliu
{
    public class ThreadSyncContext : SynchronizationContext
    {
        public static ThreadSyncContext Inst { get; } = new ThreadSyncContext();

        private readonly int _mainThreadId = Thread.CurrentThread.ManagedThreadId;

        // 线程同步队列,发送接收socket回调都放到该队列,由poll线程统一执行
        private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

        private Action _action;

        public void Update()
        {
            while (true)
            {
                if (!queue.TryDequeue(out _action))
                {
                    return;
                }
                _action();
            }
        }

        public override void Post(SendOrPostCallback callback, object data)
        {
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
            {
                callback(data);
                return;
            }

            queue.Enqueue(() => { callback(data); });
        }
    }
}