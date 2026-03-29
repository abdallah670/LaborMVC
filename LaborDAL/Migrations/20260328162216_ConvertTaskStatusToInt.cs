using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborDAL.Migrations
{
    /// <inheritdoc />
    public partial class ConvertTaskStatusToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First convert string status values to int values
            migrationBuilder.Sql("UPDATE Tasks SET Status = '0' WHERE Status = 'Created'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = '1' WHERE Status = 'Open'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = '2' WHERE Status = 'Assigned'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = '3' WHERE Status = 'Scheduled'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = '4' WHERE Status = 'InProgress'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = '5' WHERE Status = 'Completed'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = '6' WHERE Status = 'Cancelled'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = '7' WHERE Status = 'NoShow'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = '8' WHERE Status = 'Expired'");

            // Now alter the column type
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Tasks",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert int values back to string
            migrationBuilder.Sql("UPDATE Tasks SET Status = 'Created' WHERE Status = '0'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = 'Open' WHERE Status = '1'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = 'Assigned' WHERE Status = '2'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = 'Scheduled' WHERE Status = '3'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = 'InProgress' WHERE Status = '4'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = 'Completed' WHERE Status = '5'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = 'Cancelled' WHERE Status = '6'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = 'NoShow' WHERE Status = '7'");
            migrationBuilder.Sql("UPDATE Tasks SET Status = 'Expired' WHERE Status = '8'");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Tasks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
