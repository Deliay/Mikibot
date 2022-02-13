using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mikibot.Analyze.Migrations
{
    public partial class addmanydatabasemodelagain : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Msg",
                table: "LiveDanmakus",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Msg",
                table: "LiveDanmakus");
        }
    }
}
