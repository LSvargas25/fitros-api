using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutineGroupIdToWorkoutRoutines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoutineGroupId",
                table: "WorkoutRoutines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutRoutines_RoutineGroupId_Version",
                table: "WorkoutRoutines",
                columns: new[] { "RoutineGroupId", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutRoutines_RoutineGroupId_Version",
                table: "WorkoutRoutines");

            migrationBuilder.DropColumn(
                name: "RoutineGroupId",
                table: "WorkoutRoutines");
        }
    }
}
