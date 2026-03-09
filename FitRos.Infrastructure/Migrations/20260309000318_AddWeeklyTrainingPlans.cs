using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyTrainingPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeeklyTrainingPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GymId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyTrainingPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingPlanDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeeklyTrainingPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false),
                    WorkoutRoutineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPlanDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingPlanDays_WeeklyTrainingPlans_WeeklyTrainingPlanId",
                        column: x => x.WeeklyTrainingPlanId,
                        principalTable: "WeeklyTrainingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 9, 0, 3, 17, 785, DateTimeKind.Utc).AddTicks(3289));

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlanDays_WeeklyTrainingPlanId_Day",
                table: "TrainingPlanDays",
                columns: new[] { "WeeklyTrainingPlanId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTrainingPlans_ClientProfileId",
                table: "WeeklyTrainingPlans",
                column: "ClientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTrainingPlans_ClientProfileId_Status",
                table: "WeeklyTrainingPlans",
                columns: new[] { "ClientProfileId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingPlanDays");

            migrationBuilder.DropTable(
                name: "WeeklyTrainingPlans");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 8, 23, 35, 31, 42, DateTimeKind.Utc).AddTicks(7173));
        }
    }
}
