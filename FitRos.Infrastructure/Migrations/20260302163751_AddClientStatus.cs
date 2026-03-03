using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "ClientProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ClientProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ClientProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ClientProfiles_Status",
                table: "ClientProfiles",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClientProfiles_Status",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ClientProfiles");
        }
    }
}
