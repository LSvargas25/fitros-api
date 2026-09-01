using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActiveWeeklyTrainingPlanIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyTrainingPlans_ClientProfileId",
                table: "WeeklyTrainingPlans");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTrainingPlans_ClientProfileId_ActiveOnly",
                table: "WeeklyTrainingPlans",
                column: "ClientProfileId",
                unique: true,
                filter: "\"Status\" = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyTrainingPlans_ClientProfileId_ActiveOnly",
                table: "WeeklyTrainingPlans");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTrainingPlans_ClientProfileId",
                table: "WeeklyTrainingPlans",
                column: "ClientProfileId");
        }
    }
}
