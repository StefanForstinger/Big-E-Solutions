using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectPlanner.Migrations
{
    public partial class AddAllFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── PROJECTS: Color ──────────────────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "Color", schema: "ADMIN", table: "PROJECTS",
                type: "NVARCHAR2(20)", maxLength: 20, nullable: false, defaultValue: "#2D9CDB");

            // ── TASKS: neue Felder ───────────────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "Priority", schema: "ADMIN", table: "TASKS",
                type: "NVARCHAR2(10)", maxLength: 10, nullable: false, defaultValue: "Medium");

            migrationBuilder.AddColumn<string>(
                name: "Status", schema: "ADMIN", table: "TASKS",
                type: "NVARCHAR2(20)", maxLength: 20, nullable: false, defaultValue: "Open");

            migrationBuilder.AddColumn<bool>(
                name: "IsMilestone", schema: "ADMIN", table: "TASKS",
                type: "NUMBER(1)", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note", schema: "ADMIN", table: "TASKS",
                type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true);

            // AssigneeId Typ korrigieren (falls noch NVARCHAR2(2000))
            migrationBuilder.AlterColumn<string>(
                name: "AssigneeId", schema: "ADMIN", table: "TASKS",
                type: "NVARCHAR2(450)", nullable: true,
                oldClrType: typeof(string), oldType: "NVARCHAR2(2000)", oldNullable: true);

            // AssigneeId Index (falls noch nicht vorhanden)
            migrationBuilder.Sql(@"
                BEGIN
                  EXECUTE IMMEDIATE 'CREATE INDEX IX_TASKS_ASSIGNEEID ON ADMIN.TASKS (ASSIGNEEID)';
                EXCEPTION WHEN OTHERS THEN NULL;
                END;");

            // AssigneeId FK (falls noch nicht vorhanden)
            migrationBuilder.Sql(@"
                BEGIN
                  EXECUTE IMMEDIATE 'ALTER TABLE ADMIN.TASKS ADD CONSTRAINT FK_TASKS_ASPNETUSERS_ASSIGNEEID
                    FOREIGN KEY (ASSIGNEEID) REFERENCES ADMIN.ASPNETUSERS(ID) ON DELETE SET NULL';
                EXCEPTION WHEN OTHERS THEN NULL;
                END;");

            // ── TASK_LINKS ───────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "TASK_LINKS", schema: "ADMIN",
                columns: table => new
                {
                    Id        = table.Column<int>(type: "NUMBER(10)", nullable: false)
                                    .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Source    = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Target    = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Type      = table.Column<string>(type: "NVARCHAR2(5)", maxLength: 5, nullable: false, defaultValue: "0"),
                    ProjectId = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TASK_LINKS", x => x.Id);
                    table.ForeignKey("FK_TASK_LINKS_PROJECTS_ProjectId",
                        x => x.ProjectId, principalSchema: "ADMIN", principalTable: "PROJECTS",
                        principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_TASK_LINKS_TASKS_SOURCE",
                        x => x.Source, principalSchema: "ADMIN", principalTable: "TASKS",
                        principalColumn: "Id", onDelete: ReferentialAction.NoAction);
                    table.ForeignKey("FK_TASK_LINKS_TASKS_TARGET",
                        x => x.Target, principalSchema: "ADMIN", principalTable: "TASKS",
                        principalColumn: "Id", onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex("IX_TASK_LINKS_ProjectId", schema: "ADMIN", table: "TASK_LINKS", column: "ProjectId");
            migrationBuilder.CreateIndex("IX_TASK_LINKS_Source",    schema: "ADMIN", table: "TASK_LINKS", column: "Source");
            migrationBuilder.CreateIndex("IX_TASK_LINKS_Target",    schema: "ADMIN", table: "TASK_LINKS", column: "Target");

            // ── TASK_COMMENTS ────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "TASK_COMMENTS", schema: "ADMIN",
                columns: table => new
                {
                    Id        = table.Column<int>(type: "NUMBER(10)", nullable: false)
                                    .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Text      = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TaskId    = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    UserId    = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    AuthorName = table.Column<string>(type: "NVARCHAR2(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TASK_COMMENTS", x => x.Id);
                    table.ForeignKey("FK_TASK_COMMENTS_TASKS_TaskId",
                        x => x.TaskId, principalSchema: "ADMIN", principalTable: "TASKS",
                        principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_TASK_COMMENTS_ASPNETUSERS_UserId",
                        x => x.UserId, principalSchema: "ADMIN", principalTable: "ASPNETUSERS",
                        principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_TASK_COMMENTS_TaskId", schema: "ADMIN", table: "TASK_COMMENTS", column: "TaskId");
            migrationBuilder.CreateIndex("IX_TASK_COMMENTS_UserId", schema: "ADMIN", table: "TASK_COMMENTS", column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TASK_COMMENTS", schema: "ADMIN");
            migrationBuilder.DropTable(name: "TASK_LINKS",    schema: "ADMIN");
            migrationBuilder.DropColumn(name: "Color",       schema: "ADMIN", table: "PROJECTS");
            migrationBuilder.DropColumn(name: "Priority",    schema: "ADMIN", table: "TASKS");
            migrationBuilder.DropColumn(name: "Status",      schema: "ADMIN", table: "TASKS");
            migrationBuilder.DropColumn(name: "IsMilestone", schema: "ADMIN", table: "TASKS");
            migrationBuilder.DropColumn(name: "Note",        schema: "ADMIN", table: "TASKS");
        }
    }
}
