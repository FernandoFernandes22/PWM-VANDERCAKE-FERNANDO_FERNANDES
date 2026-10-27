using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VanderCake.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompoundSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailyDiscountPercent",
                table: "Receivables",
                type: "decimal(15,2)",
                precision: 15,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyInterestPercent",
                table: "Receivables",
                type: "decimal(15,2)",
                precision: 15,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Receipts",
                type: "decimal(15,2)",
                precision: 15,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Interest",
                table: "Receipts",
                type: "decimal(15,2)",
                precision: 15,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrincipalSettled",
                table: "Receipts",
                type: "decimal(15,2)",
                precision: 15,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyDiscountPercent",
                table: "PaymentTerms",
                type: "decimal(15,2)",
                precision: 15,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyInterestPercent",
                table: "PaymentTerms",
                type: "decimal(15,2)",
                precision: 15,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyDiscountPercent",
                table: "Orders",
                type: "decimal(15,2)",
                precision: 15,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyInterestPercent",
                table: "Orders",
                type: "decimal(15,2)",
                precision: 15,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
            // Recebimentos anteriores liquidavam somente principal; preserva as baixas existentes.
            migrationBuilder.Sql("UPDATE `Receipts` SET `PrincipalSettled` = `Amount`;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyDiscountPercent",
                table: "Receivables");

            migrationBuilder.DropColumn(
                name: "DailyInterestPercent",
                table: "Receivables");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "Interest",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "PrincipalSettled",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "DailyDiscountPercent",
                table: "PaymentTerms");

            migrationBuilder.DropColumn(
                name: "DailyInterestPercent",
                table: "PaymentTerms");

            migrationBuilder.DropColumn(
                name: "DailyDiscountPercent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DailyInterestPercent",
                table: "Orders");
        }
    }
}
