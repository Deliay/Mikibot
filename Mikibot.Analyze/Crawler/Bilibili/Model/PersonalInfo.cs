using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Crawler.Bilibili.Model
{
    public struct PersonalInfo
    {
        public struct LiveRoomDetail
        {
            public int LiveStatus { get; set; }
            public string Title { get; set; }
            public string Url { get; set; }
            public string Cover { get; set; }
        }

        public LiveRoomDetail Live_Room { get; set; }
    }
}
