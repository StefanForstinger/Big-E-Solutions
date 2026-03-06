using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectPlanner.Migrations
{
    public partial class AddProjectMembers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PROJECT_MEMBERS",
                schema: "ADMIN",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    UserId    = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROJECT_MEMBERS", x => new { x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_PROJECT_MEMBERS_PROJECTS_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "ADMIN",
                        principalTable: "PROJECTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PROJECT_MEMBERS_ASPNETUSERS_UserId",
                        column: x => x.UserId,
                        principalSchema: "ADMIN",
                        principalTable: "ASPNETUSERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_MEMBERS_UserId",
                schema: "ADMIN",
                table: "PROJECT_MEMBERS",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PROJECT_MEMBERS", schema: "ADMIN");
        }
    }
}
