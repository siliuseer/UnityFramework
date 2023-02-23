using System;
using FairyGUI;
using siliu;

public class AlertMsg : BaseDialog<fui.Common.AlertMsg>
{
    private class AlertData
    {
        public string msg;
        public string title = "温馨提示";
        public string confirmTitle = "确认";
        public string cancelTitle;
        public Action confirm;
        public Action cancel;
    }
    protected override void OnShow(params object[] args)
    {
        if (args[0] is not AlertData data)
        {
            UIMgr.Close<AlertMsg>();
            return;
        }
        view.m_msg.text = data.msg;
        view.m_title.text = data.title;
        view.m_confirm.visible = !string.IsNullOrEmpty(data.confirmTitle);
        view.m_confirm.title = data.confirmTitle;
        view.m_confirm.BindClick(() =>
        {
            data.confirm?.Invoke();
            UIMgr.Close<AlertMsg>();
        });
        view.m_cancel.visible = !string.IsNullOrEmpty(data.cancelTitle);
        view.m_cancel.title = data.cancelTitle;
        view.m_cancel.BindClick(() =>
        {
            data.cancel?.Invoke();
            UIMgr.Close<AlertMsg>();
        });
    }

    public static void Alert(string msg)
    {
        UIMgr.Show<AlertMsg>(new AlertData {msg = msg});
    }
}