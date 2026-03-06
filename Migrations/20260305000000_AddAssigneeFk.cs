using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectPlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddAssigneeFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AssigneeId war NVARCHAR2(2000) → auf NVARCHAR2(450) kürzen (Identity-Key-Länge)
            migrationBuilder.AlterColumn<string>(
                name: "AssigneeId",
                schema: "ADMIN",
                table: "TASKS",
                type: "NVARCHAR2(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TASKS_AssigneeId",
                schema: "ADMIN",
                table: "TASKS",
                column: "AssigneeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TASKS_ASPNETUSERS_AssigneeId",
                schema: "ADMIN",
                table: "TASKS",
                column: "AssigneeId",
                principalSchema: "ADMIN",
                principalTable: "ASPNETUSERS",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TASKS_ASPNETUSERS_AssigneeId",
                schema: "ADMIN",
                table: "TASKS");

            migrationBuilder.DropIndex(
                name: "IX_TASKS_AssigneeId",
                schema: "ADMIN",
                table: "TASKS");

            migrationBuilder.AlterColumn<string>(
                name: "AssigneeId",
                schema: "ADMIN",
                table: "TASKS",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)",
                oldNullable: true);
        }
    }
}
