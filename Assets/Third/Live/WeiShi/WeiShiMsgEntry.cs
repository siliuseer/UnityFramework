using System.Collections.Generic;

namespace siliu
{
    public class WeiShiMsgEntry
    {
        public class LiveContact
        {
            public Contact contact;

            public class Contact
            {
                public string username;
                public string nickname;
                public string headUrl;
                public string signature;
                public AuthInfo authInfo;
                public ExtInfo extInfo;

                public class AuthInfo
                {
                    public int authIconType;
                    public string authIconUrl;
                }

                public class ExtInfo
                {
                    public string country;
                    public string province;
                    public string city;
                    public int sex;
                }
            }
        }
        
        public class PayLoad
        {
            public class Gift
            {
                public string reward_product_id;
                public string name;
                public float price;
                public int gift_type;
                public int flag;
            }
            public string reward_product_id;
            public long combo_product_count;
            public string combo_id;
            public int type;
            public string ext_info;
            public string reward_amount_in_wecoin;
            public Gift reward_gift;
        }
        
        public string nickname;
        public string headUrl;
        public string content;
        public int type;
        public string username;
        public string seq;
        public string clientMsgId;
        public int isFloatmsg;
        public int floatType;
        public LiveContact finderLiveContact;
        
        //--- 礼物
        public int msgType;
        public string payload;
        public LiveContact fromUserContact;

        public Dictionary<string, object> GetInfo()
        {
            var dic = new Dictionary<string, object>();
            dic.Add("name", nickname);

            var contact = finderLiveContact?.contact;
            if (contact == null)
            {
                return dic;
            }

            dic.Add("signature", string.IsNullOrEmpty(contact.signature) ? "" : contact.signature);

            var extInfo = contact.extInfo;
            if (extInfo == null)
            {
                return dic;
            }

            dic.Add("country", extInfo.country);
            dic.Add("province", extInfo.province);
            dic.Add("city", extInfo.city);
            dic.Add("sex", extInfo.sex);

            return dic;
        }
    }
}