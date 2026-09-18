using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TradingCardsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBundlesRemoveSellerStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerStocks");

            migrationBuilder.AddColumn<int>(
                name: "BundleCardId",
                table: "OrderItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bundles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bundles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bundles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BundleCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BundleId = table.Column<int>(type: "integer", nullable: false),
                    CardId = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BundleCards_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BundleCards_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_BundleCardId",
                table: "OrderItems",
                column: "BundleCardId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleCards_BundleId_CardId",
                table: "BundleCards",
                columns: new[] { "BundleId", "CardId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BundleCards_CardId",
                table: "BundleCards",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_UserId",
                table: "Bundles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_BundleCards_BundleCardId",
                table: "OrderItems",
                column: "BundleCardId",
                principalTable: "BundleCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_BundleCards_BundleCardId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "BundleCards");

            migrationBuilder.DropTable(
                name: "Bundles");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_BundleCardId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "BundleCardId",
                table: "OrderItems");

            migrationBuilder.CreateTable(
                name: "SellerStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CardId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UserPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerStocks_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SellerStocks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerStocks_CardId",
                table: "SellerStocks",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerStocks_UserId",
                table: "SellerStocks",
                column: "UserId");
        }
    }
}
