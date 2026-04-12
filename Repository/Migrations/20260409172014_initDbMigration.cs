using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class initDbMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "paths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paths", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PathId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_skills_paths_PathId",
                        column: x => x.PathId,
                        principalTable: "paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    SelectedPathId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_students_paths_SelectedPathId",
                        column: x => x.SelectedPathId,
                        principalTable: "paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "learning_objectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_objectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_learning_objectives_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<int>(type: "integer", nullable: false),
                    MainSkillId = table.Column<int>(type: "integer", nullable: false),
                    TaskData = table.Column<string>(type: "jsonb", nullable: false),
                    SearchVector = table.Column<Vector>(type: "vector(4096)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tasks_paths_PathId",
                        column: x => x.PathId,
                        principalTable: "paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tasks_skills_MainSkillId",
                        column: x => x.MainSkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_learning_objectives",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningObjectiveId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_learning_objectives", x => new { x.StudentId, x.LearningObjectiveId });
                    table.ForeignKey(
                        name: "FK_student_learning_objectives_learning_objectives_LearningObj~",
                        column: x => x.LearningObjectiveId,
                        principalTable: "learning_objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_student_learning_objectives_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_prerequisites",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningObjectiveId = table.Column<int>(type: "integer", nullable: false),
                    Justification = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_prerequisites", x => new { x.TaskId, x.LearningObjectiveId });
                    table.ForeignKey(
                        name: "FK_task_prerequisites_learning_objectives_LearningObjectiveId",
                        column: x => x.LearningObjectiveId,
                        principalTable: "learning_objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_prerequisites_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_targets",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningObjectiveId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_targets", x => new { x.TaskId, x.LearningObjectiveId });
                    table.ForeignKey(
                        name: "FK_task_targets_learning_objectives_LearningObjectiveId",
                        column: x => x.LearningObjectiveId,
                        principalTable: "learning_objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_targets_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_learning_objectives_SkillId",
                table: "learning_objectives",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_skills_PathId",
                table: "skills",
                column: "PathId");

            migrationBuilder.CreateIndex(
                name: "IX_student_learning_objectives_LearningObjectiveId",
                table: "student_learning_objectives",
                column: "LearningObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_students_SelectedPathId",
                table: "students",
                column: "SelectedPathId");

            migrationBuilder.CreateIndex(
                name: "IX_task_prerequisites_LearningObjectiveId",
                table: "task_prerequisites",
                column: "LearningObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_task_targets_LearningObjectiveId",
                table: "task_targets",
                column: "LearningObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_MainSkillId",
                table: "tasks",
                column: "MainSkillId");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_PathId",
                table: "tasks",
                column: "PathId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_learning_objectives");

            migrationBuilder.DropTable(
                name: "task_prerequisites");

            migrationBuilder.DropTable(
                name: "task_targets");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "learning_objectives");

            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "paths");
        }
    }
}
