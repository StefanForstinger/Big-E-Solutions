using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectPlanner.Migrations
{
    /// <inheritdoc />
    public partial class testen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ADMIN");

            migrationBuilder.CreateTable(
                name: "ASPNETROLES",
                schema: "ADMIN",
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETROLES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ASPNETUSERS",
                schema: "ADMIN",
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    FullName = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    UserName = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    PasswordHash = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETUSERS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ASPNETROLECLAIMS",
                schema: "ADMIN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    RoleId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ClaimValue = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETROLECLAIMS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ASPNETROLECLAIMS_ASPNETROLES_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETROLES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ASPNETUSERCLAIMS",
                schema: "ADMIN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ClaimValue = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETUSERCLAIMS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ASPNETUSERCLAIMS_ASPNETUSERS_UserId",
                        column: x => x.UserId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETUSERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ASPNETUSERLOGINS",
                schema: "ADMIN",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETUSERLOGINS", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_ASPNETUSERLOGINS_ASPNETUSERS_UserId",
                        column: x => x.UserId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETUSERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ASPNETUSERROLES",
                schema: "ADMIN",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    RoleId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETUSERROLES", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_ASPNETUSERROLES_ASPNETROLES_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETROLES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ASPNETUSERROLES_ASPNETUSERS_UserId",
                        column: x => x.UserId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETUSERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ASPNETUSERTOKENS",
                schema: "ADMIN",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Value = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETUSERTOKENS", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_ASPNETUSERTOKENS_ASPNETUSERS_UserId",
                        column: x => x.UserId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETUSERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PROJECTS",
                schema: "ADMIN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "NCLOB", maxLength: 4000, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Color = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    OwnerId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROJECTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PROJECTS_ASPNETUSERS_OwnerId",
                        column: x => x.OwnerId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETUSERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PROJECT_MEMBERS",
                schema: "ADMIN",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROJECT_MEMBERS", x => new { x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_PROJECT_MEMBERS_ASPNETUSERS_UserId",
                        column: x => x.UserId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETUSERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PROJECT_MEMBERS_PROJECTS_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "ADMIN",
                        principalTable: "PROJECTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TASKS",
                schema: "ADMIN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Title = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Progress = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ParentId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    Priority = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    IsMilestone = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    Note = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    AssigneeId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    ProjectId = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TASKS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TASKS_ASPNETUSERS_AssigneeId",
                        column: x => x.AssigneeId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETUSERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TASKS_PROJECTS_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "ADMIN",
                        principalTable: "PROJECTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TASK_COMMENTS",
                schema: "ADMIN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Text = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TaskId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    AuthorName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TASK_COMMENTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TASK_COMMENTS_ASPNETUSERS_UserId",
                        column: x => x.UserId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETUSERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TASK_COMMENTS_TASKS_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "ADMIN",
                        principalTable: "TASKS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TASK_LINKS",
                schema: "ADMIN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Source = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Target = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Type = table.Column<string>(type: "NVARCHAR2(5)", maxLength: 5, nullable: false),
                    ProjectId = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TASK_LINKS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TASK_LINKS_PROJECTS_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "ADMIN",
                        principalTable: "PROJECTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TASK_LINKS_TASKS_Source",
                        column: x => x.Source,
                        principalSchema: "ADMIN",
                        principalTable: "TASKS",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TASK_LINKS_TASKS_Target",
                        column: x => x.Target,
                        principalSchema: "ADMIN",
                        principalTable: "TASKS",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ASPNETROLECLAIMS_RoleId",
                schema: "ADMIN",
                table: "ASPNETROLECLAIMS",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "ADMIN",
                table: "ASPNETROLES",
                column: "NormalizedName",
                unique: true,
                filter: "\"NormalizedName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ASPNETUSERCLAIMS_UserId",
                schema: "ADMIN",
                table: "ASPNETUSERCLAIMS",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ASPNETUSERLOGINS_UserId",
                schema: "ADMIN",
                table: "ASPNETUSERLOGINS",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ASPNETUSERROLES_RoleId",
                schema: "ADMIN",
                table: "ASPNETUSERROLES",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "ADMIN",
                table: "ASPNETUSERS",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "ADMIN",
                table: "ASPNETUSERS",
                column: "NormalizedUserName",
                unique: true,
                filter: "\"NormalizedUserName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_MEMBERS_UserId",
                schema: "ADMIN",
                table: "PROJECT_MEMBERS",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECTS_OwnerId",
                schema: "ADMIN",
                table: "PROJECTS",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TASK_COMMENTS_TaskId",
                schema: "ADMIN",
                table: "TASK_COMMENTS",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TASK_COMMENTS_UserId",
                schema: "ADMIN",
                table: "TASK_COMMENTS",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TASK_LINKS_ProjectId",
                schema: "ADMIN",
                table: "TASK_LINKS",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TASK_LINKS_Source",
                schema: "ADMIN",
                table: "TASK_LINKS",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_TASK_LINKS_Target",
                schema: "ADMIN",
                table: "TASK_LINKS",
                column: "Target");

            migrationBuilder.CreateIndex(
                name: "IX_TASKS_AssigneeId",
                schema: "ADMIN",
                table: "TASKS",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_TASKS_ProjectId",
                schema: "ADMIN",
                table: "TASKS",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ASPNETROLECLAIMS",
                schema: "ADMIN");

            migrationBuilder.DropTable(
                name: "ASPNETUSERCLAIMS",
                schema: "ADMIN");

            migrationBuilder.DropTable(
                name: "ASPNETUSERLOGINS",
                schema: "ADMIN");

            migrationBuilder.DropTable(
                name: "ASPNETUSERROLES",
                schema: "ADMIN");

            migrationBuilder.DropTable(
                name: "ASPNETUSERTOKENS",
                schema: "ADMIN");

            migrationBuilder.DropTable(
                name: "PROJECT_MEMBERS",
                schema: "ADMIN");

            migrationBuilder.DropTable(
                name: "TASK_COMMENTS",
                schema: "ADMIN");

            migrationBuilder.DropTable(
                name: "TASK_LINKS",
                schema: "ADMIN");

            migrationBuilder.DropTable(
                name: "ASPNETROLES",
                schema: "ADMIN");

            migrationBuilder.DropTable(
                name: "TASKS",
                schema: "ADMIN");

            migrationBuilder.DropTable(
                name: "PROJECTS",
                schema: "ADMIN");

            migrationBuilder.DropTable(
                name: "ASPNETUSERS",
                schema: "ADMIN");
        }
    }
}
