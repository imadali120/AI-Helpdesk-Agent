using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAgents.HelpdeskAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningMechanism : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoryPolicyParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfidenceThreshold = table.Column<double>(type: "REAL", nullable: false),
                    TotalFeedbackCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IncorrectCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryPolicyParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeedbackEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TicketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OriginalCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalPriority = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectPriority = table.Column<int>(type: "INTEGER", nullable: false),
                    WasCategoryCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    WasPriorityCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryPolicyParameters_Category",
                table: "CategoryPolicyParameters",
                column: "Category",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackEntries_TicketId",
                table: "FeedbackEntries",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryPolicyParameters");

            migrationBuilder.DropTable(
                name: "FeedbackEntries");
        }
    }
}
