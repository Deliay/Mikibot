using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mikibot.Mirai.Migrations
{
    public partial class AddIndexToFollowerStatistics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FollowerStatistic_Bid",
                table: "FollowerStatistic");

            migrationBuilder.CreateIndex(
                name: "IX_FollowerStatistic_Bid_CreatedAt",
                table: "FollowerStatistic",
                columns: new[] { "Bid", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FollowerStatistic_Bid_CreatedAt",
                table: "FollowerStatistic");

            migrationBuilder.CreateIndex(
                name: "IX_FollowerStatistic_Bid",
                table: "FollowerStatistic",
                column: "Bid");
        }
    }
}
