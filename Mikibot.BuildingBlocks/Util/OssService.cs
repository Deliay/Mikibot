using COSXML;
using COSXML.Auth;
using COSXML.Model.Tag;
using COSXML.Transfer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using static COSXML.Transfer.COSXMLUploadTask;

namespace Mikibot.BuildingBlocks.Util
{
    public class OssService : IOssService
    {
        private string SecretId { get; }
        private string SecretKey { get; }
        private string Bucket { get; }
        private string AppId { get; }
        private string BucketAppId { get; }
        private DefaultQCloudCredentialProvider CosCredentialProvider { get; }
        private ILogger<OssService> Logger { get; }
        private string Region { get; }
        private CosXmlConfig Config { get; }
        private CosXml CosXml { get; }

        public OssService(ILogger<OssService> logger)
        {
            this.Region = "ap-nanjing";
            this.SecretId = Environment.GetEnvironmentVariable("COS_SECRET_ID")!;
            this.SecretKey = Environment.GetEnvironmentVariable("COS_SECRET_KEY")!;
            this.Bucket = Environment.GetEnvironmentVariable("COS_BUCKET")!;
            this.AppId = Environment.GetEnvironmentVariable("COS_APP_ID")!;
            this.BucketAppId = $"{Bucket}-{AppId}";

            if (SecretId == null || SecretKey == null)
            {
                throw new Exception("Oss not initialized");
            }
            this.CosCredentialProvider = new DefaultQCloudCredentialProvider(SecretId, SecretKey, 600);
            Logger = logger;
            this.Config = new CosXmlConfig.Builder()
               .IsHttps(true)
               .SetRegion(Region)
               .SetDebugLog(true)
               .Build();
            this.CosXml = new CosXmlServer(Config, CosCredentialProvider);
        }

        private static TransferConfig TransferConfig = new();
        public async Task<string> Upload(string fileName)
        {
            string name = Path.GetFileName(fileName) ?? $"{Guid.NewGuid()}.flv";
            COSXMLUploadTask uploadTask = new(BucketAppId, name);
            uploadTask.SetSrcPath(fileName);
            uploadTask.progressCallback = (c, t) => Logger.LogInformation(string.Format("切片上传 {0:##.##}%", c * 100.0 / t));

            TransferManager transferManager = new(CosXml, TransferConfig);
            UploadTaskResult result = await transferManager.UploadAsync(uploadTask);
            return GetDownloadAddress(name);
        }

        private string GetDownloadAddress(string key)
        {
            return CosXml.GenerateSignURL(new PreSignatureStruct
            {
                bucket = Bucket,
                region = Region,
                appid = AppId,
                httpMethod = "GET",
                key = key,
                isHttps = true,
                signDurationSecond = (int)TimeSpan.FromHours(6).TotalSeconds,
            });
        }
    }
}
