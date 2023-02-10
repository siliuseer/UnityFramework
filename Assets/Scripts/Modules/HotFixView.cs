using siliu;

public class HotFixView : BaseScene<fui.Loading.Loading>
{
    protected override void OnShow(params object[] objects)
    {
        StartUpdate();
    }

    private async void StartUpdate()
    {
        await new ResUpdate
        {
            OnDownloadProgress = OnDownload,
            OnFinishCallback = OnFinish
        }.Start();
    }

    private void OnDownload(long download, long total)
    {
        view.m_bar.max = total;
        view.m_bar.value = download;
    }

    private void OnFinish()
    {
        view.m_bar.visible = false;
        UIMgr.Show<LoginView>();
    }
}