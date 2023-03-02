public static class AppCfg
{
    public const bool debug = true;
    public static string url = "http://192.168.41.88/danmulogin/";
    public const string cdn = "";
    public const int w = 1920;
    public const int h = 1080;
    public const string assetRoot = "Assets/Asset";
    public const PlatType plat = PlatType.BiliBili;
    public static readonly siliu.ILive live = new siliu.LiveBiliBili();
}
