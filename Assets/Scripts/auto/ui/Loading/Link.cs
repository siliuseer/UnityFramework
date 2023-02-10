/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

using FairyGUI;
using FairyGUI.Utils;

namespace fui.Loading
{
    public class Link : GComponent
    {
        public const string URL = "ui://3u7drp4fj4sk5";

        public GProgressBar m_bar;
        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_bar = (GProgressBar)GetChildAt(1);
        }
    }
}