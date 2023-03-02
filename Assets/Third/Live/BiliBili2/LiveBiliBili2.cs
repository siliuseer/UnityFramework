using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenBLive.Runtime;
using OpenBLive.Runtime.Data;
using OpenBLive.Runtime.Utilities;
using UnityEngine;

namespace siliu
{
    public class LiveBiliBili2 : ILive
    {
        private readonly string appId;

        public LiveBiliBili2(string appid, string accessKey, string accessKeySecret)
        {
            appId = appid;
            SignUtility.accessKeySecret = accessKeySecret;
            SignUtility.accessKeyId = accessKey;
        }

        protected override async void LinkStart()
        {
            var data = await GetAnchorInfo(anchor.uid);
            if (data == null)
            {
                IsLinked = false;
                return;
            }

            anchor.uid = data.RoomId.ToString();
            anchor.name = data.UName;
            anchor.icon = data.UFace;
            IsLinked = true;
            StartLogic();
        }

        protected override async void OnDispose()
        {
            await LinkEnd();
        }

        // Start is called before the first frame update
        private WebSocketBLiveClient _bLiveClient;
        private InteractivePlayHeartBeat _heartBeat;
        private string gameId;

        public async Task<AppStartAnchorInfo> GetAnchorInfo(string code)
        {
            var ret = await BApi.StartInteractivePlay(code, appId);
            //打印到控制台日志
            var gameIdResObj = JsonConvert.DeserializeObject<AppStartInfo>(ret);
            if (gameIdResObj == null)
            {
                return null;
            }

            if (gameIdResObj.Code != 0)
            {
                Debug.LogError(gameIdResObj.Message);
                return null;
            }

            _bLiveClient = new WebSocketBLiveClient(gameIdResObj.GetWssLink(), gameIdResObj.GetAuthBody());
            _bLiveClient.OnDanmaku = OnDanmaku;
            _bLiveClient.OnGift = OnGift;
            _bLiveClient.OnGuardBuy = OnGuardBuy;
            _bLiveClient.OnSuperChat = OnSuperChat;
            _bLiveClient.OnClose = () => { Debug.Log("B站链接关闭"); };
            _bLiveClient.OnError = OnError;
            _bLiveClient.Open = (sender, args) =>
            {
                gameId = gameIdResObj.GetGameId();
                _heartBeat?.Dispose();
                _heartBeat = new InteractivePlayHeartBeat(gameId);
                _heartBeat.HeartBeatError = OnHeartBeatError;
                _heartBeat.HeartBeatSucceed = OnHeartBeatSucceed;
                _heartBeat.Start();
            };

            try
            {
                _bLiveClient.Connect();
            }
            catch (Exception ex)
            {
                Debug.Log("B站连接失败: " + ex);
                return null;
            }

            while (!string.IsNullOrEmpty(gameId))
            {
                await Task.Delay(TimeSpan.FromSeconds(Time.deltaTime));
            }

            return gameIdResObj.Data.AnchorInfo;
        }

        private async Task LinkEnd()
        {
            _bLiveClient?.Dispose();
            _bLiveClient = null;
            _heartBeat?.Dispose();
            _heartBeat = null;
            await BApi.EndInteractivePlay(appId, gameId);
            Debug.Log("B站断开链接");
        }

        private void OnSuperChat(SuperChat superChat)
        {
            StringBuilder sb = new StringBuilder("收到SC!");
            sb.AppendLine();
            sb.Append("来自用户：");
            sb.AppendLine(superChat.userName);
            sb.Append("留言内容：");
            sb.AppendLine(superChat.message);
            sb.Append("金额：");
            sb.Append(superChat.rmb);
            sb.Append("元");
            Debug.Log(sb);
        }

        private void OnGuardBuy(Guard guard)
        {
            StringBuilder sb = new StringBuilder("收到大航海!");
            sb.AppendLine();
            sb.Append("来自用户：");
            sb.AppendLine(guard.userInfo.userName);
            sb.Append("赠送了");
            sb.Append(guard.guardUnit);
            Debug.Log(sb);
        }

        private void OnGift(SendGift data)
        {
            EnqueueMsg(new LiveMsgGift
            {
                uid = data.uid.ToString(),
                name = data.userName,
                icon = data.userFace,
                gift = data.giftName,
                num = data.giftNum
            });
        }

        private void OnDanmaku(Dm data)
        {
            EnqueueMsg(new LiveMsgDanMu
            {
                uid = data.uid.ToString(),
                name = data.userName,
                icon = data.userFace,
                msg = data.msg
            });
        }

        private static void OnHeartBeatSucceed()
        {
            // Debug.Log("心跳成功");
        }

        private async void OnError()
        {
            Debug.Log("B站链接错误");

            await LinkEnd();
            await Task.Delay(2000);
            LinkStart();
        }

        private static void OnHeartBeatError(string json)
        {
            Debug.Log("心跳失败" + json);
        }

        private async void StartLogic()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            while (IsLinked)
            {
                await Task.Delay(TimeSpan.FromSeconds(Time.deltaTime));
                if (_bLiveClient is { ws: { State: NativeWebSocket.WebSocketState.Open } })
                {
                    _bLiveClient.ws.DispatchMessageQueue();
                }
            }
#endif
        }
    }
}