using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mikibot.BuildingBlocks.Util;
using Mikibot.Crawler.Http.Bilibili;
using Mikibot.Crawler.Http.Bilibili.Model;
using Mikibot.Crawler.WebsocketCrawler.Data.Commands.KnownCommand;
using Mikibot.Database;
using Mikibot.Database.Model;
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
        private readonly MikibotDatabaseContext db = new(MySqlConfiguration.FromEnviroment());

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


        public async Task ExportGuardList(DanmuMsg msg)
        {
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
                    base64content: Convert.ToBase64String(excel.ToArray()));

                Logger.LogInformation("舰长名单发送完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while sending email");
            }
        }

        private static IEnumerable<string> VoxExcelHeader()
        {
            yield return "用户ID";
            yield return "用户名称";
            yield return "用户Bid";
            yield return "时间";
        }

        private static IEnumerable<string> VoxExcelRow(VoxList userInfo)
        {
            yield return $"{userInfo.Id}";
            yield return $"{userInfo.Name}";
            yield return $"{userInfo.Bid}";
            yield return $"{userInfo.CreatedAt}";
        }

        private static IEnumerable<IEnumerable<string>> GenerateVoxExcel(IEnumerable<VoxList> userInfos)
        {
            yield return VoxExcelHeader();
            foreach (var userInfo in userInfos)
                yield return VoxExcelRow(userInfo);
        }

        private static MemoryStream GenerateExcelFromVoxList(IEnumerable<VoxList> guards)
        {
            var memoryStream = new MemoryStream();
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("guards");

            int rowIdx = 1;
            foreach (var row in GenerateVoxExcel(guards))
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

        public async Task ExportVoxList(DanmuMsg msg)
        {
            if (!allowList.Contains(msg.UserId)) return;
            var voxList = await db.VoxList.ToListAsync();

            var subject = $"(Mikibot) 历史音声中奖名单";
            var text = $"弥希Miki: \n 你请求的历史音声中奖名单已经生成了";
            var html = $"<p>{text.Replace("\n", "<br>")}</p>";

            var filename = $"vox-list-{msg.SentAt.LocalDateTime:yyyy-MM-dd-HH-mm-ss}.xlsx";
            using var excel = GenerateExcelFromVoxList(voxList);

            try
            {
                Logger.LogInformation("正在发送邮件");
                await Mailer.SendEmail(
                    to: mxmke,
                    subject: subject,
                    textContent: text,
                    htmlContent: html,
                    filename: filename,
                    base64content: Convert.ToBase64String(excel.ToArray()));

                Logger.LogInformation("音声名单发送完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while sending email");
            }
        }

        public async Task HandleDanmaku(DanmuMsg msg)
        {
            if (msg.Msg.StartsWith("#导出舰长名单"))
            {
                await ExportGuardList(msg);
            }
            else if (msg.Msg.StartsWith("#导出音声名单"))
            {
                await ExportVoxList(msg);
            }
        }
    }
}
