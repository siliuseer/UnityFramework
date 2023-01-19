namespace siliu.net
{
    public abstract class IDownCfg
    {
        public int proto;
        public string protoStr;
        public bool gz;
        public abstract BaseDownEntry CreateEntry();
    }

    public class DownCfg<T> : IDownCfg where T : BaseDownEntry, new()
    {
        public override BaseDownEntry CreateEntry()
        {
            return new T();
        }
    }
}