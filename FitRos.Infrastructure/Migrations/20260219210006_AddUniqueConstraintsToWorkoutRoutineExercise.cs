using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintsToWorkoutRoutineExercise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutRoutineExercises_WorkoutRoutineId",
                table: "WorkoutRoutineExercises");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutRoutineExercises_WorkoutRoutineId_ExerciseId",
                table: "WorkoutRoutineExercises",
                columns: new[] { "WorkoutRoutineId", "ExerciseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutRoutineExercises_WorkoutRoutineId_Order",
                table: "WorkoutRoutineExercises",
                columns: new[] { "WorkoutRoutineId", "Order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutRoutineExercises_WorkoutRoutineId_ExerciseId",
                table: "WorkoutRoutineExercises");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutRoutineExercises_WorkoutRoutineId_Order",
                table: "WorkoutRoutineExercises");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutRoutineExercises_WorkoutRoutineId",
                table: "WorkoutRoutineExercises",
                column: "WorkoutRoutineId");
        }
    }
}
