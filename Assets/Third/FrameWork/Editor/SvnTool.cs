using System.IO;
using UnityEditor;
using UnityEngine;

public class SvnTool
{
    public const string SvnRootUrl = "https://120.78.159.8/svn/xiaobing/";

    /// <summary>
    /// 更新或检出svn
    /// </summary>
    /// <param name="path">存放路径</param>
    /// <param name="svn">svn地址</param>
    /// <returns></returns>
    public static bool UpdateOrCheckout(string path, string svn)
    {
        if (Directory.Exists(path))
        {
            return Update(path);
        }

        return Checkout(path, svn);
    }

    /// <summary>
    /// 更新svn
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static bool Update(string path)
    {
        if (!Directory.Exists(path))
        {
            EditorUtility.DisplayDialog("目录不存在", path, "确定");
            return false;
        }
#if UNITY_EDITOR_OSX
        return true;
#else
        //更新配置表
        var update_bat = BatchSystem.BatBatch("TortoiseProc.exe", "/closeonend:2 /command:update /path:./ ", path);
        update_bat.Start();
        if (!update_bat.IsSuccess)
        {
            EditorUtility.DisplayDialog("更新失败", path, "确定");
            return false;
        }

        AssetDatabase.Refresh();
        return true;
#endif
    }
    // public static void commit()
    // {
    //     var exePath = Application.dataPath;
    //     var bat = BatchSystem.BatBatch("TortoiseProc.exe", "/closeonend:2 /command:commit -m /path:" + Application.dataPath + "/Excel/*" + Application.dataPath + "/Pro/" + " ", exePath);
    //     bat.Start();
    //     if (!bat.IsSuccess)
    //     {
    //         UnityEngine.Debug.Log("常规文件提交失败");
    //         return;
    //     }
    //     UnityEngine.Debug.Log("常规文件提交成功");
    // }

    /// <summary>
    /// 检出svn
    /// </summary>
    /// <param name="path">存放目录</param>
    /// <param name="svn">svn地址</param>
    /// <returns></returns>
    private static bool Checkout(string path, string svn)
    {
        svn = SvnRootUrl + svn;
#if UNITY_EDITOR_OSX
        return Directory.Exists(path);
#else
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            var checkout_bat = BatchSystem.BatBatch("TortoiseProc.exe", "/command:checkout /closeonend:2 /url:" + svn + " /path:./", path);
            checkout_bat.Start();
            if (!checkout_bat.IsSuccess)
            {
                Directory.Delete(path, true);
                EditorUtility.DisplayDialog("检出失败", "请检查Svn路径: " + svn, "确定");
                return false;
            }
        }

        return true;
#endif
    }
}