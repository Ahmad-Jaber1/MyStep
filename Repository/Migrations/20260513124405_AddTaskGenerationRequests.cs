using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskGenerationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "task_generation_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupervisorId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<int>(type: "integer", nullable: false),
                    MainSkillId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_generation_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_generation_requests_paths_PathId",
                        column: x => x.PathId,
                        principalTable: "paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_generation_requests_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_generation_requests_supervisors_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "supervisors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_task_generation_requests_PathId",
                table: "task_generation_requests",
                column: "PathId");

            migrationBuilder.CreateIndex(
                name: "IX_task_generation_requests_StudentId",
                table: "task_generation_requests",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_task_generation_requests_SupervisorId",
                table: "task_generation_requests",
                column: "SupervisorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_generation_requests");
        }
    }
}
