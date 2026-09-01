using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    public partial class AddGymIdToTenantEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GymId",
                table: "WorkoutSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GymId",
                table: "WorkoutRoutines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GymId",
                table: "Exercises",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GymId",
                table: "ClientProgressReportSnapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GymId",
                table: "ClientProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GymId",
                table: "ClientKpiSnapshots",
                type: "uuid",
                nullable: true);

            // =========================
            // Indexes for multi-tenant performance
            // =========================

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_GymId",
                table: "WorkoutSessions",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutRoutines_GymId",
                table: "WorkoutRoutines",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_GymId",
                table: "Exercises",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientProgressReportSnapshots_GymId",
                table: "ClientProgressReportSnapshots",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientProfiles_GymId",
                table: "ClientProfiles",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientKpiSnapshots_GymId",
                table: "ClientKpiSnapshots",
                column: "GymId");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 4, 19, 49, 23, 596, DateTimeKind.Utc).AddTicks(576));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutSessions_GymId",
                table: "WorkoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutRoutines_GymId",
                table: "WorkoutRoutines");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_GymId",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_ClientProgressReportSnapshots_GymId",
                table: "ClientProgressReportSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_ClientProfiles_GymId",
                table: "ClientProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ClientKpiSnapshots_GymId",
                table: "ClientKpiSnapshots");

            migrationBuilder.DropColumn(
                name: "GymId",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "GymId",
                table: "WorkoutRoutines");

            migrationBuilder.DropColumn(
                name: "GymId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "GymId",
                table: "ClientProgressReportSnapshots");

            migrationBuilder.DropColumn(
                name: "GymId",
                table: "ClientProfiles");

            migrationBuilder.DropColumn(
                name: "GymId",
                table: "ClientKpiSnapshots");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 4, 3, 37, 31, 52, DateTimeKind.Utc).AddTicks(2848));
        }
    }
}