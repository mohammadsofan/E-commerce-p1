using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueBaseCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure at most one base currency exists before applying the unique index.
            // Selection rule: Prioritize 'ILS' (the default system base currency) if it is currently marked as a base currency.
            // If 'ILS' is not among the base currencies, fall back to alphabetical ordering of the Code.
            // All other currencies marked as base will be demoted to non-base (IsBaseCurrency = 0).
            migrationBuilder.Sql(
                @"
                WITH RankedBases AS (
                    SELECT Id,
                           ROW_NUMBER() OVER(ORDER BY CASE WHEN Code = 'ILS' THEN 0 ELSE 1 END, Code ASC) as rn
                    FROM Currencies
                    WHERE IsBaseCurrency = 1
                )
                UPDATE Currencies
                SET IsBaseCurrency = 0
                WHERE IsBaseCurrency = 1
                  AND Id NOT IN (SELECT Id FROM RankedBases WHERE rn = 1);
                "
            );

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_IsBaseCurrency_Unique",
                table: "Currencies",
                column: "IsBaseCurrency",
                unique: true,
                filter: "[IsBaseCurrency] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Currencies_IsBaseCurrency_Unique",
                table: "Currencies");
        }
    }
}
