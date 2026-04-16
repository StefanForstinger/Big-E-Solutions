using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectPlanner.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAssigneeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fremdschlüssel entfernen
            migrationBuilder.DropForeignKey(
                name: "FK_TASKS_ASPNETUSERS_AssigneeId",
                schema: "ADMIN",
                table: "TASKS");

            // Index entfernen
            migrationBuilder.DropIndex(
                name: "IX_TASKS_AssigneeId",
                schema: "ADMIN",
                table: "TASKS");

            // Spalte entfernen
            migrationBuilder.DropColumn(
                name: "AssigneeId",
                schema: "ADMIN",
                table: "TASKS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Spalte wiederherstellen
            migrationBuilder.AddColumn<string>(
                name: "AssigneeId",
                schema: "ADMIN",
                table: "TASKS",
                type: "NVARCHAR2(450)",
                nullable: true);

            // Index wiederherstellen
            migrationBuilder.CreateIndex(
                name: "IX_TASKS_AssigneeId",
                schema: "ADMIN",
                table: "TASKS",
                column: "AssigneeId");

            // Fremdschlüssel wiederherstellen
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
    }
}
