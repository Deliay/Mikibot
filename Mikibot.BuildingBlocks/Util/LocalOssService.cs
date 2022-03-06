using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.BuildingBlocks.Util
{
    public class LocalOssService : IOssService
    {
        public Task<string> Upload(string fileName)
        {
            return Task.FromResult(fileName);
        }
    }
}
