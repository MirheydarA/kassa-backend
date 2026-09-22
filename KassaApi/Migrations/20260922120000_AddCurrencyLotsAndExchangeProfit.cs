using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KassaApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyLotsAndExchangeProfit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RealizedProfit",
                table: "Exchanges",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CurrencyLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SourceExchangeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyLots_Exchanges_SourceExchangeId",
                        column: x => x.SourceExchangeId,
                        principalTable: "Exchanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LotConsumptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotId = table.Column<int>(type: "int", nullable: false),
                    SellExchangeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LotConsumptions_CurrencyLots_LotId",
                        column: x => x.LotId,
                        principalTable: "CurrencyLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LotConsumptions_Exchanges_SellExchangeId",
                        column: x => x.SellExchangeId,
                        principalTable: "Exchanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyLots_SourceExchangeId",
                table: "CurrencyLots",
                column: "SourceExchangeId");

            migrationBuilder.CreateIndex(
                name: "IX_LotConsumptions_LotId",
                table: "LotConsumptions",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_LotConsumptions_SellExchangeId",
                table: "LotConsumptions",
                column: "SellExchangeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LotConsumptions");

            migrationBuilder.DropTable(
                name: "CurrencyLots");

            migrationBuilder.DropColumn(
                name: "RealizedProfit",
                table: "Exchanges");
        }
    }
}
