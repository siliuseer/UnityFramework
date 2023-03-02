using kuaishou;
using UnityEngine;

namespace siliu
{
    public class LiveKuaiShou2 : ILive, KsInteractCallback
    {
        private KsInteractSdkDll ks;

        public LiveKuaiShou2(string appId)
        {
            ks = new KsInteractSdkDll(appId, this);
        }

        protected override async void LinkStart()
        {
            var connect = await ks.Connect(anchor.uid, "");
            if (connect)
            {
                IsLinked = true;
            }
        }

        protected override void OnDispose()
        {
            if (ks == null)
            {
                return;
            }

            ks.Release();
            ks = null;
        }

        //------ KsInteractCallback
        public void OnConnected(ConnectData data)
        {
            anchor.uid = data.ksUid.ToString();
            anchor.name = data.user.userName;
        }

        public void OnDisconnected()
        {
            Debug.Log("OnDisconnected");
        }

        public void OnDanMu(DanMuData data)
        {
            EnqueueMsg(new LiveMsgDanMu
            {
                uid = data.user.id,
                name = data.user.userName,
                icon = data.user.headUrl,
                msg = data.content
            });
        }

        public void OnGift(GiftData data)
        {
            EnqueueMsg(new LiveMsgGift
            {
                uid = data.user.id,
                name = data.user.userName,
                icon = data.user.headUrl,
                gift = data.giftName,
                num = data.count
            });
        }

        public void OnFollow(FollowData data)
        {
        }

        public void OnDianZhan(DianZhanData data)
        {
            EnqueueMsg(new LiveMsgLike
            {
                uid = data.user.id,
                name = data.user.userName,
                icon = data.user.headUrl,
                num = data.count
            });
        }

        public void OnShare(ShareData data)
        {
        }

        public void OnError(int code, string msg)
        {
            Debug.LogError("OnError code:" + code + ", msg: " + msg);
        }
    }
}