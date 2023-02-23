using siliu;
using UnityEngine;

public class LinkView : BaseScene<fui.Loading.Loading>
{
    protected override void OnShow(params object[] args)
    {
        if (args == null || args.Length < 2)
        {
            Debug.LogError("参数异常");
            return;
        }
        view.m_bar.value = 0;
        view.m_bar.TweenValue(100, 30);

        var code = args[0] as string;
        var pwd = args[1] as string;
    }
}