using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.BuildingBlocks.Util
{
    public class SendGridEmailService : IEmailService
    {
        public SendGridEmailService()
        {
            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY")!;
            if (apiKey == null) throw new Exception("Sendgrid apikey not configured");

            var senderEmail = Environment.GetEnvironmentVariable("SENDGRID_SENDER_EMAIL") ?? "admin@remiliascarlet.com";
            var senderNick = Environment.GetEnvironmentVariable("SENDGRID_SENDER_NICKNAME") ?? "Mikibot";
            this.From = new EmailAddress(senderEmail, senderNick);

            this.SendGridClient = new SendGridClient(apiKey);
        }

        private EmailAddress From { get; }
        private SendGridClient SendGridClient { get; }

        public static EmailAddress Of(string email, string nick) => new(email, nick);

        public void AppendAttachment(SendGridMessage msg, Attachment attachment)
        {
            msg.AddAttachment(attachment);
        }

        public SendGridMessage CreateEmail(string to, string subject, string textContent, string htmlContent)
        {
            return MailHelper.CreateSingleEmail(From, new EmailAddress(to), subject, textContent, htmlContent);
        }

        public Attachment GenerateAttachment(string filename, string base64content)
        {
            return new Attachment
            {
                Content = base64content,
                Filename = filename,
            };
        }

        public async ValueTask SendEmail(SendGridMessage msg, CancellationToken token = default)
        {
            var res = await SendGridClient.SendEmailAsync(msg, token);
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException("SendGrid report an error while sending email");
        }

        public ValueTask SendEmail(string to, string subject, string textContent, string htmlContent, string filename, string base64content, CancellationToken token = default)
        {
            var email = CreateEmail(to, subject, textContent, htmlContent);
            AppendAttachment(email, GenerateAttachment(filename, base64content));

            return SendEmail(email, token);
        }
    }
}
