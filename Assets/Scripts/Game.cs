using siliu;
using Sirenix.OdinInspector;
using UnityEngine;

public class Game : MonoBehaviour
{
    [LabelText("资源服地址")] public string cdn;
    [LabelText("运行模式")] public PlayMode playMode;
    [LabelWidth(30)]
    [LabelText("宽")]
    [HorizontalGroup("size")]
    public int w;
    [HorizontalGroup("size")]
    [LabelWidth(30)]
    [LabelText("高")] public int h;

    private async void Start()
    {
#if UNITY_EDITOR
        AppCfg.cdn = cdn;
#endif
#if !UNITY_EDITOR
        playMode = string.IsNullOrEmpty(AppCfg.cdn) ? PlayMode.Offline : PlayMode.Host;
#endif
        await GameMgr.InitAsync(playMode, cdn);
        UIMgr.Show<LoginView>();
    }
}
