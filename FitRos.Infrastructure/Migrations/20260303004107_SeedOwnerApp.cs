using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedOwnerApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "WorkoutSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "WorkoutSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedBy",
                table: "WorkoutSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "ClientProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "ClientProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedBy",
                table: "ClientProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FirstName", "LastName", "NormalizedEmail", "PasswordHash", "PasswordResetTokenExpiresAtUtc", "PasswordResetTokenHash", "Role", "Status", "UpdatedAt" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 3, 3, 0, 41, 7, 489, DateTimeKind.Utc).AddTicks(8980), "owner@fitros.com", "FitRos", "Owner", "OWNER@FITROS.COM", "TEMP_HASH_REPLACE_ME", null, null, 0, 1, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ClientProfiles");
        }
    }
}
