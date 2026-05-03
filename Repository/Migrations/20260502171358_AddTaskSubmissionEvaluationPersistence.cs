using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskSubmissionEvaluationPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "SearchVector",
                table: "tasks",
                type: "vector(1024)",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector(4096)");

            migrationBuilder.AlterColumn<double>(
                name: "StreakCount",
                table: "student_learning_objectives",
                type: "double precision",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 1.0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.CreateTable(
                name: "task_submission_evaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OverallSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RawModelResponseJson = table.Column<string>(type: "text", nullable: false),
                    ValidationCount = table.Column<int>(type: "integer", nullable: false),
                    PassedValidationCount = table.Column<int>(type: "integer", nullable: false),
                    FailedValidationCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_submission_evaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_submission_evaluations_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_submission_evaluations_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_submission_evaluation_validations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskSubmissionEvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidationId = table.Column<int>(type: "integer", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    ObjectiveId = table.Column<int>(type: "integer", nullable: false),
                    ValidationString = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsPass = table.Column<bool>(type: "boolean", nullable: false),
                    WhyNotPass = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_submission_evaluation_validations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_submission_evaluation_validations_task_submission_eval~",
                        column: x => x.TaskSubmissionEvaluationId,
                        principalTable: "task_submission_evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_task_submission_evaluation_validations_TaskSubmissionEvalua~",
                table: "task_submission_evaluation_validations",
                columns: new[] { "TaskSubmissionEvaluationId", "ValidationId" });

            migrationBuilder.CreateIndex(
                name: "IX_task_submission_evaluations_StudentId_TaskId_CreatedAt",
                table: "task_submission_evaluations",
                columns: new[] { "StudentId", "TaskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_task_submission_evaluations_TaskId",
                table: "task_submission_evaluations",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_submission_evaluation_validations");

            migrationBuilder.DropTable(
                name: "task_submission_evaluations");

            migrationBuilder.AlterColumn<Vector>(
                name: "SearchVector",
                table: "tasks",
                type: "vector(4096)",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector(1024)");

            migrationBuilder.AlterColumn<int>(
                name: "StreakCount",
                table: "student_learning_objectives",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 1.0);
        }
    }
}
