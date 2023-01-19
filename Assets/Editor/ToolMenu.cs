using System;
using siliu.editor;
using UnityEditor;
using UnityEngine;

public static class ToolMenu
{
    private static string ExtDir => Environment.CurrentDirectory + "/ext";
        
    [MenuItem("Tools/导表/开发", priority = 2000)]
    private static void ExportDev()
    {
        ExportExcel(ExcelType.dev);
    }

    private static void ExportExcel(ExcelType excel)
    {
        // if (!SvnTool.UpdateOrCheckout($"{ExtDir}/{ExcelType.dev}", "Doc/%E6%95%B0%E6%8D%AE%E8%A1%A8/%E5%BC%80%E5%8F%91"))
        // {
        //     return;
        // }
        ExcelExport.Export($"{ExtDir}/{excel}/excel");
    }
        
    [MenuItem("Tools/语言包/开发", priority = 2000)]
    private static void Menu()
    {
        ExportI18N(ExcelType.dev);
    }
    private static void ExportI18N(ExcelType excel)
    {
        // if (!SvnTool.UpdateOrCheckout($"{ExtDir}/{ExcelType.dev}", "Doc/%E6%95%B0%E6%8D%AE%E8%A1%A8/%E5%BC%80%E5%8F%91"))
        // {
        //     return;
        // }
        I18NExport.Export($"{ExtDir}/{excel}/i18n");
    }

    [MenuItem("Tools/更新客户端", priority = 2000)]
    private static void UpdateClient()
    {
        SvnTool.Update(Environment.CurrentDirectory);
    }

    [MenuItem("Tools/打开客户端目录", false, 2000)]
    private static void OpenClientPath()
    {
        System.Diagnostics.Process.Start(Environment.CurrentDirectory);
    }
    
    [MenuItem("Tools/打开临时目录", false, 2000)]
    private static void OpenLinShiPath()
    {
        System.Diagnostics.Process.Start(Application.persistentDataPath);
    }
}
