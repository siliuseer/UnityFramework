using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using WebSocketSharp;

namespace siliu
{
    public class LiveKuaiShou : ILive
    {
        private class MsgData
        {
            /// <summary>
            /// 消息类型, 1:来了 2:发言 3:点赞 4:送礼 5:关注
            /// </summary>
            public int type;

            public string uid;
            public string name;
            public string url;
            public string msg;
            public string guft;
            public int num;
        }

        private WebSocket webSocket;
        private bool connected;
        private bool disposed;

        protected override async void LinkStart()
        {
            var start = Path.Combine(Environment.CurrentDirectory, "elive/start.exe");
            if (File.Exists(start))
            {
                Debug.Log("启动弹幕助手");
                System.Diagnostics.Process.Start(start);
                await Task.Delay(1000);
            }

            if (webSocket == null)
            {
                webSocket = new WebSocket("ws://127.0.0.1:3000");
                webSocket.OnOpen += OnOpen;
                webSocket.OnMessage += OnMessage;
                webSocket.OnError += OnError;
                webSocket.OnClose += OnClose;
            }

            ConnectAsync();
            while (!connected)
            {
                await Task.Delay((int)(Time.deltaTime * 1000));
            }

            IsLinked = true;
        }

        protected override void OnDispose()
        {
            disposed = true;
            webSocket?.CloseAsync();
        }

        private void ConnectAsync()
        {
            connected = false;
            webSocket?.ConnectAsync();
        }

        private void OnOpen(object sender, EventArgs args)
        {
            Debug.Log("弹幕助手连接成功");
            connected = true;
        }

        private void OnError(object sender, WebSocketSharp.ErrorEventArgs args)
        {
            Debug.Log("弹幕助手报错: " + args.Exception);
        }

        private void OnClose(object sender, CloseEventArgs args)
        {
            Debug.Log("弹幕助手关闭: " + disposed);
            connected = false;
            if (disposed)
            {
                return;
            }

            ConnectAsync();
        }

        private void OnMessage(object sender, MessageEventArgs args)
        {
            if (!args.IsText)
            {
                return;
            }

            var data = JsonConvert.DeserializeObject<MsgData>(args.Data);
            if (data == null)
            {
                return;
            }

            switch (data.type)
            {
                case 2: //发言
                    EnqueueMsg(new LiveMsgDanMu
                    {
                        uid = data.uid,
                        name = data.name,
                        icon = data.url,
                        msg = data.msg
                    });
                    break;
                case 3: //点赞
                    EnqueueMsg(new LiveMsgLike
                    {
                        uid = data.uid,
                        name = data.name,
                        icon = data.url,
                        num = data.num
                    });
                    break;
                case 4: //送礼
                    EnqueueMsg(new LiveMsgGift
                    {
                        uid = data.uid,
                        name = data.name,
                        icon = data.url,
                        gift = data.guft,
                        num = data.num
                    });
                    break;
            }
        }
    }
}