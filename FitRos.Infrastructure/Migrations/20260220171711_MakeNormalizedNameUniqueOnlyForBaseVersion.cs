using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeNormalizedNameUniqueOnlyForBaseVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing unique index that blocks versioning
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_WorkoutRoutines_NormalizedName";
            """);

            // Create partial unique index: NormalizedName must be unique only for Version = 1
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_WorkoutRoutines_NormalizedName"
                ON "WorkoutRoutines" ("NormalizedName")
                WHERE "Version" = 1;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_WorkoutRoutines_NormalizedName";
            """);

            // Restore original behavior: NormalizedName unique for all rows
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_WorkoutRoutines_NormalizedName"
                ON "WorkoutRoutines" ("NormalizedName");
            """);
        }
    }
}