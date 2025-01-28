using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mikibot.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTableForSaveMessageHistories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_chatbotGroupChatHistories",
                table: "chatbotGroupChatHistories");

            migrationBuilder.DropIndex(
                name: "IX_chatbotGroupChatHistories_GroupId_UserId",
                table: "chatbotGroupChatHistories");

            migrationBuilder.RenameTable(
                name: "chatbotGroupChatHistories",
                newName: "ChatbotGroupChatHistories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatbotGroupChatHistories",
                table: "ChatbotGroupChatHistories",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotGroupChatHistories_GroupId_UserId_Id",
                table: "ChatbotGroupChatHistories",
                columns: new[] { "GroupId", "UserId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatbotGroupChatHistories",
                table: "ChatbotGroupChatHistories");

            migrationBuilder.DropIndex(
                name: "IX_ChatbotGroupChatHistories_GroupId_UserId_Id",
                table: "ChatbotGroupChatHistories");

            migrationBuilder.RenameTable(
                name: "ChatbotGroupChatHistories",
                newName: "chatbotGroupChatHistories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_chatbotGroupChatHistories",
                table: "chatbotGroupChatHistories",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_chatbotGroupChatHistories_GroupId_UserId",
                table: "chatbotGroupChatHistories",
                columns: new[] { "GroupId", "UserId" });
        }
    }
}
