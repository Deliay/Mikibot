using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TencentCloud.Common;
using TencentCloud.Common.Profile;
using TencentCloud.Ses.V20201002;
using TencentCloud.Ses.V20201002.Models;

namespace Mikibot.BuildingBlocks.Util
{
    public class TecentEmailService : IEmailService
    {
        public TecentEmailService()
        {
            this.SecretId = Environment.GetEnvironmentVariable("COS_SECRET_ID")!;
            this.SecretKey = Environment.GetEnvironmentVariable("COS_SECRET_KEY")!;
            if (this.SecretId == null) throw new Exception("tencent secret id/key not configured");

            Credential cred = new()
            {
                SecretId = SecretId,
                SecretKey = SecretKey,
            };
            ClientProfile clientProfile = new();
            HttpProfile httpProfile = new();
            httpProfile.Endpoint = ("ses.tencentcloudapi.com");
            clientProfile.HttpProfile = httpProfile;
            this.SenderEmail = Environment.GetEnvironmentVariable("SENDGRID_SENDER_EMAIL") ?? "mikibot@mail.remiliascarlet.com";

            this.Client = new SesClient(cred, "ap-hongkong", clientProfile);
        }

        private string SenderEmail { get; }
        private SesClient Client { get; }
        private string SecretId { get; }
        private string SecretKey { get; }

        public void AppendAttachment(SendEmailRequest msg, Attachment attachment)
        {
            msg.Attachments = new Attachment[] { attachment };
        }

        private static readonly Template Template = new()
        {
            TemplateData = string.Empty,
            TemplateID = 24413,
        };
        public SendEmailRequest CreateEmail(string to, string subject, string textContent, string htmlContent)
        {
            return new SendEmailRequest()
            {
                Subject = subject,
                Destination = new[] { to },
                FromEmailAddress = SenderEmail,
                Template = Template,
            };
        }

        public Attachment GenerateAttachment(string filename, string base64content)
        {
            return new Attachment()
            {
                Content = base64content,
                FileName = filename,
            };
        }

        public async ValueTask SendEmail(SendEmailRequest msg, CancellationToken token = default)
        {
            await Client.SendEmail(msg);
        }

        public ValueTask SendEmail(string to, string subject, string textContent, string htmlContent, string filename, string base64content, CancellationToken token = default)
        {
            var email = CreateEmail(to, subject, textContent, htmlContent);
            AppendAttachment(email, GenerateAttachment(filename, base64content));

            return SendEmail(email, token);
        }
    }
}
