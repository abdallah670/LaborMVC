using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborDAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Disputes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    RaisedBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Resolution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolutionType = table.Column<int>(type: "int", nullable: true),
                    WorkerPercentage = table.Column<int>(type: "int", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disputes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Disputes_AspNetUsers_RaisedBy",
                        column: x => x.RaisedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Disputes_AspNetUsers_ResolvedBy",
                        column: x => x.ResolvedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Disputes_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_BookingId",
                table: "Disputes",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_RaisedBy",
                table: "Disputes",
                column: "RaisedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_ResolvedBy",
                table: "Disputes",
                column: "ResolvedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Disputes");
        }
    }
}
