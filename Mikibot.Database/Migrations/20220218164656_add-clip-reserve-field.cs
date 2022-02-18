using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mikibot.Migrations
{
    public partial class addclipreservefield : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Reserve",
                table: "LiveStreamRecords",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reserve",
                table: "LiveStreamRecords");
        }
    }
}
