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

namespace siliu
{
    public class LiveDouYin : ILive
    {
        private IBrowser _browser;
        private IPage _page;

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
                //await _page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/106.0.0.0 Safari/537.36");
                var url = $"https://live.douyin.com/{anchor.uid}";
                await _page.DeleteCookieAsync();
                await _page.GoToAsync(url);
                var html = await _page.GetContentAsync();
                GetAnchorInfo(html);

                var session = await _page.Target.CreateCDPSessionAsync();
                await session.SendAsync("Network.enable");
                session.MessageReceived += OnMessageReceived;
                Debug.Log(url);
                IsLinked = true;
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

        private void OnMessageReceived(object sender, MessageEventArgs data)
        {
            if (!IsLinked)
            {
                return;
            }

            if (data.MessageID != "Network.webSocketFrameReceived")
            {
                return;
            }

            var payloadData = data.MessageData.Value<JToken>("response")?.Value<string>("payloadData");
            if (payloadData is null or "hi")
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

        private static Dictionary<long, string> specialGift = new Dictionary<long, string>
        {
            { 685, "粉丝灯牌" },
            { 3389, "欢乐盲盒" },
            { 4021, "欢乐拼图" },
            { 4353, "保时捷" },
            { 2193, "爱的守护" },
            { 3447, "亲吻" },
        };

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
                        var userIcon = user.AvatarThumb is { UrlList: { Count: > 0 } }
                            ? user.AvatarThumb.UrlList[0]
                            : "";

                        // Debug.Log($"[DanMu]: {user.Nickname}[{user.ShortId}]: {message.Content}");
                        EnqueueMsg(new LiveMsgDanMu
                        {
                            uid = user.ShortId.ToString(),
                            name = user.Nickname,
                            icon = userIcon,
                            msg = message.Content
                        });
                        break;
                    }
                    case "WebcastGiftMessage":
                    {
                        var message = GiftMessage.Parser.ParseFrom(msg.Payload);
                        var user = message.User;
                        var userIcon = user.AvatarThumb is { UrlList: { Count: > 0 } }
                            ? user.AvatarThumb.UrlList[0]
                            : "";
                        var gift = message.Gift;
                        string giftName;
                        var type = -1;
                        var gid = -1L;
                        if (gift != null && !string.IsNullOrEmpty(gift.Name))
                        {
                            giftName = gift.Name;
                            type = gift.Type;
                            gid = gift.Id;
                        }
                        else
                        {
                            specialGift.TryGetValue(message.GiftId, out giftName);
                        }

                        if ((gift == null && !string.IsNullOrEmpty(giftName)) || (gift != null && gift.Type != 1) ||
                            message.RepeatEnd == 1)
                        {
                            EnqueueMsg(new LiveMsgGift
                            {
                                uid = user.ShortId.ToString(),
                                name = user.Nickname,
                                icon = userIcon,
                                gift = giftName,
                                num = message.RepeatCount
                            });
                        }
                        else
                        {
                            LogUtil.Log(
                                $"[UnknownGift]: {user.Nickname}[{user.ShortId}]: GiftId: {message.GiftId}, Name: {giftName}, Type: {type}, Gid: {gid}, RepeatEnd: {message.RepeatEnd}, GroupCount: {message.GroupCount}, RepeatCount: {message.RepeatCount}, ComboCount: {message.ComboCount}");
                        }

                        break;
                    }
                    case "WebcastLikeMessage":
                    {
                        var message = LikeMessage.Parser.ParseFrom(msg.Payload);
                        var user = message.User;
                        var userIcon = user.AvatarThumb is { UrlList: { Count: > 0 } }
                            ? user.AvatarThumb.UrlList[0]
                            : "";

                        EnqueueMsg(new LiveMsgLike
                        {
                            uid = user.ShortId.ToString(),
                            name = user.Nickname,
                            icon = userIcon,
                            num = message.Count,
                        });
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
                    // case "WebcastLinkMicGuideMessage": break;
                    // case "WebcastLinkerContributeMessage": break;
                    // case "WebcastLuckyBoxEndMessage": break;
                    // case "WebcastRoomMessage": break;
                    // case "WebcastRoomNotifyMessage": break;
                    // case "WebcastPrivilegeScreenChatMessage": break;
                    // case "WebcastEmojiChatMessage": break;
                    // case "WebcastScreenChatMessage": break;
                    // case "WebcastBattleTeamTaskMessage": break;
                    // case "WebcastUpdateFanTicketMessage": break;
                    // case "WebcastLinkMicArmiesMethod": break;
                    // case "WebcastLuckyBoxMessage": break;
                    // case "WebcastBindingGiftMessage": break;
                    // case "WebcastLinkMicMethod": break;
                    // case "WebcastLinkMessage": break;
                    // case "WebcastProfitInteractionScoreMessage": break;
                    // case "LinkMicMethod": break;
                    // case "WebcastLinkMicBattleMethod": break;
                    // case "LinkMicBattleMethod": break;
                    // case "WebcastLotteryEventMessage": break;
                    // case "WebcastLotteryEventNewMessage": break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"parse [{msg.Method}] error: {e}");
            }
        }

        private void GetAnchorInfo(string html)
        {
            try
            {
                int title = html.IndexOf("<title>");
                int titleEnd = html.IndexOf("</title>");

                string titleStr = html.Substring(title, titleEnd - title);
                int anchorNameIndex = titleStr.IndexOf("的抖音直播间");
                string name = titleStr.Substring(7, anchorNameIndex - 7);
                string icon = null;
                var maybe = html.Split("<img src=");
                foreach (var str in maybe)
                {
                    if (str.IndexOf("aweme-avatar") != -1)
                    {
                        int end = str.IndexOf("?from");
                        int start = str.IndexOf("https:");
                        icon = str.Substring(start, end - start);
                        break;
                    }
                }

                Debug.Log("主播:" + name + " icon:" + icon);
            }
            catch (Exception ex)
            {
                Debug.Log("页面解析失败:--" + ex);
            }
        }
    }
}