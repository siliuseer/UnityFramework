using Liluo.BiliBiliLive;

namespace siliu
{
    public class LiveBiliBili : ILive
    {
        private IBiliBiliLiveRequest req;

        protected override async void LinkStart()
        {
            if (!long.TryParse(anchor.uid, out var roomId))
            {
                IsLinked = false;
                return;
            }

            // 创建一个监听对象
            req = await BiliBiliLive.Connect(roomId);
            if (req == null)
            {
                IsLinked = false;
                return;
            }

            req.OnDanmuCallBack -= OnDanmu;
            req.OnGiftCallBack -= OnGift;
            req.OnSuperChatCallBack -= OnSuperChat;
            req.OnGuardCallBack -= OnGuard;
            req.OnDataError -= OnError;

            req.OnDanmuCallBack += OnDanmu;
            req.OnGiftCallBack += OnGift;
            req.OnSuperChatCallBack += OnSuperChat;
            req.OnGuardCallBack += OnGuard;
            req.OnDataError += OnError;
            IsLinked = true;
        }

        protected override void OnDispose()
        {
            if (req == null)
            {
                return;
            }

            req.DisConnect();
            req = null;
        }

        /// <summary>
        /// 接收到礼物的回调
        /// </summary>
        private void OnGift(BiliBiliLiveGiftData data)
        {
            EnqueueMsg(new LiveMsgGift
            {
                uid = data.userId,
                name = data.username,
                gift = data.giftName,
                num = data.num
            });
        }

        /// <summary>
        /// 接收到弹幕的回调
        /// </summary>
        private void OnDanmu(BiliBiliLiveDanmuData data)
        {
            EnqueueMsg(new LiveMsgDanMu
            {
                uid = data.userId,
                name = data.username,
                msg = data.content
            });
        }

        /// <summary>
        /// 接收到SC的回调
        /// </summary>
        private static void OnSuperChat(BiliBiliLiveSuperChatData data)
        {
            LogUtil.Log($"<color=#FFD766>SC</color> 用户名: {data.username}, 内容: {data.content}, 金额: {data.price}");
        }

        private static void OnGuard(BiliBiliLiveGuardData data)
        {
            LogUtil.Log(
                $"<color=#FFD766>上舰</color> 用户名: {data.username}, 购买舰长: {data.guardName}, 数量: {data.guardCount}, 当前等级: {data.guardLevel}");
        }

        private void OnError(string msg)
        {
            Dispose();
            LinkStart();
        }
    }
}