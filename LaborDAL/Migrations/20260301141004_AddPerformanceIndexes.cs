using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborDAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Message_bookingId",
                table: "Message");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_PosterId",
                table: "Bookings");

            migrationBuilder.RenameIndex(
                name: "IX_Message_SenderId",
                table: "Message",
                newName: "IX_Messages_SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_PosterId_Status",
                table: "Tasks",
                columns: new[] { "PosterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status_CreatedAt",
                table: "Tasks",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskApplications_CreatedAt",
                table: "TaskApplications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TaskApplications_TaskItemId_Status",
                table: "TaskApplications",
                columns: new[] { "TaskItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskApplications_WorkerId_Status",
                table: "TaskApplications",
                columns: new[] { "WorkerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_BookingId_SentAt",
                table: "Message",
                columns: new[] { "bookingId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_IsRead",
                table: "Message",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PosterId_Status",
                table: "Bookings",
                columns: new[] { "PosterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_WorkerId_Status",
                table: "Bookings",
                columns: new[] { "WorkerId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_PosterId_Status",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_Status_CreatedAt",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_TaskApplications_CreatedAt",
                table: "TaskApplications");

            migrationBuilder.DropIndex(
                name: "IX_TaskApplications_TaskItemId_Status",
                table: "TaskApplications");

            migrationBuilder.DropIndex(
                name: "IX_TaskApplications_WorkerId_Status",
                table: "TaskApplications");

            migrationBuilder.DropIndex(
                name: "IX_Messages_BookingId_SentAt",
                table: "Message");

            migrationBuilder.DropIndex(
                name: "IX_Messages_IsRead",
                table: "Message");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_PosterId_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_WorkerId_Status",
                table: "Bookings");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_SenderId",
                table: "Message",
                newName: "IX_Message_SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_bookingId",
                table: "Message",
                column: "bookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PosterId",
                table: "Bookings",
                column: "PosterId");
        }
    }
}
