using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.BuildingBlocks.Util
{
    public interface IEmailService<TMsg, TAttachment>
    {
        TAttachment GenerateAttachment(string filename, string base64content);

        TMsg CreateEmail(string to, string subject, string textContent, string htmlContent);

        void AppendAttachment(TMsg msg, TAttachment attachment);

        ValueTask SendEmail(TMsg msg, CancellationToken token = default);
    }
}
