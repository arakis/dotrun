using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotRun.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:execution_status", "queued,pending,started,failed,succeeded,cancelled,skipped")
                .OldAnnotation("Npgsql:Enum:execution_status", "started,queued,succeeded,cancelled,failed");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Workflow",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRunAt",
                table: "Workflow",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Project",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Workflow");

            migrationBuilder.DropColumn(
                name: "LastRunAt",
                table: "Workflow");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Project");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:execution_status", "started,queued,succeeded,cancelled,failed")
                .OldAnnotation("Npgsql:Enum:execution_status", "queued,pending,started,failed,succeeded,cancelled,skipped");
        }
    }
}
