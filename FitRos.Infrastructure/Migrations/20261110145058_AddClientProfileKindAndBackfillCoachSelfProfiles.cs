using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientProfileKindAndBackfillCoachSelfProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 0 = GymClient (default for every existing row), 1 = CoachSelf.
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "ClientProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: every Coach (Role = 2) assigned to a gym that does not
            // already have a ClientProfile gets a personal one
            // (Kind = 1 / CoachSelf, CoachId = NULL, Status = 1 / Active).
            // Gym-less coaches are skipped - they get one when assigned to a gym
            // or re-created. Raw SQL, so no ClientProfileCreated domain event is
            // raised; that is intentional for a one-off data backfill.
            migrationBuilder.Sql(@"
                INSERT INTO ""ClientProfiles""
                    (""Id"", ""UserId"", ""CoachId"", ""Status"", ""Kind"", ""GymId"", ""CreatedAt"", ""CreatedBy"")
                SELECT
                    gen_random_uuid(),
                    u.""Id"",
                    NULL,
                    1,
                    1,
                    u.""GymId"",
                    now(),
                    u.""Id""
                FROM ""Users"" u
                WHERE u.""Role"" = 2
                  AND u.""GymId"" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM ""ClientProfiles"" cp WHERE cp.""UserId"" = u.""Id""
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the backfilled coach "My Area" profiles before the column goes.
            migrationBuilder.Sql(@"DELETE FROM ""ClientProfiles"" WHERE ""Kind"" = 1;");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "ClientProfiles");
        }
    }
}
