namespace FairyGUI
{
    public static class FguiExt
    {
        public static void BindClick(this GComponent comp, EventCallback0 action)
        {
            comp.onClick.Add(action);
        }
        public static void BindClick(this GComponent comp, EventCallback1 action)
        {
            comp.onClick.Add(action);
        }
    }
}