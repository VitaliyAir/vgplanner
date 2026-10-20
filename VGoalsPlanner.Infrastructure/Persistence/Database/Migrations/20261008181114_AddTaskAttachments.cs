using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VTaskAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VTaskAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VTaskAttachments_VTasks_VTaskId",
                        column: x => x.VTaskId,
                        principalTable: "VTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VTaskAttachments_VUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "VUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VTaskAttachments_UserId",
                table: "VTaskAttachments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VTaskAttachments_VTaskId",
                table: "VTaskAttachments",
                column: "VTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VTaskAttachments");
        }
    }
}
