namespace siliu
{
    public class LiveDouYin2 : ILive
    {
        protected override void LinkStart()
        {
            IsLinked = true;
        }

        protected override void OnDispose()
        {
        }
    }
}