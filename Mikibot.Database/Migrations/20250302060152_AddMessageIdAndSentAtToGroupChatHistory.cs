using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mikibot.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageIdAndSentAtToGroupChatHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageId",
                table: "ChatbotGroupChatHistories",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SentAt",
                table: "ChatbotGroupChatHistories",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotGroupChatHistories_MessageId",
                table: "ChatbotGroupChatHistories",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatbotGroupChatHistories_MessageId",
                table: "ChatbotGroupChatHistories");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "ChatbotGroupChatHistories");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "ChatbotGroupChatHistories");
        }
    }
}
