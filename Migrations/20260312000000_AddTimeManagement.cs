using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectPlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── AppUser: neue Spalten ────────────────────────────────────────
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                schema: "ADMIN",
                table: "ASPNETUSERS",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrivacyAccepted",
                schema: "ADMIN",
                table: "ASPNETUSERS",
                nullable: false,
                defaultValue: false);

            // ── ProjectMember: neue Spalten ──────────────────────────────────
            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedAt",
                schema: "ADMIN",
                table: "PROJECT_MEMBERS",
                nullable: false,
                defaultValueSql: "SYSDATE");

            // ── Tasks: neue Spalten ──────────────────────────────────────────
            migrationBuilder.AddColumn<decimal>(
                name: "PlannedDuration",
                schema: "ADMIN",
                table: "TASKS",
                type: "NUMBER(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualDuration",
                schema: "ADMIN",
                table: "TASKS",
                type: "NUMBER(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkSharePercent",
                schema: "ADMIN",
                table: "TASKS",
                type: "NUMBER(5,2)",
                nullable: false,
                defaultValue: 0m);

            // ── PRIVACY_CONSENTS ─────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "PRIVACY_CONSENTS",
                schema: "ADMIN",
                columns: table => new
                {
                    Id        = table.Column<int>(nullable: false)
                                    .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    UserId    = table.Column<string>(maxLength: 450, nullable: false),
                    AcceptedAt = table.Column<DateTime>(nullable: false),
                    IpAddress = table.Column<string>(maxLength: 45, nullable: true),
                    Version   = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "1.0"),
                    Accepted  = table.Column<bool>(nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRIVACY_CONSENTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PRIVACY_CONSENTS_ASPNETUSERS",
                        column: x => x.UserId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETUSERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PRIVACY_CONSENTS_UserId",
                schema: "ADMIN",
                table: "PRIVACY_CONSENTS",
                column: "UserId");

            // ── WORK_SCHEDULES ───────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WORK_SCHEDULES",
                schema: "ADMIN",
                columns: table => new
                {
                    Id             = table.Column<int>(nullable: false)
                                         .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Name           = table.Column<string>(maxLength: 100, nullable: false),
                    ProjectId      = table.Column<int>(nullable: true),
                    WorkDaysMask   = table.Column<int>(nullable: false, defaultValue: 62),
                    DailyStartTime = table.Column<string>(maxLength: 5, nullable: false, defaultValue: "08:00"),
                    DailyEndTime   = table.Column<string>(maxLength: 5, nullable: false, defaultValue: "17:00"),
                    DailyHours     = table.Column<decimal>(type: "NUMBER(4,2)", nullable: false, defaultValue: 8m),
                    IsDefault      = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORK_SCHEDULES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WORK_SCHEDULES_PROJECTS",
                        column: x => x.ProjectId,
                        principalSchema: "ADMIN",
                        principalTable: "PROJECTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WORK_SCHEDULES_ProjectId",
                schema: "ADMIN",
                table: "WORK_SCHEDULES",
                column: "ProjectId");

            // ── TIME_ENTRIES ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "TIME_ENTRIES",
                schema: "ADMIN",
                columns: table => new
                {
                    Id            = table.Column<int>(nullable: false)
                                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    UserId        = table.Column<string>(maxLength: 450, nullable: false),
                    TaskId        = table.Column<int>(nullable: false),
                    ProjectId     = table.Column<int>(nullable: false),
                    StartTime     = table.Column<DateTime>(nullable: false),
                    EndTime       = table.Column<DateTime>(nullable: true),
                    DurationHours = table.Column<decimal>(type: "NUMBER(8,2)", nullable: true),
                    Description   = table.Column<string>(maxLength: 500, nullable: true),
                    CreatedAt     = table.Column<DateTime>(nullable: false),
                    IsManual      = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TIME_ENTRIES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TIME_ENTRIES_ASPNETUSERS",
                        column: x => x.UserId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETUSERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TIME_ENTRIES_TASKS",
                        column: x => x.TaskId,
                        principalSchema: "ADMIN",
                        principalTable: "TASKS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TIME_ENTRIES_PROJECTS",
                        column: x => x.ProjectId,
                        principalSchema: "ADMIN",
                        principalTable: "PROJECTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TIME_ENTRIES_UserId",
                schema: "ADMIN",
                table: "TIME_ENTRIES",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TIME_ENTRIES_TaskId",
                schema: "ADMIN",
                table: "TIME_ENTRIES",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TIME_ENTRIES_ProjectId",
                schema: "ADMIN",
                table: "TIME_ENTRIES",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TIME_ENTRIES",     schema: "ADMIN");
            migrationBuilder.DropTable(name: "WORK_SCHEDULES",   schema: "ADMIN");
            migrationBuilder.DropTable(name: "PRIVACY_CONSENTS", schema: "ADMIN");

            migrationBuilder.DropColumn(name: "WorkSharePercent", schema: "ADMIN", table: "TASKS");
            migrationBuilder.DropColumn(name: "ActualDuration",   schema: "ADMIN", table: "TASKS");
            migrationBuilder.DropColumn(name: "PlannedDuration",  schema: "ADMIN", table: "TASKS");
            migrationBuilder.DropColumn(name: "JoinedAt",              schema: "ADMIN", table: "PROJECT_MEMBERS");
            migrationBuilder.DropColumn(name: "PrivacyAccepted",       schema: "ADMIN", table: "ASPNETUSERS");
            migrationBuilder.DropColumn(name: "MustChangePassword",    schema: "ADMIN", table: "ASPNETUSERS");
        }
    }
}
