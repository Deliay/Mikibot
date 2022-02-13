using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mikibot.Analyze.Migrations
{
    public partial class addmanydatabasemodel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Bid",
                table: "LiveDanmakus",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LiveBuyGuardLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Bid = table.Column<int>(type: "int", nullable: false),
                    Uid = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GuardLevel = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<int>(type: "int", nullable: false),
                    GiftName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BoughtAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveBuyGuardLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LiveGiftCombos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Bid = table.Column<int>(type: "int", nullable: false),
                    Uid = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComboId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComboNum = table.Column<int>(type: "int", nullable: false),
                    GiftName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalCoin = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveGiftCombos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LiveGifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Bid = table.Column<int>(type: "int", nullable: false),
                    Uid = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComboId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CoinType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiscountPrice = table.Column<int>(type: "int", nullable: false),
                    GiftName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SentAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveGifts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LiveGuardEnterLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Bid = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GuardLevel = table.Column<int>(type: "int", nullable: false),
                    CopyWriting = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnteredAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveGuardEnterLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LiveUserInteractiveLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Bid = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InteractedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    GuardLevel = table.Column<int>(type: "int", nullable: false),
                    MedalLevel = table.Column<int>(type: "int", nullable: false),
                    MedalName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FansTagUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveUserInteractiveLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LiveDanmakus_Bid_SentAt",
                table: "LiveDanmakus",
                columns: new[] { "Bid", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveBuyGuardLogs_Bid_BoughtAt",
                table: "LiveBuyGuardLogs",
                columns: new[] { "Bid", "BoughtAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveBuyGuardLogs_Bid_Uid",
                table: "LiveBuyGuardLogs",
                columns: new[] { "Bid", "Uid" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveGiftCombos_Bid_ComboId",
                table: "LiveGiftCombos",
                columns: new[] { "Bid", "ComboId" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveGiftCombos_Bid_CreatedAt",
                table: "LiveGiftCombos",
                columns: new[] { "Bid", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveGiftCombos_Bid_Uid",
                table: "LiveGiftCombos",
                columns: new[] { "Bid", "Uid" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveGifts_Bid_ComboId",
                table: "LiveGifts",
                columns: new[] { "Bid", "ComboId" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveGifts_Bid_SentAt",
                table: "LiveGifts",
                columns: new[] { "Bid", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveGifts_Bid_Uid",
                table: "LiveGifts",
                columns: new[] { "Bid", "Uid" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveGuardEnterLogs_Bid_EnteredAt",
                table: "LiveGuardEnterLogs",
                columns: new[] { "Bid", "EnteredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveGuardEnterLogs_Bid_UserId",
                table: "LiveGuardEnterLogs",
                columns: new[] { "Bid", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveUserInteractiveLogs_Bid_FansTagUserId",
                table: "LiveUserInteractiveLogs",
                columns: new[] { "Bid", "FansTagUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveUserInteractiveLogs_Bid_InteractedAt",
                table: "LiveUserInteractiveLogs",
                columns: new[] { "Bid", "InteractedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveUserInteractiveLogs_Bid_UserId",
                table: "LiveUserInteractiveLogs",
                columns: new[] { "Bid", "UserId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveBuyGuardLogs");

            migrationBuilder.DropTable(
                name: "LiveGiftCombos");

            migrationBuilder.DropTable(
                name: "LiveGifts");

            migrationBuilder.DropTable(
                name: "LiveGuardEnterLogs");

            migrationBuilder.DropTable(
                name: "LiveUserInteractiveLogs");

            migrationBuilder.DropIndex(
                name: "IX_LiveDanmakus_Bid_SentAt",
                table: "LiveDanmakus");

            migrationBuilder.AlterColumn<string>(
                name: "Bid",
                table: "LiveDanmakus",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
