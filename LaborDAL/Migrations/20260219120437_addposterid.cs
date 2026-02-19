using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborDAL.Migrations
{
    /// <inheritdoc />
    public partial class addposterid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PosterId",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PosterId",
                table: "Bookings",
                column: "PosterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_PosterId",
                table: "Bookings",
                column: "PosterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_PosterId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_PosterId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PosterId",
                table: "Bookings");
        }
    }
}
