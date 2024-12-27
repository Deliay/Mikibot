using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct BilibiliApiResponse<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        
        [JsonPropertyName("data")]
        public T Data { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; }

        public void AssertCode()
        {
            if (Code != 0)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
