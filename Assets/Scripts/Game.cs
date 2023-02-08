using siliu;
using Sirenix.OdinInspector;
using UnityEngine;

public class Game : MonoBehaviour
{
    [LabelText("CDN地址")] public string cdn;
    [LabelText("运行模式")] public PlayMode playMode;

    private async void Start()
    {
        var _cdn = AppCfg.cdn;
#if UNITY_EDITOR
        _cdn = cdn;
#endif
#if !UNITY_EDITOR
        playMode = string.IsNullOrEmpty(AppCfg.cdn) ? PlayMode.Offline : PlayMode.Host;
#endif
        LogUtil.Init();
        await ResUpdate.Init(playMode, _cdn);
        AssetLoader.Init(AppCfg.assetRoot);
        siliu.i18n.I18N.Init();
        UIMgr.Init(AppCfg.w, AppCfg.h);
        
        InitEnd();
    }

    private void InitEnd()
    {
        UIMgr.Show<LoginView>();
    }

    private void OnDestroy()
    {
        LogUtil.Dispose();
    }
}
