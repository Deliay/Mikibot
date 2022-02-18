using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mikibot.Analyze.Migrations
{
    public partial class AddBidField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bid",
                table: "LiveStatuses",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Bid",
                table: "FollowerStatistic",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LiveStatuses_Bid",
                table: "LiveStatuses",
                column: "Bid");

            migrationBuilder.CreateIndex(
                name: "IX_FollowerStatistic_Bid",
                table: "FollowerStatistic",
                column: "Bid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LiveStatuses_Bid",
                table: "LiveStatuses");

            migrationBuilder.DropIndex(
                name: "IX_FollowerStatistic_Bid",
                table: "FollowerStatistic");

            migrationBuilder.DropColumn(
                name: "Bid",
                table: "LiveStatuses");

            migrationBuilder.DropColumn(
                name: "Bid",
                table: "FollowerStatistic");
        }
    }
}
