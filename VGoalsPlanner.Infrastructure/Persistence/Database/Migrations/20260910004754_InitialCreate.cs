using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VUsers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VUsers", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangesJson = table.Column<string>(type: "jsonb", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_VUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "VUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    SpecialNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VDays_VUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "VUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ParentGoalId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VGoals_VGoals_ParentGoalId",
                        column: x => x.ParentGoalId,
                        principalTable: "VGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VGoals_VUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "VUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DayId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VNotifications_VDays_DayId",
                        column: x => x.DayId,
                        principalTable: "VDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VNotifications_VUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "VUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'[]'::jsonb"),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CompletedPercent = table.Column<int>(type: "integer", nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VTasks_VGoals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "VGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VTasks_VUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "VUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VDayVTasks",
                columns: table => new
                {
                    VDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    VTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VDayVTasks", x => new { x.VDayId, x.VTaskId });
                    table.ForeignKey(
                        name: "FK_VDayVTasks_VDays_VDayId",
                        column: x => x.VDayId,
                        principalTable: "VDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VDayVTasks_VTasks_VTaskId",
                        column: x => x.VTaskId,
                        principalTable: "VTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VTaskLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Attachments = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'[]'::jsonb"),
                    TimeSpentMinutes = table.Column<int>(type: "integer", nullable: true),
                    OldStatus = table.Column<int>(type: "integer", nullable: true),
                    NewStatus = table.Column<int>(type: "integer", nullable: true),
                    VTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VTaskLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VTaskLogs_VDays_DayId",
                        column: x => x.DayId,
                        principalTable: "VDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VTaskLogs_VTasks_VTaskId",
                        column: x => x.VTaskId,
                        principalTable: "VTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VTaskLogs_VUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "VUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "VUsers",
                columns: new[] { "UserId", "UserName" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "test-user" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                table: "AuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType",
                table: "AuditLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VDays_UserId_Date",
                table: "VDays",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VDayVTasks_VDayId",
                table: "VDayVTasks",
                column: "VDayId");

            migrationBuilder.CreateIndex(
                name: "IX_VDayVTasks_VTaskId",
                table: "VDayVTasks",
                column: "VTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_VGoals_ParentGoalId",
                table: "VGoals",
                column: "ParentGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_VGoals_Status",
                table: "VGoals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VGoals_UserId",
                table: "VGoals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VGoals_UserId_ParentGoalId",
                table: "VGoals",
                columns: new[] { "UserId", "ParentGoalId" });

            migrationBuilder.CreateIndex(
                name: "IX_VNotifications_DayId",
                table: "VNotifications",
                column: "DayId");

            migrationBuilder.CreateIndex(
                name: "IX_VNotifications_IsRead",
                table: "VNotifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_VNotifications_UserId",
                table: "VNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VTaskLogs_CreatedByUserId",
                table: "VTaskLogs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VTaskLogs_DayId",
                table: "VTaskLogs",
                column: "DayId");

            migrationBuilder.CreateIndex(
                name: "IX_VTaskLogs_VTaskId",
                table: "VTaskLogs",
                column: "VTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_VTasks_Deadline",
                table: "VTasks",
                column: "Deadline");

            migrationBuilder.CreateIndex(
                name: "IX_VTasks_GoalId",
                table: "VTasks",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_VTasks_Status",
                table: "VTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VTasks_UserId",
                table: "VTasks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VTasks_UserId_GoalId",
                table: "VTasks",
                columns: new[] { "UserId", "GoalId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "VDayVTasks");

            migrationBuilder.DropTable(
                name: "VNotifications");

            migrationBuilder.DropTable(
                name: "VTaskLogs");

            migrationBuilder.DropTable(
                name: "VDays");

            migrationBuilder.DropTable(
                name: "VTasks");

            migrationBuilder.DropTable(
                name: "VGoals");

            migrationBuilder.DropTable(
                name: "VUsers");
        }
    }
}
