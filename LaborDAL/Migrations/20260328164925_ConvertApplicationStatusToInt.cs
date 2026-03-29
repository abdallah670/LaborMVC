using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborDAL.Migrations
{
    /// <inheritdoc />
    public partial class ConvertApplicationStatusToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert ApplicationStatus from string to int
            migrationBuilder.Sql("UPDATE TaskApplications SET Status = '1' WHERE Status = 'Pending'");
            migrationBuilder.Sql("UPDATE TaskApplications SET Status = '2' WHERE Status = 'Viewed'");
            migrationBuilder.Sql("UPDATE TaskApplications SET Status = '3' WHERE Status = 'Accepted'");
            migrationBuilder.Sql("UPDATE TaskApplications SET Status = '4' WHERE Status = 'Rejected'");
            migrationBuilder.Sql("UPDATE TaskApplications SET Status = '5' WHERE Status = 'Withdrawn'");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "TaskApplications",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            // Add new user penalty tracking columns
            migrationBuilder.AddColumn<int>(
                name: "ActivePenaltyCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CancellationCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasUnacknowledgedPenalties",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAcceptanceRestricted",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPostingRestricted",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastStrikeDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NoShowCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecentCancellationCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RestrictionEndDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RestrictionReason",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StrikeCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspensionEndDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserPenalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RelatedTaskId = table.Column<int>(type: "int", nullable: true),
                    RatingDecreaseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PreviousRating = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewRating = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_UserPenalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPenalties_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CancellationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    CancelledByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientRefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WorkerPaymentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlatformFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinancialSettlementComplete = table.Column<bool>(type: "bit", nullable: false),
                    FinancialSettlementCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PenaltyApplied = table.Column<bool>(type: "bit", nullable: false),
                    PenaltyTier = table.Column<int>(type: "int", nullable: true),
                    PenaltyId = table.Column<int>(type: "int", nullable: true),
                    TimeBeforeStart = table.Column<TimeSpan>(type: "time", nullable: true),
                    WorkerHadCheckedIn = table.Column<bool>(type: "bit", nullable: false),
                    ClientHadConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    OutcomeDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                    table.PrimaryKey("PK_CancellationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CancellationRecords_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CancellationRecords_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CancellationRecords_UserPenalties_PenaltyId",
                        column: x => x.PenaltyId,
                        principalTable: "UserPenalties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRecords_AppUserId",
                table: "CancellationRecords",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRecords_PenaltyId",
                table: "CancellationRecords",
                column: "PenaltyId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRecords_TaskId",
                table: "CancellationRecords",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPenalties_UserId",
                table: "UserPenalties",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert ApplicationStatus from int back to string
            migrationBuilder.Sql("UPDATE TaskApplications SET Status = 'Pending' WHERE Status = '1'");
            migrationBuilder.Sql("UPDATE TaskApplications SET Status = 'Viewed' WHERE Status = '2'");
            migrationBuilder.Sql("UPDATE TaskApplications SET Status = 'Accepted' WHERE Status = '3'");
            migrationBuilder.Sql("UPDATE TaskApplications SET Status = 'Rejected' WHERE Status = '4'");
            migrationBuilder.Sql("UPDATE TaskApplications SET Status = 'Withdrawn' WHERE Status = '5'");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TaskApplications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.DropTable(
                name: "CancellationRecords");

            migrationBuilder.DropTable(
                name: "UserPenalties");

            migrationBuilder.DropColumn(
                name: "ActivePenaltyCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CancellationCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HasUnacknowledgedPenalties",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsAcceptanceRestricted",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsPostingRestricted",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastStrikeDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NoShowCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RecentCancellationCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RestrictionEndDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RestrictionReason",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StrikeCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SuspensionEndDate",
                table: "AspNetUsers");
        }
    }
}
