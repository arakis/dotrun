using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotRun.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsActiveWithDeactivatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workflow_Project_ProjectId",
                table: "Workflow");

            migrationBuilder.DropIndex(
                name: "IX_Workflow_ProjectId",
                table: "Workflow");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Workflow");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Workflow");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeactivatedAt",
                table: "Workflow",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualifiedPath",
                table: "Workflow",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceHash",
                table: "Workflow",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "YamlFilePath",
                table: "Workflow",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_QualifiedPath",
                table: "Workflow",
                column: "QualifiedPath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workflow_QualifiedPath",
                table: "Workflow");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Workflow");

            migrationBuilder.DropColumn(
                name: "QualifiedPath",
                table: "Workflow");

            migrationBuilder.DropColumn(
                name: "SourceHash",
                table: "Workflow");

            migrationBuilder.DropColumn(
                name: "YamlFilePath",
                table: "Workflow");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Workflow",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Workflow",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Workflow_ProjectId",
                table: "Workflow",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workflow_Project_ProjectId",
                table: "Workflow",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
