using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using UnityEngine;

namespace siliu
{
    public class LiveWeiShi : ILive
    {
        private IBrowser _browser;
        private IPage _page;
        private List<string> msgIds = new List<string>();
        private List<string> giftIds = new List<string>();
        private Dictionary<string, string> cacheUid = new Dictionary<string, string>();
        private readonly string signKey;
        private readonly string uidUrl;
        private readonly  int _plat;
        private readonly string _flag;
        
        public LiveWeiShi(int plat, string flag, string key, string url)
        {
            _plat = plat;
            _flag = flag;
            signKey = key;
            uidUrl = url;
        }

        protected override async void LinkStart()
        {
            try
            {
                using (var fetcher = new BrowserFetcher())
                {
                    if (!fetcher.LocalRevisions().Contains(BrowserFetcher.DefaultChromiumRevision))
                    {
                        await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
                    }
                }

                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = false
                });
                _page = await _browser.NewPageAsync();
                const string url = "https://channels.weixin.qq.com";
                await _page.DeleteCookieAsync();
                await _page.GoToAsync(url);
                _page.Response += OnResponse;
                IsLinked = true;
                Debug.Log(url);
            }
            catch (Exception e)
            {
                IsLinked = false;
                Debug.LogError("启动Puppeteer失败: " + e);
            }
        }

        protected override void OnDispose()
        {
            _page?.Dispose();
            _page = null;
            _browser?.Dispose();
            _browser = null;
        }

        private async void OnResponse(object sender, ResponseCreatedEventArgs args)
        {
            if (!IsLinked)
            {
                return;
            }

            var response = args.Response;
            if (!response.Url.EndsWith("/live/msg"))
            {
                return;
            }

            var json = await response.JsonAsync();
            if (!json.TryGetValue("data", out var data))
            {
                return;
            }
            
            LogUtil.Log(data.ToString());

            var msgList = data.Value<JArray>("msgList");
            if (msgList is { Count: > 0 })
            {
                lock (msgIds)
                {
                    foreach (var token in msgList)
                    {
                        var msg = token.ToObject<WeiShiMsgEntry>();
                        if (msg == null)
                        {
                            continue;
                        }

                        if (msgIds.Contains(msg.clientMsgId))
                        {
                            continue;
                        }

                        msgIds.Add(msg.clientMsgId);
                        DecodeMsg(msg);
                    }
                }
            }
            var giftList = data.Value<JArray>("appMsgList");
            if (giftList is { Count: > 0 })
            {
                lock (giftIds)
                {
                    foreach (var token in giftList)
                    {
                        var msg = token.ToObject<WeiShiMsgEntry>();
                        if (msg == null)
                        {
                            continue;
                        }

                        if (giftIds.Contains(msg.clientMsgId))
                        {
                            continue;
                        }

                        giftIds.Add(msg.clientMsgId);
                        DecodeGift(msg);
                    }
                }
            }
        }

        private async void DecodeMsg(WeiShiMsgEntry msg)
        {
            var openid = await GetUid(msg);
            if (string.IsNullOrEmpty(openid))
            {
                LogUtil.Log("无法获取openid, 直接丢弃");
                return;
            }

            switch (msg.type)
            {
                case 1: //弹幕
                {
                    EnqueueMsg(new LiveMsgDanMu
                    {
                        uid = openid,
                        name = msg.nickname,
                        icon = msg.headUrl,
                        msg = msg.content,
                    });
                    break;
                }
                case 10005: //来了
                    break;
            }
        }

        private async void DecodeGift(WeiShiMsgEntry msg)
        {
            var openid = await GetUid(msg);
            if (string.IsNullOrEmpty(openid))
            {
                LogUtil.Log("无法获取openid, 直接丢弃");
                return;
            }

            switch (msg.msgType)
            {
                case 20009: //最终礼物
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(msg.payload));
                    var payLoad = JsonConvert.DeserializeObject<WeiShiMsgEntry.PayLoad>(json);
                    if (payLoad == null)
                    {
                        return;
                    }
                    EnqueueMsg(new LiveMsgGift
                    {
                        uid = openid,
                        name = msg.nickname,
                        icon = msg.headUrl,
                        gift = payLoad.reward_gift.name,
                        num = payLoad.combo_product_count
                    });
                    break;
                }
                case 20013: //礼物
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(msg.payload));
                    var payLoad = JsonConvert.DeserializeObject<WeiShiMsgEntry.PayLoad>(json);
                    if (payLoad == null)
                    {
                        return;
                    }
                    EnqueueMsg(new LiveMsgGift
                    {
                        uid = openid,
                        name = msg.nickname,
                        icon = msg.headUrl,
                        gift = payLoad.reward_gift.name,
                        num = payLoad.combo_product_count
                    });
                    break;
                }
                case 20031: //升级
                    break;
            }
        }

        private async Task<string> GetUid(WeiShiMsgEntry msg)
        {
            var info = msg.GetInfo();
            var json = JsonConvert.SerializeObject(info);
            var md5 = Util.MD5(json);
            if (cacheUid.TryGetValue(md5, out var uid))
            {
                return uid;
            }

            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var tm = Convert.ToInt32(ts.TotalSeconds);

            var heads = new Dictionary<string, string>
            {
                { "x-aiya-md5", md5 },
                { "x-aiya-platform", _plat.ToString() },
                { "x-aiya-url", _flag },
                { "x-aiya-tm", tm.ToString() },
                { "x-aiya-signature", Util.MD5(_flag + _plat + tm + md5 + signKey) }
            };

            uid = await Post(uidUrl, json, heads);
            if (string.IsNullOrEmpty(uid))
            {
                return string.Empty;
            }

            cacheUid.TryAdd(md5, uid);
            return uid;
        }

        private static async Task<string> Post(string url, string data, Dictionary<string, string> heads = null)
        {
            var content = new StringContent(data, Encoding.UTF8, "application/json");

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
    }
}