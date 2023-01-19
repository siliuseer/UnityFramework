using FairyGUI;
using siliu;

public class AlertMsg : BaseDialog<fui.Common.AlertMsg>
{
    public class AlertData
    {
        public string msg;
        public string title = "温馨提示";
        public string confirmTxt = "确认";
        public string cancelTxt = "取消";
        public EventCallback0 confirm;
        public EventCallback0 cancel;
    }
    protected override void OnShow()
    {
        var data = args[0] as AlertData;
        if (data == null)
        {
            UIMgr.Close<AlertMsg>();
            return;
        }
        view.m_msg.text = data.msg;
        view.m_title.text = data.title;
        view.m_confirm.title = data.confirmTxt;
        view.m_cancel.title = data.cancelTxt;
        view.m_confirm.BindClick(() =>
        {
            data.confirm?.Invoke();
            UIMgr.Close<AlertMsg>();
        });
        view.m_cancel.BindClick(() =>
        {
            data.cancel?.Invoke();
            UIMgr.Close<AlertMsg>();
        });
    }
}