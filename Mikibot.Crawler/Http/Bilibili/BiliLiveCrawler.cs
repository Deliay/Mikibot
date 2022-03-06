using Mikibot.Crawler.Http.Bilibili.Model;
using Mikibot.Crawler.Http.Bilibili.Model.LiveServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili
{
    public class BiliLiveCrawler : HttpCrawler
    {
        public const int mxmkr = 21672023;
        public const int mxmk = 477317922;
        public const string mxmks = "477317922";

        public async ValueTask<PersonalInfo> GetPersonalInfo(int uid, CancellationToken token = default)
        {
            var result = await GetAsync<BilibiliApiResponse<PersonalInfo>>($"https://api.bilibili.com/x/space/acc/info?mid={uid}", token);
            result.AssertCode();

            return result.Data;
        }

        public async ValueTask<int> GetFollowerCount(int uid, CancellationToken token = default)
        {
            var result = await GetAsync<BilibiliApiResponse<StatInfo>>($"https://api.bilibili.com/x/relation/stat?vmid={uid}", token);
            result.AssertCode();

            return result.Data.Follower;
        }

        public async ValueTask<LiveInitInfo> GetRealRoomInfo(int roomId, CancellationToken token = default)
        {
            if (roomIdMappingCache.ContainsKey(roomId))
            {
                return roomIdMappingCache[roomId];
            }
            var roomUrl = $"http://api.live.bilibili.com/room/v1/Room/room_init?id={roomId}";
            var roomResult = await GetAsync<BilibiliApiResponse<LiveInitInfo>>(roomUrl, token);
            roomResult.AssertCode();

            roomIdMappingCache.Add(roomId, roomResult.Data);
            return roomResult.Data;
        }

        public async ValueTask<int> GetRealRoomId(int roomId, CancellationToken token = default)
        {
            return (await GetRealRoomInfo(roomId, token)).RoomId;
        }

        public async ValueTask<LiveToken> GetLiveTokenByUid(int uid, CancellationToken token = default)
        {
            var personal = await GetPersonalInfo(uid, token);

            return await GetLiveToken(personal.LiveRoom.RoomId, token);
        }

        public async ValueTask<LiveToken> GetLiveToken(int roomId, CancellationToken token = default)
        {
            var tokenUrl = $"http://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?id={roomId}";
            var result = await GetAsync<BilibiliApiResponse<LiveToken>>(tokenUrl, token);
            result.AssertCode();

            return result.Data;
        }

        public async ValueTask<List<LiveStreamAddress>> GetLiveStreamAddress(int roomid, CancellationToken token = default)
        {
            var url = $"http://api.live.bilibili.com/room/v1/Room/playUrl?cid={roomid}&platform=web&quality=4&qn=400";
            var result = await GetAsync<BilibiliApiResponse<LiveStreamAddresses>>(url, token);
            result.AssertCode();

            return result.Data.Urls;
        }

        private static readonly Dictionary<int, LiveInitInfo> roomIdMappingCache = new();
        public async ValueTask<GuardInfo> GetRoomGuardList(int roomId, int page = 1, CancellationToken token = default)
        {
            var roomInfo = await GetRealRoomInfo(roomId, token);
            return await GetRoomGuardList(roomInfo.RoomId, roomInfo.BId, page, token);
        }

        public async ValueTask<GuardInfo> GetRoomGuardList(int roomId, int bId, int page = 1, CancellationToken token = default)
        {
            var url = $"http://api.live.bilibili.com/xlive/app-room/v2/guardTab/topList?roomid={roomId}&page={page}&ruid={bId}&page_size=29";
            var result = await GetAsync<BilibiliApiResponse<GuardInfo>>(url, token);
            result.AssertCode();

            return result.Data;
        }
    }
}
