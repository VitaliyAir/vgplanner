using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class ExpandTaskLogNoteLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "VTaskLogs",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "VTaskLogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20000)",
                oldMaxLength: 20000,
                oldNullable: true);
        }
    }
}
