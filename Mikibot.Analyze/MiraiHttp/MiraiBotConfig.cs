using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Mirai.MiraiHttp
{
    public struct MiraiBotConfig
    {
        public string Address { get; set; }
        public string Uid { get; set; }
        public string VerifyKey { get; set; }


        public static MiraiBotConfig FromEnviroment()
        {
            return new()
            {
                Address = Environment.GetEnvironmentVariable("MIRAI_ADDRESS"),
                VerifyKey = Environment.GetEnvironmentVariable("MIRAI_VERIFY_KEY"),
                Uid = Environment.GetEnvironmentVariable("MIKIBOT_QQ"),
            };
        }
    }
}
