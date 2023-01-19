using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace siliu
{
    public static class HttpUtil
    {
        public static async Task<byte[]> Post(string url, byte[] data)
        {
            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(data);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = 5;
            webRequest.SendWebRequest();
            while (!webRequest.isDone)
            {
                await Task.Delay(TimeSpan.FromSeconds(Time.deltaTime));
            }

            return webRequest.result != UnityWebRequest.Result.Success ? null : webRequest.downloadHandler.data;
        }
        
        public static async Task<string> PostJson(string url, string json)
        {
            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = 5;
            webRequest.SendWebRequest();
            while (!webRequest.isDone)
            {
                await Task.Delay(TimeSpan.FromSeconds(Time.deltaTime));
            }

            return webRequest.result != UnityWebRequest.Result.Success ? string.Empty : webRequest.downloadHandler.text;
        }
        
        public static async Task<string> Get(string url)
        {
            using var webRequest = UnityWebRequest.Get(url);
            webRequest.timeout = 5;
            webRequest.SendWebRequest();
            while (!webRequest.isDone)
            {
                await Task.Delay(TimeSpan.FromSeconds(Time.deltaTime));
            }

            return webRequest.result != UnityWebRequest.Result.Success ? null : webRequest.downloadHandler.text;
        }

        public static async Task<byte[]> Download(string url)
        {
            using var webRequest = UnityWebRequest.Get(url);
            webRequest.timeout = 10;
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SendWebRequest();
            while (!webRequest.isDone)
            {
                await Task.Delay(TimeSpan.FromSeconds(Time.deltaTime));
            }

            return webRequest.result != UnityWebRequest.Result.Success ? null : webRequest.downloadHandler.data;
        }
    }
}