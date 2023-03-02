using System;
using System.Collections.Generic;
using UnityEngine;

namespace siliu.net
{
    public class NetMgr : MonoBehaviour
    {
        private static NetMgr inst;

        public static NetMgr Inst
        {
            get
            {
                if (inst != null) return inst;

                var go = new GameObject("[NetMgr]");
                DontDestroyOnLoad(go);
                inst = go.AddComponent<NetMgr>();

                return inst;
            }
        }

        public static IDownCfg FindDown(int proto)
        {
            foreach (var cfg in ProtoReceive.cfgs)
            {
                if (cfg.proto == proto)
                {
                    return cfg;
                }
            }

            return null;
        }

        public static IDownCfg FindDown(string proto)
        {
            foreach (var cfg in ProtoReceive.cfgs)
            {
                if (cfg.protoStr == proto)
                {
                    return cfg;
                }
            }

            return null;
        }

        private HttpRequest _http;
        private WebSocketRequest _socket;
        private readonly List<SendEntry> _sendList = new List<SendEntry>();
        private readonly List<BaseDownEntry> _receiveList = new List<BaseDownEntry>();

        public Action SocketConnectSuccess;
        public Action SocketConnectFail;
        public Action SocketDisconnect;
        public Action Heart;
        private long _lastSendTime;
        private bool _socketConnected;

        public void ConnectWss(string wss)
        {
            _lastSendTime = ServerTime.Now;
            _socket.Connect(wss);
        }

        public void Send(SendEntry msg)
        {
            IRequest request = msg.http ? _http : _socket;
            if (!request.IsConnected())
            {
                Debug.Log($"断网无法发送消息: {msg.proto}");
                return;
            }

            lock (_sendList)
            {
                var flag = -1;
                foreach (var entry in _sendList)
                {
                    if (entry.proto != msg.proto)
                    {
                        continue;
                    }

                    if (!entry.repeat)
                    {
                        Debug.Log($"无法重复发送消息: {entry.proto}");
                        return;
                    }

                    flag = Math.Max(flag, entry.flag);
                }

                msg.flag = Math.Max(flag + 1, msg.flag);
                _sendList.Add(msg);
            }
        }

        private void Awake()
        {
            _http = new HttpRequest(AppCfg.url, OnReceive);
            _socket = new WebSocketRequest(e =>
            {
                switch (e)
                {
                    case SocketEvent.ConnectSuccess:
                    {
                        _lastSendTime = ServerTime.Now;
                        SocketConnectSuccess?.Invoke();
                        _socketConnected = true;
                        break;
                    }
                    case SocketEvent.ConnectFail:
                    {
                        _socketConnected = false;
                        SocketConnectFail?.Invoke();
                        break;
                    }
                    case SocketEvent.Disconnect:
                    {
                        _socketConnected = false;
                        SocketDisconnect?.Invoke();
                        break;
                    }
                }
            }, OnReceive);
        }

        private void OnReceive(BaseDownEntry down)
        {
            lock (_receiveList)
            {
                _receiveList.Add(down);
            }
        }

        private void Update()
        {
            var curTime = ServerTime.Now;
            lock (_sendList)
            {
                var remove = new List<int>();
                for (var i = 0; i < _sendList.Count; i++)
                {
                    var send = _sendList[i];
                    IRequest request = send.http ? _http : _socket;
                    if (request.IsConnected())
                    {
                        if (AppCfg.debug && send.proto != 100)
                        {
                            Debug.Log($"[{(send.http ? "http" : "socket")}] send: {(send.http ? send.protoStr : send.proto)}, flag: {send.flag}");
                        }
                        request.Send(send);
                        remove.Add(i);
                        if (!send.http)
                        {
                            _lastSendTime = curTime;
                        }
                    }
                }

                for (var i = remove.Count - 1; i >= 0; i--)
                {
                    _sendList.RemoveAt(remove[i]);
                }
            }

            lock (_receiveList)
            {
                foreach (var entry in _receiveList)
                {
                    try
                    {
                        entry.DealResult();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("协议[" + entry.proto + "]处理出错:" + e);
                    }
                }

                _receiveList.Clear();
            }

            if (!_socketConnected) return;

            if (curTime - _lastSendTime > 5000)
            {
                _lastSendTime = curTime;
                Heart?.Invoke();
            }
        }

        private void OnDestroy()
        {
            Debug.Log("[NetMgr] destroy");
            _socket?.Close();
            _socket = null;
        }
    }
}