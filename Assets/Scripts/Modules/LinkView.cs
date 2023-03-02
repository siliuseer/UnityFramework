using siliu;
using UnityEngine;

public class LinkView : BaseScene<fui.Loading.Loading>
{
    protected override void OnShow(params object[] args)
    {
        if (args == null || args.Length < 2)
        {
            Debug.LogError("参数异常");
            return;
        }

        view.m_bar.value = 0;
        view.m_bar.TweenValue(100, 30);

        var code = args[0] as string;
        var pwd = args[1] as string;

        var live = AppCfg.live;
        live.SetMsgHandler(new MsgHandler());
        live.Start(code, pwd, args.Length > 2 ? args[2] as string : string.Empty);
    }
    private class MsgHandler : ILiveMsgHandler
    {
        public void OnDanMu(LiveMsgDanMu msg)
        {
            Debug.Log($"<color=#13ee54>弹幕</color>[{msg.uid}]{msg.name}: {msg.msg}, 头像: {msg.icon}");
        }

        public void OnGift(LiveMsgGift msg)
        {
            Debug.Log($"<color=#eecb13>礼物</color>[{msg.uid}]{msg.name}: {msg.gift} x {msg.num}, 头像: {msg.icon}");
        }

        public void OnLike(LiveMsgLike msg)
        {
            Debug.Log($"<color=#eecb13>点赞</color>[{msg.uid}]{msg.name}: 次数 x {msg.num}, 头像: {msg.icon}");
        }
    }
}