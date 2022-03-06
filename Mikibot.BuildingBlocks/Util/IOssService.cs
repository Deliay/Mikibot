using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.BuildingBlocks.Util
{
    public interface IOssService
    {
        Task<string> Upload(string fileName);
    }
}
