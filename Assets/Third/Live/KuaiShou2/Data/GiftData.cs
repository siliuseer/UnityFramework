namespace kuaishou
{
    public class GiftData
    {
        /// <summary> 礼物id </summary>
        public string giftId;
        /// <summary> 礼物名称 </summary>
        public string giftName;
        /// <summary> 礼物总价值 (1元=10dou) </summary>
        public string giftTotalDou;
        /// <summary> 礼物总价值 (1元=1000azuan) </summary>
        public int azuan;
        /// <summary> 礼物个数 </summary>
        public int count;
        /// <summary> 用户信息 </summary>
        public UserData user;
    }
}