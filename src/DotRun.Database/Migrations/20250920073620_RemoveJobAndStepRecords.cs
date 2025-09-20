using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotRun.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJobAndStepRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobRun_Job_JobId",
                table: "JobRun");

            migrationBuilder.DropForeignKey(
                name: "FK_StepRun_Step_StepId",
                table: "StepRun");

            migrationBuilder.DropTable(
                name: "Step");

            migrationBuilder.DropTable(
                name: "Job");

            migrationBuilder.DropIndex(
                name: "IX_StepRun_StepId",
                table: "StepRun");

            migrationBuilder.DropIndex(
                name: "IX_JobRun_JobId",
                table: "JobRun");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "StepRun");

            migrationBuilder.DropColumn(
                name: "StepId",
                table: "StepRun");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "JobRun");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "StepRun",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "JobRun",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "StepRun");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "JobRun");

            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "StepRun",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StepId",
                table: "StepRun",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "JobRun",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Job",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Job_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Step",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Step", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Step_Job_JobId",
                        column: x => x.JobId,
                        principalTable: "Job",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StepRun_StepId",
                table: "StepRun",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRun_JobId",
                table: "JobRun",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_ProjectId",
                table: "Job",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Step_JobId",
                table: "Step",
                column: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobRun_Job_JobId",
                table: "JobRun",
                column: "JobId",
                principalTable: "Job",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StepRun_Step_StepId",
                table: "StepRun",
                column: "StepId",
                principalTable: "Step",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
