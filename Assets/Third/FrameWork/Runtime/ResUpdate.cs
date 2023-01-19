// ==================================================
// File: ResUpdate.cs
// Time: 2022-07-27 10:34:54
// Desc: 
// ==================================================

using System;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;

public class ResUpdate
{
    private const string VerKey = "Ver";

    private static string ResVer
    {
        get => PlayerPrefs.GetString(VerKey, string.Empty);
        set => PlayerPrefs.SetString(VerKey, value);
    }

    public Action<long, long> OnDownloadProgress;
    public Action OnFinishCallback;

    public static async Task Init(PlayMode playMode, string cdn)
    {
        var start = DateTime.Now;
        // 初始化资源系统
        YooAssets.Initialize();

        // 创建默认的资源包
        var package = YooAssets.CreateAssetsPackage("DefaultPackage");
        
        InitializeParameters initParameters = null;
        switch (playMode)
        {
            case PlayMode.Editor:
            {
                initParameters = new EditorSimulateModeParameters
                {
                    SimulatePatchManifestPath = EditorSimulateModeHelper.SimulateBuild("DefaultPackage")
                };
                break;
            }
            case PlayMode.Offline:
            {
                initParameters = new OfflinePlayModeParameters();
                break;
            }
            case PlayMode.Host:
            {
                var _cdn = GetCdnUrl(cdn);
                initParameters = new HostPlayModeParameters
                {
                    DefaultHostServer = _cdn,
                    FallbackHostServer = _cdn,
                };
                break;
            }
        }
        await package.InitializeAsync(initParameters).Task;
        
        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultAssetsPackage(package);
        
        
        Debug.Log($"YooAssets.InitializeAsync cost {(DateTime.Now - start).TotalMilliseconds} ms");
    }

    public async Task Start()
    {
        var package = YooAssets.GetAssetsPackage("DefaultPackage");
        var versionAsync = package.UpdatePackageVersionAsync(30);
        await versionAsync.Task;

        if (versionAsync.Status != EOperationStatus.Succeed)
        {
            Debug.Log("Get Res Ver Error: " + versionAsync.Error);
            OnFinishCallback?.Invoke();
            return;
        }

        var newVer = versionAsync.PackageVersion;
        var manifestOperation = package.UpdatePackageManifestAsync(newVer);
        await manifestOperation.Task;

        if (manifestOperation.Status != EOperationStatus.Succeed)
        {
            Debug.Log("Update Manifest Error: " + manifestOperation.Error);
            OnFinishCallback?.Invoke();
            return;
        }

        var downloader = YooAssets.CreatePatchDownloader(10, 5);
        // 没有资源需要更新
        if (downloader.TotalDownloadCount == 0)
        {
            ResVer = newVer;
            OnFinishCallback?.Invoke();
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(Time.deltaTime));

        //注册回调方法
        downloader.OnDownloadErrorCallback = (name, error) => { Debug.LogError(name + ", error: " + error); };
        downloader.OnDownloadProgressCallback = (count, downloadCount, bytes, downloadBytes) =>
        {
            // var percent = 1f * downloadBytes / bytes;
            // Debug.Log($"download progress: {percent}, count: {count}, downloadCount: {downloadCount}, bytes: {bytes}, downloadBytes: {downloadBytes}");
            OnDownloadProgress?.Invoke(downloadBytes, bytes);
        };

        //开启下载
        downloader.BeginDownload();
        await downloader.Task;

        if (downloader.Status != EOperationStatus.Succeed)
        {
            OnFinishCallback?.Invoke();
            return;
        }

        ResVer = newVer;
        OnFinishCallback?.Invoke();
    }

    public static string GetCdnUrl(string cdn)
    {
        var split = Application.version.Split('.');
        return $"{cdn}/v{split[0]}.{split[1]}";
    }
}