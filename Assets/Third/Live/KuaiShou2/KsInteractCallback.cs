namespace kuaishou
{
    public interface KsInteractCallback
    {
        void OnConnected(ConnectData data);
        void OnDisconnected();
        void OnDanMu(DanMuData data);
        void OnGift(GiftData data);
        void OnFollow(FollowData data);
        void OnDianZhan(DianZhanData data);
        void OnShare(ShareData data);
        void OnError(int code, string msg);
    }
}