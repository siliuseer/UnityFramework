using System;
using UnityEngine;
using UnityWebSocket;

namespace siliu.net
{
    public class WebSocketRequest : IRequest
    {
        private WebSocket webSocket;

        private const int MaxRead = 1024 * 5; //单条消息上下行最大字节数
        private const int SOCKET_RECEIVE_HEAD_LEN = 7;
        private readonly byte[] _byteBuffer = new byte[MaxRead]; //下行缓存
        private ByteStream streamSend = new ByteStream();
        private ByteStream streamReceive = new ByteStream();
        private Action<SocketRequest.SocketEvent> _connectEvent;
        private Action<BaseDownEntry> _onReceive;
        
        public WebSocketRequest(Action<SocketRequest.SocketEvent> connectCallback, Action<BaseDownEntry> receiveCallback)
        {
            _connectEvent = connectCallback;
            _onReceive = receiveCallback;
        }

        ~WebSocketRequest()
        {
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
            webSocket = new WebSocket(wss);
            webSocket.OnOpen += OnOpen;
            webSocket.OnMessage += OnMessage;
            webSocket.OnError += OnError;
            webSocket.OnClose += OnClose;
        }
        
        private void OnOpen(object sender, EventArgs args)
        {
        }

        private void OnError(object sender, UnityWebSocket.ErrorEventArgs args)
        {
        }

        private void OnClose(object sender, CloseEventArgs args)
        {
        }

        private void OnMessage(object sender, MessageEventArgs args)
        {
        }
    }
}