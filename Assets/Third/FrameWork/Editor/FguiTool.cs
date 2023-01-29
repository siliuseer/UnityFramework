using UnityEditor;
using UnityEngine;

namespace siliu.editor
{
    public class FguiTool
    {
        [MenuItem("Tools/生成Fgui依赖")]
        public static void Build()
        {
            BatchSystem.BatBatch("python", "tools/fui_define.py");
            Debug.Log("Fgui依赖生成成功");
        }
    }
}