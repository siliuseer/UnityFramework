/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace fui.Common
{
    public class ToastMsg : GComponent
    {
        public const string URL = "ui://31igx9w0c0x44";

        public GTextField m_msg;
        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_msg = (GTextField)GetChildAt(1);
        }
    }
}