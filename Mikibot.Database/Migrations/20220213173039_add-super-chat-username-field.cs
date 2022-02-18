using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mikibot.Migrations
{
    public partial class addsuperchatusernamefield : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "LiveSuperChats",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserName",
                table: "LiveSuperChats");
        }
    }
}
