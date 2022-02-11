using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Crawler.Http.Bilibili.Model
{
    public struct BilibiliApiResponse<T>
    {
        public int Code { get; set; }
        public T Data { get; set; }
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
