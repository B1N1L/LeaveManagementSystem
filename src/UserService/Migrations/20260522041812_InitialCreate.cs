using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using BCrypt.Net;
#nullable disable

namespace UserService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    ManagerId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            // Seed initial users with hashed passwords
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("NAGP2026");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "FullName", "Email", "PasswordHash", "Role", "ManagerId", "CreatedAt" },
                values: new object[,]
                {
                    { 1, "Manas", "manas@company.com", hashedPassword, "Manager", null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "Binil", "binil@company.com", hashedPassword, "Employee", 1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "Rohit", "rohit@company.com", hashedPassword, "Employee", 1, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValues: new object[] { 1, 2, 3 }
            );
        }
    }
}
