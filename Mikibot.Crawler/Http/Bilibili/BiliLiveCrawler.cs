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

        public async ValueTask<int> GetRealRoomId(int roomId, CancellationToken token = default)
        {
            var roomUrl = $"http://api.live.bilibili.com/room/v1/Room/room_init?id={roomId}";
            var roomResult = await GetAsync<BilibiliApiResponse<LiveInitInfo>>(roomUrl, token);
            roomResult.AssertCode();

            return roomResult.Data.RoomId;
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
            var result = await GetAsync<BilibiliApiResponse<List<LiveStreamAddress>>>(url, token);
            result.AssertCode();

            return result.Data;
        }
    }
}
