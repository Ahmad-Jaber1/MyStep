using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    public partial class AddTaskSubmissionEvaluationsAndFractionalStreak : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE student_learning_objectives ALTER COLUMN \"StreakCount\" TYPE double precision USING \"StreakCount\"::double precision;");
            migrationBuilder.Sql("ALTER TABLE student_learning_objectives ALTER COLUMN \"StreakCount\" SET DEFAULT 1;");

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
                        name: "FK_tse_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tse_tasks_TaskId",
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
                    WhyNotPass = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_submission_evaluation_validations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tsev_tse_TaskSubmissionEvaluationId",
                        column: x => x.TaskSubmissionEvaluationId,
                        principalTable: "task_submission_evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tse_Student_Task_CreatedAt",
                table: "task_submission_evaluations",
                columns: new[] { "StudentId", "TaskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tse_TaskId",
                table: "task_submission_evaluations",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_tse_StudentId",
                table: "task_submission_evaluations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_tsev_EvalId_ValidationId",
                table: "task_submission_evaluation_validations",
                columns: new[] { "TaskSubmissionEvaluationId", "ValidationId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_submission_evaluation_validations");

            migrationBuilder.DropTable(
                name: "task_submission_evaluations");

            migrationBuilder.Sql("ALTER TABLE student_learning_objectives ALTER COLUMN \"StreakCount\" TYPE integer USING ROUND(\"StreakCount\")::integer;");
            migrationBuilder.Sql("ALTER TABLE student_learning_objectives ALTER COLUMN \"StreakCount\" SET DEFAULT 1;");
        }
    }
}
