using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;

public class GmEditor : OdinMenuEditorWindow
{
    [MenuItem("Tools/GM指令 %G")]
    private static void ShowWindow()
    {
        GetWindow<GmEditor>().Show();
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();

        var assembly = typeof(Menu).Assembly;
        
        assembly.GetTypes()
        .Where(x => !x.IsAbstract)
        .Where(x => !x.IsGenericTypeDefinition)
        .Where(x => typeof(Menu).IsAssignableFrom(x))
        .Select(x => x.Name)
        .ForEach(s =>
        {
            if (assembly.CreateInstance(s) is Menu gm)
            {
                tree.Add(gm.MenuName, gm);
            }
        });

        return tree;
    }
    public abstract class Menu
    {
        public abstract string MenuName { get; }
    }
}
