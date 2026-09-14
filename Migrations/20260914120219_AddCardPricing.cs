using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingCardsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCardPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFoil",
                table: "Cards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MarketPrice",
                table: "Cards",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFoil",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "MarketPrice",
                table: "Cards");
        }
    }
}
