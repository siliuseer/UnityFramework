using System;
using UnityEngine;
using UnityWebSocket;

namespace siliu.net
{
    public class WebSocketRequest : IRequest
    {
        private WebSocket webSocket;

        private const int SOCKET_RECEIVE_HEAD_LEN = 7;
        private ByteStream streamSend = new ByteStream();
        private ByteStream streamReceive = new ByteStream();
        private Action<SocketEvent> _connectEvent;
        private Action<BaseDownEntry> _onReceive;
        
        public WebSocketRequest(Action<SocketEvent> connectCallback, Action<BaseDownEntry> receiveCallback)
        {
            _connectEvent = connectCallback;
            _onReceive = receiveCallback;
        }

        ~WebSocketRequest()
        {
            Close();
            streamSend?.Close();
            streamSend = null;
            lock (streamReceive)
            {
                streamReceive?.Close();
                streamReceive = null;
            }
            _connectEvent = null;
            _onReceive = null;
        }
        public bool IsConnected()
        {
            return webSocket?.ReadyState == WebSocketState.Open;
        }

        public void Send(SendEntry data)
        {
            if (data == null) return;

            if (!IsConnected())
            {
                Debug.Log("Network Error, Can't Send Data");
                return;
            }

            lock (streamSend)
            {
                var body = data.bytes;
                var len = 4;
                if (body != null)
                {
                    len += body.Length;
                }

                streamSend.Clear();
                streamSend.WritUShort(len);
                streamSend.WritUShort(data.proto);
                streamSend.WritUShort(data.flag);
                if (body != null)
                {
                    streamSend.Write(body, 0, body.Length);
                }

                streamSend.Flush();

                var bytes = streamSend.ToArray();
                // Debug.Log("发送数据长度: "+bytes.Length);
                // Debug.Log("发送数据: "+BitConverter.ToString(bytes).Replace('-', ' '));

                if (IsConnected())
                {
                    webSocket.SendAsync(bytes);
                }
                else
                {
                    Debug.Log("Network Error, Can't Send Data");
                }
            }
        }

        public void Connect(string wss)
        {
            try
            {
                Close();
                Debug.Log($"try connect {wss}");
                webSocket = new WebSocket(wss);
                webSocket.OnOpen += OnOpen;
                webSocket.OnMessage += OnMessage;
                webSocket.OnError += OnError;
                webSocket.OnClose += OnClose;
                webSocket.ConnectAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                _connectEvent?.Invoke(SocketEvent.ConnectFail);
            }
        }

        public void Close()
        {
            webSocket?.CloseAsync();
            webSocket = null;
        }
        
        private void OnOpen(object sender, EventArgs args)
        {
            _connectEvent?.Invoke(SocketEvent.ConnectSuccess);
        }

        private void OnError(object sender, ErrorEventArgs args)
        {
            Debug.Log("websocket error: "+args.Message);
        }

        private void OnClose(object sender, CloseEventArgs args)
        {
            _connectEvent?.Invoke(SocketEvent.Disconnect);
        }

        private void OnMessage(object sender, MessageEventArgs args)
        {
            if (!args.IsBinary)
            {
                return;
            }

            var bytes = args.RawData;
            lock (streamReceive)
            {
                streamReceive.Append(bytes, 0, bytes.Length);
            }
            try
            {
                DealReceive();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        
        private void DealReceive()
        {
            lock (streamReceive)
            {
                streamReceive.SeekToBegin();
                while (streamReceive.Available >= SOCKET_RECEIVE_HEAD_LEN)
                {
                    int len = streamReceive.ReadUShort();
                    if (len - 2 > streamReceive.Available)
                    {
                        //如果不够消息长度,回退读取长度的2字节
                        streamReceive.SeekOffset(-2);
                        break;
                    }

                    var protocol = streamReceive.ReadUShort(); //协议号
                    var flag = streamReceive.ReadUShort(); //标记
                    var result = streamReceive.ReadSbyte(); //处理结果
                    if (AppCfg.debug && protocol != 100)
                    {
                        Debug.Log($"receive proto: {protocol}, flag: {flag}, result: {result}, msg len: {len}");
                    }

                    byte[] bytes;
                    if (result < 0)
                    {
                        var s = streamReceive.ReadShort();
                        bytes = BitConverter.GetBytes(s);
                    }
                    else
                    {
                        bytes = streamReceive.ReadBytes(len - SOCKET_RECEIVE_HEAD_LEN);
                    }

                    var down = NetMgr.FindDown(protocol);
                    if (down == null)
                    {
                        //Debug.LogWarning("找不到处理下行: " + protocol);
                        continue;
                    }

                    var entry = down.CreateEntry();
                    entry.proto = protocol.ToString();
                    entry.flag = flag;
                    entry.DealReceive(result, bytes);
                    _onReceive?.Invoke(entry);
                }

                //将剩余数据移动到流开头,并调整数据流大小
                streamReceive.ConvertToAvailable();
            }
        }
    }
}