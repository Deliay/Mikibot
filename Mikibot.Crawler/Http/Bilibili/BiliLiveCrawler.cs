using Mikibot.Crawler.Http.Bilibili.Model;
using Mikibot.Crawler.Http.Bilibili.Model.LiveServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili
{
    public class BiliLiveCrawler : HttpCrawler
    {
        public const int mxmkr = 21672023;
        public const int mxmk = 477317922;
        public const string mxmks = "477317922";

        public HttpClient Client => client;

        public long Uid { get; set; }

        [Obsolete("不让抓了")]
        public async ValueTask<PersonalInfo> GetPersonalInfo(long uid, CancellationToken token = default)
        {
            var result = await GetAsync<BilibiliApiResponse<PersonalInfo>>($"https://api.bilibili.com/x/space/acc/info?mid={uid}", token);
            result.AssertCode();

            return result.Data;
        }

        public async ValueTask<long> GetFollowerCount(long uid, CancellationToken token = default)
        {
            var result = await GetAsync<BilibiliApiResponse<StatInfo>>($"https://api.bilibili.com/x/relation/stat?vmid={uid}", token);
            result.AssertCode();

            return result.Data.Follower;
        }

        [Obsolete("不让抓了")]
        public async ValueTask<PersonalInfo.LiveRoomDetail> GetPersonalLiveRoomDetail(long uid, CancellationToken token = default)
        {
            var result = await GetAsync<BilibiliApiResponse<PersonalInfo.LiveRoomDetail>>(
                $"https://api.live.bilibili.com/room/v1/Room/getRoomInfoOld?mid={uid}", token);
            result.AssertCode();

            return result.Data;
        }

        public async ValueTask<LiveInitInfo> GetRealRoomInfo(long roomId, CancellationToken token = default)
        {
            if (roomIdMappingCache.TryGetValue(roomId, out LiveInitInfo value))
            {
                return value;
            }
            var roomUrl = $"http://api.live.bilibili.com/room/v1/Room/room_init?id={roomId}";
            var roomResult = await GetAsync<BilibiliApiResponse<LiveInitInfo>>(roomUrl, token);
            roomResult.AssertCode();

            roomIdMappingCache.TryAdd(roomId, roomResult.Data);
            return roomResult.Data;
        }

        public async ValueTask<long> GetRealRoomId(long roomId, CancellationToken token = default)
        {
            return (await GetRealRoomInfo(roomId, token)).RoomId;
        }

        [Obsolete("不让抓了")]
        public async ValueTask<LiveToken> GetLiveTokenByUid(long uid, CancellationToken token = default)
        {
            var liveRoom = await GetPersonalLiveRoomDetail(uid, token);

            return await GetLiveToken(liveRoom.RoomId, token);
        }

        public async ValueTask<LiveToken> GetLiveToken(long roomId, CancellationToken token = default)
        {
            var tokenUrl = $"http://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?id={roomId}";
            var result = await GetAsync<BilibiliApiResponse<LiveToken>>(tokenUrl, token);
            result.AssertCode();

            return result.Data;
        }

        public async ValueTask<List<LiveStreamAddress>> GetLiveStreamAddress(long roomid, CancellationToken token = default)
        {
            var url = $"http://api.live.bilibili.com/room/v1/Room/playUrl?cid={roomid}&platform=web&quality=4&qn=400";
            var result = await GetAsync<BilibiliApiResponse<LiveStreamAddresses>>(url, token);
            result.AssertCode();

            return result.Data.Urls;
        }

        public async ValueTask<LiveStreamAddressesV2> GetLiveStreamAddressV2(long roomid, CancellationToken token = default)
        {
            var url = $"https://api.live.bilibili.com/xlive/web-room/v2/index/getRoomPlayInfo?room_id={roomid}&protocol=0,1&format=0,1,2&codec=0,1&qn=30000&platform=web&ptype=8&dolby=5&panorama=1";
            var result = await GetAsync<BilibiliApiResponse<LiveStreamAddressesV2>>(url, token);
            result.AssertCode();

            return result.Data;
        }

        private static readonly Dictionary<long, LiveInitInfo> roomIdMappingCache = new();
        public async ValueTask<GuardInfo> GetRoomGuardList(long roomId, int page = 1, CancellationToken token = default)
        {
            var roomInfo = await GetRealRoomInfo(roomId, token);
            return await GetRoomGuardList(roomInfo.RoomId, roomInfo.BId, page, token);
        }

        public async ValueTask<GuardInfo> GetRoomGuardList(long roomId, long bId, int page = 1, CancellationToken token = default)
        {
            var url = $"http://api.live.bilibili.com/xlive/app-room/v2/guardTab/topList?roomid={roomId}&page={page}&ruid={bId}&page_size=29";
            var result = await GetAsync<BilibiliApiResponse<GuardInfo>>(url, token);
            result.AssertCode();

            return result.Data;
        }

        public async ValueTask<LiveRoomInfo> GetLiveRoomInfo(long roomId, CancellationToken token = default)
        {
            var url = $"http://api.live.bilibili.com/room/v1/Room/get_info?room_id={roomId}";
            var result = await GetAsync<BilibiliApiResponse<LiveRoomInfo>>(url, token);
            result.AssertCode();

            return result.Data;
        }

        private static string GetDevId()
        {
            var array = Guid.NewGuid().ToByteArray();
            array[6] = (byte)(0x40 | (array[6] & 0xF0));
            array[8] = (byte)(3 & array[8] | 8);

            return new Guid(array).ToString();
        }

        public async ValueTask SendMessage(string cookie, long senderBid, long targetBid, string message, CancellationToken token = default)
        {
            var indexOfCsrf = cookie.IndexOf("bili_jct=") + 9;
            if (indexOfCsrf == -1) throw new InvalidDataException("Cookie not include any csrf token, consider update you cookie!");

            var csrf = cookie[indexOfCsrf..cookie.IndexOf(';', indexOfCsrf)];

            var body = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "msg[sender_uid]", $"{senderBid}" },
                { "msg[receiver_id]", $"{targetBid}" },
                { "msg[receiver_type]", "1" },
                { "msg[msg_type]", "1" },
                { "msg[dev_id]", GetDevId() },
                { "msg[timestamp]", $"{DateTimeOffset.Now.ToUnixTimeSeconds()}" },
                { "msg[content]", $"{{\"content\":{JsonSerializer.Serialize(message)}}}" },
                { "csrf", csrf },
            });
            body.Headers.Add("cookie", cookie);

            var res = await PostFormAsync<BilibiliApiResponse<object>>("http://api.vc.bilibili.com/web_im/v1/web_im/send_msg", body, token);
            res.AssertCode();
        }

        public async ValueTask SendDanmaku(string msg, long roomId, bool isEmoji, CancellationToken cancellationToken = default)
        {
            var cookie = client.DefaultRequestHeaders.GetValues("cookie").FirstOrDefault()!;
            var indexOfCsrf = cookie.IndexOf("bili_jct=") + 9;
            if (indexOfCsrf == -1) throw new InvalidDataException("Cookie not include any csrf token, consider update you cookie!");

            var csrf = cookie[indexOfCsrf..cookie.IndexOf(';', indexOfCsrf)];

            var args = new Dictionary<string, string>()
            {
                { "bubble", $"2" },
                { "msg", $"{msg}" },
                { "roomid", $"{roomId}" },
                { "color", "9920249" },
                { "mode", "4" },
                { "fontsize", "25" },
                { "csrf", csrf },
                { "csrf_token", csrf },
                { "rnd", $"{DateTimeOffset.Now.ToUnixTimeSeconds()}" },
            };

            if (isEmoji)
            {
                args.Add("dm_type", "1");
            }
            var body = new FormUrlEncodedContent(args);

            var res = await PostFormAsync<BilibiliApiResponse<object>>("http://api.vc.bilibili.com/web_im/v1/web_im/send_msg", body, cancellationToken);
            res.AssertCode();
        }
    }
}
