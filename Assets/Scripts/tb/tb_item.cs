using System;
using siliu.i18n;
using siliu.tb;

namespace tb
{
    [Serializable]
    public class tb_row_item : IBaseRow
    {
        public I18NKey name;
        public string icon;
        public int quality;
    }
    
    public class tb_item : BaseTb<tb_item, tb_row_item>
    {
        
    }
}