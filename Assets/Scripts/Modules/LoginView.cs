using FairyGUI;
using siliu;

public class LoginView : BaseDialog<fui.Login.Login>
{
    private const string SaveCodeKey = "SaveCode";
    private const string IdCodeKey = "RoomCode";
    private const string PwdKey = "RoomPwd";

    protected override void OnShow(params object[] args)
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

    private void LinkStart()
    {
        var pwd = view.m_InputPwd.text;
        var code = view.m_InputCode.text.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace(" ", string.Empty);
        if (string.IsNullOrEmpty(code))
        {
            return;
        }

        var split = code.Split('|');
        code = split[0];
        if (split.Length > 1 && split[1].StartsWith("http"))
        {
            AppCfg.url = split[1];
        }

        if (view.m_BtnSave.selected)
        {
            PlayerPrefsUtil.SetString(IdCodeKey, code);
            PlayerPrefsUtil.SetString(PwdKey, pwd);
        }
        CloseMySelf();
        UIMgr.Show<LinkView>(code, pwd);
    }
}