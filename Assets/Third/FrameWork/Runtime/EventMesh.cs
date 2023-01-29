using System;
using System.Collections.Generic;
using UnityEngine;

namespace siliu
{
    public class EventMesh
    {
        private static readonly List<EventMesh> Meshes = new List<EventMesh>();

        private interface IEventHandler
        {
            public void Trigger(object obj);
        }

        private class EventHandler<T> : IEventHandler
        {
            public Action<T> Action { get; }

            public EventHandler(Action<T> action)
            {
                Action = action;
            }

            public void Trigger(object obj)
            {
                try
                {
                    Action?.Invoke((T)obj);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        private readonly Dictionary<string, List<IEventHandler>> _entries;

        public EventMesh()
        {
            _entries = new Dictionary<string, List<IEventHandler>>();
            if (!Meshes.Contains(this))
            {
                Meshes.Add(this);
            }
        }

        public void Register<T>(Action<T> callback)
        {
            var key = typeof(T).FullName;
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!_entries.TryGetValue(key, out var handlers))
            {
                handlers = new List<IEventHandler>();
                _entries.Add(key, handlers);
            }

            foreach (var t in handlers)
            {
                if (t is not EventHandler<T> handler)
                {
                    continue;
                }

                if (handler.Action == callback)
                {
                    return;
                }
            }
            handlers.Add(new EventHandler<T>(callback));
        }

        public void Remove<T>(Action<T> callback)
        {
            var key = typeof(T).FullName;
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!_entries.TryGetValue(key, out var handlers))
            {
                return;
            }

            for (var i = 0; i < handlers.Count; i++)
            {
                if (handlers[i] is not EventHandler<T> handler)
                {
                    continue;
                }

                if (handler.Action != callback)
                {
                    continue;
                }

                handlers.RemoveAt(i);
                break;
            }
        }

        public void Clear<T>()
        {
            var key = typeof(T).FullName;
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (_entries.TryGetValue(key, out var handlers))
            {
                handlers.Clear();
            }
        }

        public static void Trigger<T>(T obj)
        {
            var key = typeof(T).FullName;
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            foreach (var mesh in Meshes)
            {
                if (!mesh._entries.TryGetValue(key, out var handlers))
                {
                    continue;
                }
                
                foreach (var handler in handlers)
                {
                    handler.Trigger(obj);
                }
            }
        }

        public void Dispose()
        {
            _entries.Clear();
            Meshes.Remove(this);
        }
    }
}