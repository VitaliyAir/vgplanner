using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDayStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VDayStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    GoalsTotal = table.Column<int>(type: "integer", nullable: false),
                    TasksNew = table.Column<int>(type: "integer", nullable: false),
                    TasksInProgress = table.Column<int>(type: "integer", nullable: false),
                    TasksCompleted = table.Column<int>(type: "integer", nullable: false),
                    TasksOnHold = table.Column<int>(type: "integer", nullable: false),
                    TasksCancelled = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VDayStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VDayStatistics_VUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "VUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VDayStatistics_UserId_Date",
                table: "VDayStatistics",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VDayStatistics");
        }
    }
}
