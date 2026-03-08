using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateownerpasswordhash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 3, 0, 55, 18, 908, DateTimeKind.Utc).AddTicks(920), "AQAAAAIAAYagAAAAELe0J5jOZGpfuPmwQO01ca1V7Q7UBF6m/sJkq/Z8GrxFQYv6RxjKnlqfGpwRwMjWAQ==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 3, 0, 41, 7, 489, DateTimeKind.Utc).AddTicks(8980), "TEMP_HASH_REPLACE_ME" });
        }
    }
}
