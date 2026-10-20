using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Links",
                table: "VTasks",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Links",
                table: "VTasks");
        }
    }
}
