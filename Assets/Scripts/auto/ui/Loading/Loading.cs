/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace fui.Loading
{
    public class Loading : GComponent
    {
        public const string URL = "ui://3u7drp4fc0x40";

        public GProgressBar m_bar;
        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_bar = (GProgressBar)GetChildAt(1);
        }
    }
}