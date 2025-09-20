using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotRun.Migrations
{
    /// <inheritdoc />
    public partial class AddedSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "WorkflowRun",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "WorkflowRun",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Workflow",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "StepRun",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "JobRun",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "WorkflowRun");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "WorkflowRun");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Workflow");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "StepRun");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "JobRun");
        }
    }
}
