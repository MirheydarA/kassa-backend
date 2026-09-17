using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KassaApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMyDebtPaymentsAndRevert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RemainingAmount",
                table: "MyDebts",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "MyDebts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE [MyDebts] SET [RemainingAmount] = [Amount] WHERE [RemainingAmount] = 0");

            migrationBuilder.CreateTable(
                name: "MyDebtPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MyDebtId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyDebtPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MyDebtPayments_MyDebts_MyDebtId",
                        column: x => x.MyDebtId,
                        principalTable: "MyDebts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MyDebtPayments_MyDebtId",
                table: "MyDebtPayments",
                column: "MyDebtId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MyDebtPayments");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "MyDebts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MyDebts");
        }
    }
}
