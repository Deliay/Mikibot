using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mikibot.Database.Migrations
{
    /// <inheritdoc />
    public partial class DefineCharacterTableInContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatbotCharacter",
                table: "ChatbotCharacter");

            migrationBuilder.RenameTable(
                name: "ChatbotCharacter",
                newName: "ChatbotCharacters");

            migrationBuilder.RenameIndex(
                name: "IX_ChatbotCharacter_GroupId",
                table: "ChatbotCharacters",
                newName: "IX_ChatbotCharacters_GroupId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatbotCharacters",
                table: "ChatbotCharacters",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatbotCharacters",
                table: "ChatbotCharacters");

            migrationBuilder.RenameTable(
                name: "ChatbotCharacters",
                newName: "ChatbotCharacter");

            migrationBuilder.RenameIndex(
                name: "IX_ChatbotCharacters_GroupId",
                table: "ChatbotCharacter",
                newName: "IX_ChatbotCharacter_GroupId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatbotCharacter",
                table: "ChatbotCharacter",
                column: "Id");
        }
    }
}
