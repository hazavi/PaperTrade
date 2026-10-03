using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaperTrade.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTradingEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "realized_pnl",
                table: "portfolios",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    side = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    requested_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    executed_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    executed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.CheckConstraint("ck_orders_executed_price_positive", "executed_price IS NULL OR executed_price > 0");
                    table.CheckConstraint("ck_orders_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_orders_requested_price_positive", "requested_price > 0");
                    table.ForeignKey(
                        name: "fk_orders_portfolios_portfolio_id",
                        column: x => x.portfolio_id,
                        principalTable: "portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    average_entry_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_positions", x => x.id);
                    table.CheckConstraint("ck_positions_average_entry_price_positive", "average_entry_price > 0");
                    table.CheckConstraint("ck_positions_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_positions_portfolios_portfolio_id",
                        column: x => x.portfolio_id,
                        principalTable: "portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    side = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    total_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    realized_pnl = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    executed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trades", x => x.id);
                    table.CheckConstraint("ck_trades_price_positive", "price > 0");
                    table.CheckConstraint("ck_trades_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_trades_total_value_positive", "total_value > 0");
                    table.ForeignKey(
                        name: "fk_trades_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_trades_portfolios_portfolio_id",
                        column: x => x.portfolio_id,
                        principalTable: "portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_portfolio_id_created_at",
                table: "orders",
                columns: new[] { "portfolio_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_positions_portfolio_id_symbol",
                table: "positions",
                columns: new[] { "portfolio_id", "symbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trades_portfolio_id_executed_at",
                table: "trades",
                columns: new[] { "portfolio_id", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "ux_trades_order_id",
                table: "trades",
                column: "order_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropTable(
                name: "trades");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropColumn(
                name: "realized_pnl",
                table: "portfolios");
        }
    }
}
