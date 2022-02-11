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

        public async ValueTask<LiveToken> GetLiveTokenByUid(int uid, CancellationToken token = default)
        {
            var personal = await GetPersonalInfo(uid, token);

            return await GetLiveToken(personal.LiveRoom.RoomId, token);
        }

        public async ValueTask<LiveToken> GetLiveToken(int roomId, CancellationToken token = default)
        {
            var roomUrl = $"http://api.live.bilibili.com/room/v1/Room/room_init?id={roomId}";
            var roomResult = await GetAsync<BilibiliApiResponse<LiveInitInfo>>(roomUrl, token);

            var realRoomId = roomResult.Data.RoomId;

            var tokenUrl = $"http://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?id={realRoomId}";
            var result = await GetAsync<BilibiliApiResponse<LiveToken>>(tokenUrl, token);

            return result.Data;
        }
    }
}
