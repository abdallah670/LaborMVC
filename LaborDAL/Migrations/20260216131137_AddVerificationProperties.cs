using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborDAL.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationExpiry",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IDDocumentSubmittedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IDDocumentUrl",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneVerificationCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneVerificationExpiry",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationTier",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CreatedAt",
                table: "Bookings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TaskId",
                table: "Bookings",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_WorkerId",
                table: "Bookings",
                column: "WorkerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_CreatedAt",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TaskId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_WorkerId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EmailVerificationExpiry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IDDocumentSubmittedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IDDocumentUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationExpiry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VerificationTier",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
