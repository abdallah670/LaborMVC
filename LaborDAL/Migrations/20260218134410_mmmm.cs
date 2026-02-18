using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborDAL.Migrations
{
    /// <inheritdoc />
    public partial class mmmm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TaskId",
                table: "Bookings",
                newName: "TaskItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Bookings_TaskId",
                table: "Bookings",
                newName: "IX_Bookings_TaskItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Tasks_TaskItemId",
                table: "Bookings",
                column: "TaskItemId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Tasks_TaskItemId",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "TaskItemId",
                table: "Bookings",
                newName: "TaskId");

            migrationBuilder.RenameIndex(
                name: "IX_Bookings_TaskItemId",
                table: "Bookings",
                newName: "IX_Bookings_TaskId");
        }
    }
}
