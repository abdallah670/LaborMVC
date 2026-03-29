using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborDAL.Migrations
{
    /// <inheritdoc />
    public partial class SeedDataUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rating_AspNetUsers_RateeId",
                table: "Rating");

            migrationBuilder.DropForeignKey(
                name: "FK_Rating_AspNetUsers_RaterId",
                table: "Rating");

            migrationBuilder.DropForeignKey(
                name: "FK_Rating_Bookings_bookingId",
                table: "Rating");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rating",
                table: "Rating");

            migrationBuilder.RenameTable(
                name: "Rating",
                newName: "Ratings");

            migrationBuilder.RenameIndex(
                name: "IX_Rating_RaterId_RateeId_bookingId",
                table: "Ratings",
                newName: "IX_Ratings_RaterId_RateeId_bookingId");

            migrationBuilder.RenameIndex(
                name: "IX_Rating_RateeId",
                table: "Ratings",
                newName: "IX_Ratings_RateeId");

            migrationBuilder.RenameIndex(
                name: "IX_Rating_bookingId",
                table: "Ratings",
                newName: "IX_Ratings_bookingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Ratings",
                table: "Ratings",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_AspNetUsers_RateeId",
                table: "Ratings",
                column: "RateeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_AspNetUsers_RaterId",
                table: "Ratings",
                column: "RaterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_Bookings_bookingId",
                table: "Ratings",
                column: "bookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_AspNetUsers_RateeId",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_AspNetUsers_RaterId",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Bookings_bookingId",
                table: "Ratings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Ratings",
                table: "Ratings");

            migrationBuilder.RenameTable(
                name: "Ratings",
                newName: "Rating");

            migrationBuilder.RenameIndex(
                name: "IX_Ratings_RaterId_RateeId_bookingId",
                table: "Rating",
                newName: "IX_Rating_RaterId_RateeId_bookingId");

            migrationBuilder.RenameIndex(
                name: "IX_Ratings_RateeId",
                table: "Rating",
                newName: "IX_Rating_RateeId");

            migrationBuilder.RenameIndex(
                name: "IX_Ratings_bookingId",
                table: "Rating",
                newName: "IX_Rating_bookingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rating",
                table: "Rating",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rating_AspNetUsers_RateeId",
                table: "Rating",
                column: "RateeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rating_AspNetUsers_RaterId",
                table: "Rating",
                column: "RaterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rating_Bookings_bookingId",
                table: "Rating",
                column: "bookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
