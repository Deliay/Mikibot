using Microsoft.Extensions.Logging;
using Mikibot.BuildingBlocks.Util;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.Http.Bilibili.Model;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using NPOI.XSSF.UserModel;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikibot.Analyze.Notification
{
    public class DanmakuExportGuardList
    {
        public DanmakuExportGuardList(ILogger<DanmakuExportGuardList> logger, IEmailService mailer, BiliLiveCrawler crawler)
        {
            Logger = logger;
            Mailer = mailer;
            Crawler = crawler;
        }

        public ILogger<DanmakuExportGuardList> Logger { get; }
        public IEmailService Mailer { get; }
        public BiliLiveCrawler Crawler { get; }

        private async Task<HashSet<GuardUserInfo>> GetGuards()
        {
            var allGuards = new HashSet<GuardUserInfo>();
            var init = await Crawler.GetRoomGuardList(roomId: BiliLiveCrawler.mxmkr, bId: BiliLiveCrawler.mxmk);
            allGuards.UnionWith(init.Top3);
            allGuards.UnionWith(init.List);
            while (init.List.Count > 0
                && init.Info.Count > allGuards.Count
                && init.Info.PageCount > init.Info.Current)
            {
                init = await Crawler.GetRoomGuardList(roomId: BiliLiveCrawler.mxmkr, bId: BiliLiveCrawler.mxmk, init.Info.Current + 1);
                allGuards.UnionWith(init.List);
            }

            return allGuards;
        }

        private static IEnumerable<string> ExcelHeader()
        {
            yield return "用户ID";
            yield return "用户名称";
            yield return "船员类型";
            yield return "房间排名";
            yield return "牌子等级";
            yield return "导出时是否在线";
        }

        private static IEnumerable<string> ExcelRow(GuardUserInfo userInfo)
        {
            yield return $"{userInfo.Uid}";
            yield return $"{userInfo.UserName}";
            yield return userInfo.GuardType switch { 3 => "舰长", 2 => "提督", 1 => "总督", _ => throw new Exception("数据异常") };
            yield return $"{userInfo.RoomRank}";
            yield return $"{userInfo.MedalInfo.Level}";
            yield return userInfo.Online == 1 ? "在线" : "不在";
        }

        private static IEnumerable<IEnumerable<string>> GenerateExcel(IEnumerable<GuardUserInfo> userInfos)
        {
            yield return ExcelHeader();
            foreach (var userInfo in userInfos)
                yield return ExcelRow(userInfo);
        }

        private static MemoryStream GenerateExcelFromGuardList(IEnumerable<GuardUserInfo> guards)
        {
            var memoryStream = new MemoryStream();
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("guards");

            int rowIdx = 1;
            foreach (var row in GenerateExcel(guards))
            {
                var excelRow = sheet.CreateRow(rowIdx);
                int columnIdx = 0;
                foreach (var col in row)
                {
                    excelRow.CreateCell(columnIdx).SetCellValue(col);
                    columnIdx++;
                }
                rowIdx++;
            }
            
            workbook.Write(memoryStream, leaveOpen: true);
            workbook.Close();
            return memoryStream;
        }

        private static readonly HashSet<int> allowList = new() { 477317922, 403496 };
        private static readonly string mxmke = "1154727918@qq.com";
        public async Task HandleDanmaku(DanmuMsg msg)
        {
            if (!msg.Msg.StartsWith("#导出舰长名单")) return;
            if (!allowList.Contains(msg.UserId)) return;

            Logger.LogInformation("正在下载舰长名单...");
            var guardList = await GetGuards();
            Logger.LogInformation("舰长名单下载完成!");

            var subject = $"(Mikibot) 舰长list";
            var text = $"弥希Miki: \n 你请求的舰长list已经生成完毕";
            var html = $"<p>{text.Replace("\n", "<br>")}</p>";

            var filename = $"guards-{msg.SentAt.LocalDateTime:yyyy-MM-dd-HH-mm-ss}.xlsx";
            using var excel = GenerateExcelFromGuardList(guardList);

            try
            {
                Logger.LogInformation("正在发送邮件");
                await Mailer.SendEmail(
                    to: mxmke,
                    subject: subject,
                    textContent: text,
                    htmlContent: html,
                    filename: filename,
                    base64content: Convert.ToBase64String(excel.GetBuffer()));

                Logger.LogInformation("舰长名单发送完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while sending email");
            }
        }
    }
}
