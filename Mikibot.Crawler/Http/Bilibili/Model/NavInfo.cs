using System.Text.Json.Serialization;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct NavInfo
    {
        [JsonPropertyName("mid")]
        public long Mid { get; set; }

        [JsonPropertyName("uname")]
        public string Name { get; set; }
    }
}