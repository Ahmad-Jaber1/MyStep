using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supervisors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    PathId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supervisors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supervisors_paths_PathId",
                        column: x => x.PathId,
                        principalTable: "paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supervisor_students",
                columns: table => new
                {
                    SupervisorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supervisor_students", x => new { x.SupervisorId, x.StudentId, x.PathId });
                    table.ForeignKey(
                        name: "FK_supervisor_students_paths_PathId",
                        column: x => x.PathId,
                        principalTable: "paths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_supervisor_students_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_supervisor_students_supervisors_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "supervisors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supervisor_students_PathId",
                table: "supervisor_students",
                column: "PathId");

            migrationBuilder.CreateIndex(
                name: "IX_supervisor_students_StudentId",
                table: "supervisor_students",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_supervisors_PathId",
                table: "supervisors",
                column: "PathId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supervisor_students");

            migrationBuilder.DropTable(
                name: "supervisors");
        }
    }
}
