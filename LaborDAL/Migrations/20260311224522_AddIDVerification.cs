using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborDAL.Migrations
{
    /// <inheritdoc />
    public partial class AddIDVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPhoneVerificationAttempt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PhoneVerificationAttempts",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "IDVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocumentCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FrontDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BackDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SelfieUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaceMatchScore = table.Column<int>(type: "int", nullable: true),
                    FaceMatchPassed = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IDVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IDVerifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IDVerifications_UserId",
                table: "IDVerifications",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IDVerifications");

            migrationBuilder.DropColumn(
                name: "LastPhoneVerificationAttempt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneVerificationAttempts",
                table: "AspNetUsers");
        }
    }
}
