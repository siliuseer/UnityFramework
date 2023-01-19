using System;
using System.Net.Sockets;
using UnityEngine;

namespace siliu.net
{
    public class SocketRequest : IRequest
    {
        public enum SocketEvent
        {
            /// <summary>
            /// 连接成功
            /// </summary>
            ConnectSuccess,

            /// <summary>
            /// 连接失败
            /// </summary>
            ConnectFail,

            /// <summary>
            /// 断线
            /// </summary>
            Disconnect,
        }

        private const int MaxRead = 1024 * 5; //单条消息上下行最大字节数
        private const int SOCKET_RECEIVE_HEAD_LEN = 7;

        private TcpClient _tcp;
        private readonly byte[] _byteBuffer = new byte[MaxRead]; //下行缓存
        private ByteStream streamSend = new ByteStream();
        private ByteStream streamReceive = new ByteStream();

        private Action<SocketEvent> _connectEvent;
        private Action<BaseDownEntry> _onReceive;

        public SocketRequest(Action<SocketEvent> connectCallback, Action<BaseDownEntry> receiveCallback)
        {
            _connectEvent = connectCallback;
            _onReceive = receiveCallback;
        }

        ~SocketRequest()
        {
            Debug.Log("SocketRequest close");
            Close();

            streamSend?.Close();
            streamSend = null;
            streamReceive?.Close();
            streamReceive = null;

            _connectEvent = null;
            _onReceive = null;
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="data">数据</param>
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
                    _tcp.GetStream().BeginWrite(bytes, 0, bytes.Length, OnAsyncSend, _tcp);
                }
                else
                {
                    Debug.Log("Network Error, Can't Send Data");
                }
            }
        }

        /// <summary>
        /// 向链接写入数据流
        /// </summary>
        private void OnAsyncSend(IAsyncResult r)
        {
            try
            {
                _tcp.GetStream().EndWrite(r);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("OnWrite--->>>" + ex.Message);
            }
        }

        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected()
        {
            return _tcp != null && _tcp.Connected;
        }

        public void Close()
        {
            if (IsConnected())
            {
                _tcp.Close();
            }

            _tcp = null;
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="ip">ip</param>
        /// <param name="port">端口</param>
        public void Connect(string ip, int port)
        {
            try
            {
                Close();
                Debug.Log($"try connect {ip}:{port}");
                _tcp = new TcpClient();
                _tcp.SendTimeout = 1000;
                _tcp.ReceiveTimeout = 1000;
                _tcp.NoDelay = true;
                _tcp.BeginConnect(ip, port, OnAsyncConnect, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                _connectEvent?.Invoke(SocketEvent.ConnectFail);
            }
        }

        private void OnAsyncConnect(IAsyncResult ar)
        {
            try
            {
                _tcp.EndConnect(ar);
                if (!_tcp.Connected)
                {
                    _connectEvent?.Invoke(SocketEvent.ConnectFail);
                    return;
                }

                _connectEvent?.Invoke(SocketEvent.ConnectSuccess);
                _tcp.GetStream().BeginRead(_byteBuffer, 0, MaxRead, OnAsyncRead, _tcp);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                _connectEvent?.Invoke(SocketEvent.ConnectFail);
            }
        }

        /// <summary>
        /// 读取消息
        /// </summary>
        private void OnAsyncRead(IAsyncResult asr)
        {
            if (_tcp == null)
            {
                return;
            }

            try
            {
                //读取字节流到缓冲区
                lock (_tcp.GetStream())
                {
                    var bytesRead = _tcp.GetStream().EndRead(asr);

                    if (bytesRead > 0)
                    {
                        lock (streamReceive)
                        {
                            streamReceive.Append(_byteBuffer, 0, bytesRead);
                        }
                    }

                    //清空缓冲区
                    Array.Clear(_byteBuffer, 0, _byteBuffer.Length);
                    //分析完，再次监听服务器发过来的新消息
                    if (IsConnected())
                    {
                        _tcp.GetStream().BeginRead(_byteBuffer, 0, MaxRead, OnAsyncRead, _tcp);
                    }
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
            catch (Exception ex)
            {
                Debug.Log(ex);
                Close();
                _connectEvent?.Invoke(SocketEvent.Disconnect);
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