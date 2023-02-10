using System;
using siliu;

public class LoadingView : BaseTopView<fui.Loading.Loading>
{
    protected override void OnShow(params object[] objects)
    {
        view.m_bar.TweenValue(100, 30);
    }

    public static async void LoadScene(string scene, Action callback)
    {
        UIMgr.Show<LoadingView>();
        await AssetLoader.LoadSceneAsync("none");
        await AssetLoader.LoadSceneAsync(scene);
        callback?.Invoke();
        UIMgr.Close<LoadingView>();
    }
}