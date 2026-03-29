using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborDAL.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBookingStatusToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, convert string status values to int values
            migrationBuilder.Sql("UPDATE Bookings SET Status = 0 WHERE Status = 'Scheduled'");
            migrationBuilder.Sql("UPDATE Bookings SET Status = 1 WHERE Status = 'InProgress'");
            migrationBuilder.Sql("UPDATE Bookings SET Status = 2 WHERE Status = 'CompletedfromWorker'");
            migrationBuilder.Sql("UPDATE Bookings SET Status = 3 WHERE Status = 'Completed'");
            migrationBuilder.Sql("UPDATE Bookings SET Status = 4 WHERE Status = 'Cancelled'");
            migrationBuilder.Sql("UPDATE Bookings SET Status = 5 WHERE Status = 'Disputed'");

            // Now alter the column type
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Bookings",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
