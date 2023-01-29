using FairyGUI;
using siliu;
using UnityEngine;

public class LoginView : BaseDialog<fui.Login.Login>
{
    private const string SaveCodeKey = "SaveCode";
    private const string IdCodeKey = "RoomCode";
    private const string PwdKey = "RoomPwd";

    protected override void OnShow()
    {
        view.m_InputCode.text = PlayerPrefsUtil.GetString(IdCodeKey);
        view.m_InputPwd.text = PlayerPrefsUtil.GetString(PwdKey);
        view.m_BtnSave.onChanged.Add(() =>
        {
            var save = view.m_BtnSave.selected;
            PlayerPrefsUtil.SetBool(SaveCodeKey, save);
            if (!save)
            {
                PlayerPrefsUtil.SetString(IdCodeKey, string.Empty);
            }
        });
        view.m_BtnSave.selected = PlayerPrefsUtil.GetBool(SaveCodeKey);
        view.m_BtnClear.BindClick(() => { view.m_InputCode.text = string.Empty; });
        view.m_PwdClear.BindClick(() => { view.m_InputPwd.text = string.Empty; });
        view.m_BtnLogin.BindClick(LinkStart);
    }
    private struct S
    {
        public int id;
    }

    private void LinkStart()
    {
        // var pwd = view.m_InputPwd.text;
        // var code = view.m_InputCode.text.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace(" ", string.Empty);
        // if (string.IsNullOrEmpty(code))
        // {
        //     return;
        // }
        //
        // if (view.m_BtnSave.selected)
        // {
        //     PlayerPrefsUtil.SetString(IdCodeKey, code);
        //     PlayerPrefsUtil.SetString(PwdKey, pwd);
        // }
        LoadingView.LoadScene("game", () =>
        {
            UIMgr.Close<LoginView>();
            AlertMsg.Alert("测试文本");
        });
    }
}