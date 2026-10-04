using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KassaApi.Migrations
{
    /// <inheritdoc />
    public partial class FixExchangeDescriptionEncoding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Əvvəlki migration 'Mübadilə ' mətnini N prefiksi olmadan yazmışdı - SQL Server bunu
            // Unicode kimi tanımayıb "ə" hərfini "?" ilə əvəz etmişdi ("Mübadil?"). Bunu düzəldir.
            migrationBuilder.Sql(@"
                UPDATE t
                SET t.Description = N'Mübadilə ' +
                    CASE e.FromCurrency WHEN 0 THEN 'USD' WHEN 1 THEN 'RUB' END +
                    '->' +
                    CASE e.ToCurrency WHEN 0 THEN 'USD' WHEN 1 THEN 'RUB' END
                FROM CashBoxTransactions t
                INNER JOIN Exchanges e ON e.Id = t.ReferenceId
                WHERE t.Source = 4;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
