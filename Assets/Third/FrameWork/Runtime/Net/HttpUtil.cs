using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace siliu
{
    public static class HttpUtil
    {
        public static async Task<byte[]> Post(string url, byte[] data, Dictionary<string, string> heads = null)
        {
            using var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(data);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = 5;
            if (heads != null)
            {
                foreach (var pair in heads)
                {
                    webRequest.SetRequestHeader(pair.Key, pair.Value);
                }
            }
            webRequest.SendWebRequest();
            while (!webRequest.isDone)
            {
                await Task.Delay(TimeSpan.FromSeconds(Time.deltaTime));
            }

            return webRequest.result != UnityWebRequest.Result.Success ? null : webRequest.downloadHandler.data;
        }
        
        public static async Task<string> PostJson(string url, string json, Dictionary<string, string> heads = null)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var http = new HttpClient();
            if (heads != null)
            {
                var headers = content.Headers;
                foreach (var pair in heads)
                {
                    headers.Add(pair.Key, pair.Value);
                }
            }

            var response = await http.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadAsStringAsync();
            return result;
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