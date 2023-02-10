using siliu;

public class LinkView : BaseScene<fui.Loading.Loading>
{
    protected override void OnShow(params object[] objects)
    {
        view.m_bar.value = 0;
        view.m_bar.TweenValue(100, 30);
    }
}