using siliu;
using Sirenix.OdinInspector;
using UnityEngine;

public class Game : MonoBehaviour
{
    [LabelText("资源服地址")] public string cdn;
    [LabelText("运行模式")] public PlayMode playMode;

    private async void Start()
    {
#if UNITY_EDITOR
        AppCfg.cdn = cdn;
#endif
#if !UNITY_EDITOR
        playMode = string.IsNullOrEmpty(AppCfg.cdn) ? PlayMode.Offline : PlayMode.Host;
#endif
        await ResUpdate.Init(playMode, cdn);
        UIMgr.Init();
        UIMgr.Show<LoginView>();
    }
}
