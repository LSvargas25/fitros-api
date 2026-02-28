using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToClientProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseSet_WorkoutSessions_WorkoutSessionId",
                table: "ExerciseSet");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkoutSessions",
                table: "WorkoutSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exercises",
                table: "Exercises");

            migrationBuilder.RenameTable(
                name: "WorkoutSessions",
                newName: "WorkoutSession");

            migrationBuilder.RenameTable(
                name: "Exercises",
                newName: "Exercise");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ClientProfiles",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkoutSession",
                table: "WorkoutSession",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exercise",
                table: "Exercise",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseSet_WorkoutSession_WorkoutSessionId",
                table: "ExerciseSet",
                column: "WorkoutSessionId",
                principalTable: "WorkoutSession",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseSet_WorkoutSession_WorkoutSessionId",
                table: "ExerciseSet");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkoutSession",
                table: "WorkoutSession");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exercise",
                table: "Exercise");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ClientProfiles");

            migrationBuilder.RenameTable(
                name: "WorkoutSession",
                newName: "WorkoutSessions");

            migrationBuilder.RenameTable(
                name: "Exercise",
                newName: "Exercises");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkoutSessions",
                table: "WorkoutSessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exercises",
                table: "Exercises",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseSet_WorkoutSessions_WorkoutSessionId",
                table: "ExerciseSet",
                column: "WorkoutSessionId",
                principalTable: "WorkoutSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
