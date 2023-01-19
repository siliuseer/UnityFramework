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
    public string roomId;
    private IBrowser _browser;
    private IPage _page;
    public int Status { get; private set; }

    private class ByteEntry
    {
        public string name;
        public byte[] bytes;
    }

    private string byteDir;

    private void Awake()
    {
        var dir = Application.dataPath + $"/../bytes/{roomId}";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        byteDir = dir;
        LogUtil.Init(roomId);
        StartPuppeteer();
    }

    private async void StartPuppeteer()
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

            var url = $"https://live.douyin.com/{roomId}";
            _page = await _browser.NewPageAsync();
            await _page.DeleteCookieAsync();
            await _page.GoToAsync(url);
            var session = await _page.Target.CreateCDPSessionAsync();
            await session.SendAsync("Network.enable");
            session.MessageReceived += this.OnMessageReceived;
            Status = 1;
            LogUtil.Log(url);
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

        LogUtil.Log("[DouYin] Destroy!");
        LogUtil.Dispose();
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

        if (payloadData == "hi")
        {
            LogUtil.Warning(payloadData);
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
            LogUtil.Warning(payloadData);
            Debug.LogWarning(e);
        }
    }

    private void DecodeMessage(Message msg)
    {
        try
        {
            var bytes = msg.Payload.ToByteArray();
            LogUtil.Log($"[{msg.Method}]: {bytes.Length}");
            if (!count.TryGetValue(msg.Method, out var num))
            {
                num = 0;
            }
            else
            {
                num++;
            }

            count[msg.Method] = num;
            Directory.CreateDirectory($"{byteDir}/{msg.Method}");
            File.WriteAllBytes($"{byteDir}/{msg.Method}/{num}_{DateTime.Now:yyyyMMdd_HHmmss}.bin", bytes);

            switch (msg.Method)
            {
                case "WebcastChatMessage":
                {
                    var message = ChatMessage.Parser.ParseFrom(msg.Payload);
                    var user = message.User;
                    Debug.Log($"[DanMu]: {user.Nickname}[{user.ShortId}]: {message.Content}");
                    Enqueue(new DouYinJson
                    {
                        type = 1,
                        uid = user.ShortId.ToString(),
                        name = user.Nickname,
                        msg = message.Content
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

                    if ((gift != null && gift.Type != 1) || message.RepeatEnd == 1)
                    {
                        LogUtil.Log($"[Gift]: {user.Nickname}[{user.ShortId}]: GiftId: {message.GiftId}, Name: {giftName}, RepeatEnd: {message.RepeatEnd}, GroupCount: {message.GroupCount}, RepeatCount: {message.RepeatCount}, ComboCount: {message.ComboCount}");
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
                    var message = MemberMessage.Parser.ParseFrom(msg.Payload);
                    // LogUtil.Log($"[进入] {message.User.Nickname} 进入直播间");
                    break;
                }
                case "WebcastSocialMessage":
                {
                    var message = SocialMessage.Parser.ParseFrom(msg.Payload);
                    // LogUtil.Log($"[关注] {message.User.Nickname} 关注了主播");
                    break;
                }
                case "WebcastLikeMessage":
                {
                    var message = LikeMessage.Parser.ParseFrom(msg.Payload);
                    // LogUtil.Log($"[点赞] {message.User.Nickname} 点赞了直播间({message.Count})");
                    break;
                }
                case "WebcastRoomUserSeqMessage":
                {
                    var message = RoomUserSeqMessage.Parser.ParseFrom(msg.Payload);
                    // LogUtil.Log($"[观看人数]: {message.Total}/ {message.TotalUser}");
                    break;
                }
                case "WebcastControlMessage":
                {
                    var message = ControlMessage.Parser.ParseFrom(msg.Payload);
                    // LogUtil.Log($"[Control] {message.Common.User.Nickname}");
                    break;
                }
                case "WebcastFansclubMessage":
                {
                    var message = FansclubMessage.Parser.ParseFrom(msg.Payload);
                    // LogUtil.Log($"[Fansclub] {message.Content}");
                    break;
                }
                case "WebcastRoomMessage": //房间信息
                case "WebcastBindingGiftMessage": //礼物达到数量特殊效果
                case "WebcastRoomStatsMessage":
                case "WebcastRoomRankMessage":
                case "WebcastInRoomBannerMessage":
                case "WebcastScreenChatMessage":
                    break;
                // case "WebcastRoomNotifyMessage":
                // case "WebcastPrivilegeScreenChatMessage":
                // case "WebcastEmojiChatMessage":
                // case "WebcastLuckyBoxMessage":
                // case "WebcastLuckyBoxEndMessage":
                // case "WebcastLinkMicArmiesMethod":
                // case "WebcastLinkMicMethod":
                // case "WebcastLinkMessage":
                // case "WebcastProfitInteractionScoreMessage":
                // case "WebcastLinkMicBattleMethod":
                // case "WebcastLotteryEventMessage":
                // case "WebcastLotteryEventNewMessage":
                // case "WebcastCommonTextMessage":
                // case "WebcastRoomDataSyncMessage":
                // case "WebcastPullStreamUpdateMessage":
                // case "WebcastGameCPUserDownloadMessage":
                // case "WebcastBattleTeamTaskMessage":
                // case "WebcastUpdateFanTicketMessage":
                // case "LinkMicMethod":
                // case "LinkMicBattleMethod":
                // case "WebcastLinkMicGuideMessage":
                // case "WebcastLinkerContributeMessage":
                //     break;
                default:
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"parse [{msg.Method}] error: {e}");
        }
    }

    private Dictionary<string, int> count = new Dictionary<string, int>();

    private void Enqueue(DouYinJson data)
    {
        lock (_queue)
        {
            _queue.Enqueue(data);
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
    }
}