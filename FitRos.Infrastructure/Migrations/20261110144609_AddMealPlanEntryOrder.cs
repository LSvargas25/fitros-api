using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitRos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealPlanEntryOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "MealPlanEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: number existing entries 1..N within each (plan, day, meal)
            // slot, ordered by Id for a stable, deterministic result.
            migrationBuilder.Sql(
                """
                UPDATE "MealPlanEntries" AS e
                SET "Order" = s.rn
                FROM (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "MealPlanId", "Day", "Meal"
                               ORDER BY "Id"
                           ) AS rn
                    FROM "MealPlanEntries"
                ) AS s
                WHERE e."Id" = s."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanEntries_MealPlanId_Day_Meal_Order",
                table: "MealPlanEntries",
                columns: new[] { "MealPlanId", "Day", "Meal", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MealPlanEntries_MealPlanId_Day_Meal_Order",
                table: "MealPlanEntries");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "MealPlanEntries");
        }
    }
}
