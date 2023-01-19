using System.Text;
using Google.Protobuf;
using Newtonsoft.Json;

namespace siliu.net
{
    public abstract class SendEntry
    {
        public int proto { get; }
        public string protoStr { get; }
        public bool http { get; set; }
        public int flag { get; set; }
        public bool repeat { get; set; } = true;
        public byte[] bytes { get; protected set; }

        public SendEntry(int proto)
        {
            this.proto = proto;
        }

        public SendEntry(string str)
        {
            protoStr = str;
        }

        public abstract SendEntry EncodeData(object data);

        public void Send()
        {
            NetMgr.Inst.Send(this);
        }
    }

    public class SendJsonEntry : SendEntry
    {
        public SendJsonEntry(string proto) : base(proto)
        {
        }

        public override SendEntry EncodeData(object data)
        {
            var json = JsonConvert.SerializeObject(data);
            bytes = Encoding.UTF8.GetBytes(json);
            return this;
        }
    }

    public class SendProtoEntry : SendEntry
    {
        public SendProtoEntry(int proto) : base(proto)
        {
        }

        public override SendEntry EncodeData(object data)
        {
            if (data is IMessage msg)
            {
                bytes = msg.ToByteArray();
            }
            return this;
        }
    }
}