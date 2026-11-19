using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Migrations
{
    /// <summary>
    /// Разовый backfill авторов: до появления авторизации CreatedBy/UpdatedBy не заполнялись,
    /// поэтому у всех исторических строк там Guid.Empty. Автором считаем владельца записи
    /// (у VTaskLog он уже лежит в CreatedByUserId) — приложение однопользовательское на запись,
    /// так что владелец и есть тот, кто создавал и менял строку.
    /// Новые записи с этого момента заполняются в VGoalsPlannerDbContext.ApplyAuditAuthors().
    /// </summary>
    public partial class BackfillAuditAuthors : Migration
    {
        private const string EmptyGuid = "'00000000-0000-0000-0000-000000000000'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "VGoals", "VTasks", "VDays", "VNotifications", "VTaskAttachments", "VDayStatistics" })
            {
                migrationBuilder.Sql($"""
                    UPDATE "{table}"
                    SET "CreatedBy" = "UserId",
                        "UpdatedBy" = COALESCE("UpdatedBy", "UserId")
                    WHERE "CreatedBy" = {EmptyGuid};
                    """);
            }

            // У логов своё поле автора, UserId в таблице нет.
            migrationBuilder.Sql($"""
                UPDATE "VTaskLogs"
                SET "CreatedBy" = "CreatedByUserId",
                    "UpdatedBy" = COALESCE("UpdatedBy", "CreatedByUserId")
                WHERE "CreatedBy" = {EmptyGuid};
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Данные не откатываем: исторические строки после Up не отличить от новых,
            // а исходное состояние (Guid.Empty) не несёт информации.
        }
    }
}
