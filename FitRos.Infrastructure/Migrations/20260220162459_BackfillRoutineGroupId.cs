using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillRoutineGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //  Fix existing rows that were created with Guid.Empty
            migrationBuilder.Sql("""
                UPDATE "WorkoutRoutines"
                SET "RoutineGroupId" = "Id"
                WHERE "RoutineGroupId" = '00000000-0000-0000-0000-000000000000';
            """);

            //  Remove default Guid.Empty to prevent future incorrect inserts
            migrationBuilder.Sql("""
                ALTER TABLE "WorkoutRoutines"
                ALTER COLUMN "RoutineGroupId" DROP DEFAULT;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Optional: restore default if rollback happens
            migrationBuilder.Sql("""
                ALTER TABLE "WorkoutRoutines"
                ALTER COLUMN "RoutineGroupId"
                SET DEFAULT '00000000-0000-0000-0000-000000000000';
            """);
        }
    }
}