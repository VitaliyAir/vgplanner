using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGoalsPlanner.Infrastructure.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentStorageObjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StorageObjectId",
                table: "VTaskAttachments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorageObjectId",
                table: "VTaskAttachments");
        }
    }
}
