using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAgents.HelpdeskAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddExplanationToTicketEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "TicketEvents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "TicketEvents");
        }
    }
}
