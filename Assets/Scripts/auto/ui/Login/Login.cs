/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace fui.Login
{
    public class Login : GComponent
    {
        public const string URL = "ui://337rf71sc0x40";

        public GButton m_BtnSave;
        public GComponent m_BtnLogin;
        public GTextInput m_InputCode;
        public GComponent m_BtnClear;
        public GTextInput m_InputPwd;
        public GComponent m_PwdClear;
        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_BtnSave = (GButton)GetChildAt(2);
            m_BtnLogin = (GComponent)GetChildAt(3);
            m_InputCode = (GTextInput)GetChildAt(7);
            m_BtnClear = (GComponent)GetChildAt(8);
            m_InputPwd = (GTextInput)GetChildAt(12);
            m_PwdClear = (GComponent)GetChildAt(13);
        }
    }
}