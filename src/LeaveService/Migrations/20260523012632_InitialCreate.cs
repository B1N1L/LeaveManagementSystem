using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeaveService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    TotalSickLeaves = table.Column<int>(type: "integer", nullable: false),
                    TotalCasualLeaves = table.Column<int>(type: "integer", nullable: false),
                    TotalPrivilegeLeaves = table.Column<int>(type: "integer", nullable: false),
                    UsedSickLeaves = table.Column<int>(type: "integer", nullable: false),
                    UsedCasualLeaves = table.Column<int>(type: "integer", nullable: false),
                    UsedPrivilegeLeaves = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    ManagerId = table.Column<int>(type: "integer", nullable: false),
                    LeaveType = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalDays = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    AppliedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                });

            // Seed leave balances for existing employees (Binil=2, Rohit=3)
            migrationBuilder.InsertData(
                table: "LeaveBalances",
                columns: new[] { "Id", "EmployeeId", "Year", "TotalSickLeaves", "TotalCasualLeaves",
                     "TotalPrivilegeLeaves", "UsedSickLeaves", "UsedCasualLeaves", "UsedPrivilegeLeaves" },
                values: new object[,]
                {
                    { 1, 2, 2026, 10, 12, 15, 0, 0, 0 },
                    { 2, 3, 2026, 10, 12, 15, 0, 0, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId_Year",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveBalances");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

        }
    }
}
