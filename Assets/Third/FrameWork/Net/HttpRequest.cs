using System;

namespace siliu.net
{
    public class HttpRequest : IRequest
    {
        private Action<BaseDownEntry> _onReceive;
        private string _url;

        public HttpRequest(string url, Action<BaseDownEntry> receive)
        {
            _url = url;
            _onReceive = receive;
        }

        public bool IsConnected()
        {
            return true;
        }

        public async void Send(SendEntry data)
        {
            var bytes = await HttpUtil.Post(_url + data.protoStr, data.bytes);
            var down = NetMgr.FindDown(data.protoStr);
            if (down == null)
            {
                return;
            }

            var result = 1;
            if (bytes == null)
            {
                result = -1;
                bytes = BitConverter.GetBytes(result);
            }

            var entry = down.CreateEntry();
            entry.flag = data.flag;
            entry.proto = data.protoStr;
            entry.DealReceive(result, bytes);
            _onReceive?.Invoke(entry);
        }
    }
}