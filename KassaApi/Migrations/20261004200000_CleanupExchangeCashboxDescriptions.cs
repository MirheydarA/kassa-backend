using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KassaApi.Migrations
{
    /// <inheritdoc />
    public partial class CleanupExchangeCashboxDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Köhnə Exchange qeydlərinin kassa təsvirini ("Mübadilə (RUB->USD): Naməlum müştəri" və
            // ya "Mübadilə redaktəsi (...): ad" kimi) Exchange cədvəlindəki valyuta cütündən yenidən
            // qurub müştəri adını silir - "Mübadilə RUB->USD" formasına gətirir.
            migrationBuilder.Sql(@"
                UPDATE t
                SET t.Description = 'Mübadilə ' +
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
            // Qeyri-bərpa olunan (orijinal mətn saxlanılmayıb) - geri qaytarmaq mümkün deyil.
        }
    }
}
