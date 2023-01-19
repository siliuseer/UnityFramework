/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace fui.Common
{
    public class AlertMsg : GComponent
    {
        public const string URL = "ui://31igx9w0c0x40";

        public GTextField m_title;
        public GRichTextField m_msg;
        public GButton m_cancel;
        public GButton m_confirm;
        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_title = (GTextField)GetChildAt(1);
            m_msg = (GRichTextField)GetChildAt(2);
            m_cancel = (GButton)GetChildAt(3);
            m_confirm = (GButton)GetChildAt(4);
        }
    }
}