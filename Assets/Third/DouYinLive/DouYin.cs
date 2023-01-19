using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Douyin;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using UnityEngine;

public class DouYin : MonoBehaviour
{
    public static DouYin LinkStart(string roomId)
    {
        var go = new GameObject("[DouYin]");
        DontDestroyOnLoad(go);
        var douYin = go.AddComponent<DouYin>();
        douYin._roomId = roomId;
        return douYin;
    }

    private string _roomId;
    private IBrowser _browser;
    private IPage _page;
    public int Status { get; private set; }

    private async void Start()
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
            //await _page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/106.0.0.0 Safari/537.36");
            var url = $"https://live.douyin.com/{_roomId}";
            await _page.DeleteCookieAsync();
            await _page.GoToAsync(url);
            var session = await _page.Target.CreateCDPSessionAsync();
            await session.SendAsync("Network.enable");
            session.MessageReceived += this.OnMessageReceived;
            Status = 1;
            Debug.Log(url);
        }
        catch (Exception e)
        {
            Status = -1;
            Debug.LogError("启动Puppeteer失败: " + e);
        }
    }

    private void OnDestroy()
    {
        Status = 0;
        _page?.Dispose();
        _page = null;
        _browser?.Dispose();
        _browser = null;
        lock (_queue)
        {
            _queue.Clear();
        }

        Debug.Log("[DouYin] Destroy!");
    }

    private void OnMessageReceived(object sender, MessageEventArgs data)
    {
        if (Status < 1)
        {
            return;
        }

        if (data.MessageID != "Network.webSocketFrameReceived")
        {
            return;
        }

        var payloadData = data.MessageData.Value<JToken>("response")?.Value<string>("payloadData");
        if (payloadData == null)
        {
            return;
        }

        try
        {
            var bytes = Convert.FromBase64String(payloadData);
            var wss = WssResponse.Parser.ParseFrom(bytes);
            using var gZipInputStream = new GZipInputStream(new MemoryStream(wss.Data.ToByteArray()));
            using var outStream = new MemoryStream();
            var buf = new byte[1024 * 4];
            StreamUtils.Copy(gZipInputStream, outStream, buf);
            var dataBytes = outStream.ToArray();
            var response = Douyin.Response.Parser.ParseFrom(dataBytes);
            foreach (var message in response.Messages)
            {
                DecodeMessage(message);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    private void DecodeMessage(Message msg)
    {
        try
        {
            switch (msg.Method)
            {
                case "WebcastChatMessage":
                {
                    var message = ChatMessage.Parser.ParseFrom(msg.Payload);
                    var user = message.User;
                    string userIconUrl="";
                    if (user.AvatarThumb.UrlList.Count > 0)
                    {
                        userIconUrl = user.AvatarThumb.UrlList[0];
                    }

                    // Debug.Log($"[DanMu]: {user.Nickname}[{user.ShortId}]: {message.Content}");
                    Enqueue(new DouYinJson
                    {
                        type = 1,
                        uid = user.ShortId.ToString(),
                        name = user.Nickname,
                        msg = message.Content,
                        icon=userIconUrl
                    });
                    break;
                }
                case "WebcastGiftMessage":
                {
                    var message = GiftMessage.Parser.ParseFrom(msg.Payload);
                    var user = message.User;
                    var giftName = string.Empty;
                    var gift = message.Gift;
                    if (gift != null && !string.IsNullOrEmpty(gift.Name))
                    {
                        giftName = gift.Name;
                    }
                    else
                    {
                        switch (message.GiftId)
                        {
                            case 685:
                                giftName = "粉丝灯牌";
                                break;
                            case 3389:
                                giftName = "欢乐盲盒";
                                break;
                            case 4021:
                                giftName = "欢乐拼图";
                                break;
                        }
                    }
                    // Debug.Log($"[Gift]: {user.Nickname}[{user.ShortId}]: {giftName} x {message.GroupCount}, RepeatCount: {message.RepeatCount}, RepeatEnd: {message.RepeatEnd}, ComboCount: {message.ComboCount}");
                    if ((gift != null && gift.Type != 1) || message.RepeatEnd == 1)
                    {
                        Enqueue(new DouYinJson
                        {
                            type = 2,
                            uid = user.ShortId.ToString(),
                            name = user.Nickname,
                            msg = giftName,
                            num = message.RepeatCount.ToString()
                        });
                    }
                    break;
                }
                case "WebcastMemberMessage":
                {
                    // var message = MemberMessage.Parser.ParseFrom(msg.Payload);
                    break;
                }
                case "WebcastSocialMessage":
                {
                    // var message = SocialMessage.Parser.ParseFrom(msg.Payload);
                    break;
                }
                case "WebcastLikeMessage":
                {
                    // var message = LikeMessage.Parser.ParseFrom(msg.Payload);
                    break;
                }
                case "WebcastRoomUserSeqMessage":
                {
                    // var message = RoomUserSeqMessage.Parser.ParseFrom(msg.Payload);
                    break;
                }
                case "WebcastControlMessage":
                {
                    // var message = ControlMessage.Parser.ParseFrom(msg.Payload);
                    break;
                }
                case "WebcastFansclubMessage":
                {
                    // var message = FansclubMessage.Parser.ParseFrom(msg.Payload);
                    break;
                }
                case "WebcastLinkMicGuideMessage": break;
                case "WebcastLinkerContributeMessage": break;
                case "WebcastLuckyBoxEndMessage": break;
                case "WebcastRoomMessage": break;
                case "WebcastRoomNotifyMessage": break;
                case "WebcastPrivilegeScreenChatMessage": break;
                case "WebcastEmojiChatMessage": break;
                case "WebcastScreenChatMessage": break;
                case "WebcastBattleTeamTaskMessage": break;
                case "WebcastUpdateFanTicketMessage": break;
                case "WebcastLinkMicArmiesMethod": break;
                case "WebcastLuckyBoxMessage": break;
                case "WebcastBindingGiftMessage": break;
                case "WebcastLinkMicMethod": break;
                case "WebcastLinkMessage": break;
                case "WebcastProfitInteractionScoreMessage": break;
                case "LinkMicMethod": break;
                case "WebcastLinkMicBattleMethod": break;
                case "LinkMicBattleMethod": break;
                case "WebcastLotteryEventMessage": break;
                case "WebcastLotteryEventNewMessage": break;
                default:
                {
                    Debug.Log($"未知消息: {msg.Method}");
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"parse [{msg.Method}] error: {e}");
        }
    }

    private void Enqueue(DouYinJson data)
    {
        lock (_queue)
        {
            _queue.Enqueue(data);
        }
    }

    private void Update()
    {
        lock (_queue)
        {
            while (_queue.Count > 0)
            {
                var data = _queue.Dequeue();
                // switch (data.type)
                // {
                //     case 1: //弹幕
                //         CmdMgr.DealDm(data.uid, data.name, data.msg, data.icon);
                //         break;
                //     case 2: //礼物
                //         CmdMgr.DealGift(data.uid, data.name, data.msg, data.num);
                //         break;
                // }
            }
        }
    }

    private Queue<DouYinJson> _queue = new Queue<DouYinJson>();

    private class DouYinJson
    {
        public int type;
        public string uid;
        public string name;
        public string msg;
        public string num;
        public string icon;
    }
}