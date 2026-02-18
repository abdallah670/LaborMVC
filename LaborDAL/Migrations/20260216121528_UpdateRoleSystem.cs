using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaborDAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoleSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, migrate data from ClientRole to a combined Role
            // Old UserType: Admin=1, Client=2
            // Old ClientRole: None=0, Worker=1, Poster=2, Both=3
            // New ClientRole: None=0, Worker=1, Poster=2, Admin=4, Both=3, AdminWorker=5, AdminPoster=6, AdminBoth=7
            
            // Add a temporary column for the new Role
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Migrate data: Combine UserType and ClientRole into new Role
            // If UserType was Admin (1), add Admin flag (4) to the ClientRole value
            // SQL: Role = ClientRole + (CASE WHEN UserType = 1 THEN 4 ELSE 0 END)
            migrationBuilder.Sql(
                "UPDATE AspNetUsers SET Role = ClientRole + CASE WHEN UserType = 1 THEN 4 ELSE 0 END");

            // Drop old columns
            migrationBuilder.DropColumn(
                name: "ClientRole",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back the old columns
            migrationBuilder.AddColumn<int>(
                name: "UserType",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 2); // Default to Client

            migrationBuilder.AddColumn<int>(
                name: "ClientRole",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Migrate data back: Extract UserType and ClientRole from Role
            // If Role has Admin flag (4), UserType = 1 (Admin), otherwise 2 (Client)
            // ClientRole = Role & 3 (mask out Admin flag)
            migrationBuilder.Sql(
                "UPDATE AspNetUsers SET UserType = CASE WHEN Role & 4 = 4 THEN 1 ELSE 2 END, ClientRole = Role & 3");

            // Drop the Role column
            migrationBuilder.DropColumn(
                name: "Role",
                table: "AspNetUsers");
        }
    }
}
