using System.Threading.Tasks;
using siliu.i18n;

namespace siliu
{
    public static class GameMgr
    {
        public static async Task InitAsync(PlayMode playMode, string cdn = "", string assetRoot = "Assets/Asset")
        {
            await ResUpdate.Init(playMode, cdn);
            AssetLoader.Init(assetRoot);
            I18N.Init();
            UIMgr.Init(1920, 1080);
        }
    }
}