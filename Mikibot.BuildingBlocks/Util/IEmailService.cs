using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.BuildingBlocks.Util
{
    public interface IEmailService
    {
        ValueTask SendEmail(string to, string subject, string textContent, string htmlContent, string filename, string base64content, CancellationToken token = default);
    }
}
