using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaperTrade.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InstrumentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_watchlist_items_watchlist_id_symbol",
                table: "watchlist_items");

            migrationBuilder.DropIndex(
                name: "ux_positions_portfolio_id_symbol",
                table: "positions");

            migrationBuilder.AddColumn<Guid>(
                name: "instrument_id",
                table: "watchlist_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "instrument_id",
                table: "trades",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "instrument_id",
                table: "price_alerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "instrument_id",
                table: "positions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "instrument_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "instruments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    asset_class = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    exchange = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    quote_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    price_precision = table.Column<int>(type: "integer", nullable: false),
                    quantity_precision = table.Column<int>(type: "integer", nullable: false),
                    tick_size = table.Column<decimal>(type: "numeric(18,10)", precision: 18, scale: 10, nullable: false),
                    minimum_order_size = table.Column<decimal>(type: "numeric(18,10)", precision: 18, scale: 10, nullable: false),
                    market_time_zone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    trading_session = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    is_tradable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instruments", x => x.id);
                    table.CheckConstraint("ck_instruments_minimum_order_size_positive", "minimum_order_size > 0");
                    table.CheckConstraint("ck_instruments_tick_size_positive", "tick_size > 0");
                });

            migrationBuilder.CreateTable(
                name: "provider_symbols",
                columns: table => new
                {
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    symbol = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provider_symbols", x => new { x.instrument_id, x.provider });
                    table.ForeignKey(
                        name: "fk_provider_symbols_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO instruments (id, symbol, display_name, asset_class, exchange,
                    base_currency, quote_currency, price_precision, quantity_precision,
                    tick_size, minimum_order_size, market_time_zone, trading_session, is_tradable)
                SELECT md5(symbol)::uuid, symbol, symbol, 'Equity', 'US', NULL, 'USD',
                    2, 6, 0.01, 0.000001, 'America/New_York', 'US equities', TRUE
                FROM (
                    SELECT upper(trim(symbol)) AS symbol FROM orders
                    UNION SELECT upper(trim(symbol)) FROM positions
                    UNION SELECT upper(trim(symbol)) FROM trades
                    UNION SELECT upper(trim(symbol)) FROM watchlist_items
                    UNION SELECT upper(trim(symbol)) FROM price_alerts
                ) AS existing_symbols;

                INSERT INTO provider_symbols (instrument_id, provider, symbol)
                SELECT id, 'finnhub', symbol FROM instruments
                UNION ALL
                SELECT id, 'twelvedata', symbol FROM instruments;

                UPDATE orders AS entity SET instrument_id = instrument.id
                    FROM instruments AS instrument WHERE upper(trim(entity.symbol)) = instrument.symbol;
                UPDATE positions AS entity SET instrument_id = instrument.id
                    FROM instruments AS instrument WHERE upper(trim(entity.symbol)) = instrument.symbol;
                UPDATE trades AS entity SET instrument_id = instrument.id
                    FROM instruments AS instrument WHERE upper(trim(entity.symbol)) = instrument.symbol;
                UPDATE watchlist_items AS entity SET instrument_id = instrument.id
                    FROM instruments AS instrument WHERE upper(trim(entity.symbol)) = instrument.symbol;
                UPDATE price_alerts AS entity SET instrument_id = instrument.id
                    FROM instruments AS instrument WHERE upper(trim(entity.symbol)) = instrument.symbol;
                """);

            foreach (var table in new[] { "orders", "positions", "trades", "watchlist_items", "price_alerts" })
            {
                migrationBuilder.AlterColumn<Guid>(
                    name: "instrument_id", table: table, type: "uuid", nullable: false,
                    oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            }

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_items_instrument_id",
                table: "watchlist_items",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ux_watchlist_items_watchlist_id_instrument_id",
                table: "watchlist_items",
                columns: new[] { "watchlist_id", "instrument_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trades_instrument_id",
                table: "trades",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_alerts_instrument_id",
                table: "price_alerts",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "IX_positions_instrument_id",
                table: "positions",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ux_positions_portfolio_id_instrument_id",
                table: "positions",
                columns: new[] { "portfolio_id", "instrument_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_instrument_id",
                table: "orders",
                column: "instrument_id");

            migrationBuilder.CreateIndex(
                name: "ux_instruments_symbol",
                table: "instruments",
                column: "symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_provider_symbols_provider_symbol",
                table: "provider_symbols",
                columns: new[] { "provider", "symbol" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_instruments_instrument_id",
                table: "orders",
                column: "instrument_id",
                principalTable: "instruments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_positions_instruments_instrument_id",
                table: "positions",
                column: "instrument_id",
                principalTable: "instruments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_alerts_instruments_instrument_id",
                table: "price_alerts",
                column: "instrument_id",
                principalTable: "instruments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_trades_instruments_instrument_id",
                table: "trades",
                column: "instrument_id",
                principalTable: "instruments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_watchlist_items_instruments_instrument_id",
                table: "watchlist_items",
                column: "instrument_id",
                principalTable: "instruments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_instruments_instrument_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "fk_positions_instruments_instrument_id",
                table: "positions");

            migrationBuilder.DropForeignKey(
                name: "fk_price_alerts_instruments_instrument_id",
                table: "price_alerts");

            migrationBuilder.DropForeignKey(
                name: "fk_trades_instruments_instrument_id",
                table: "trades");

            migrationBuilder.DropForeignKey(
                name: "fk_watchlist_items_instruments_instrument_id",
                table: "watchlist_items");

            migrationBuilder.DropTable(
                name: "provider_symbols");

            migrationBuilder.DropTable(
                name: "instruments");

            migrationBuilder.DropIndex(
                name: "IX_watchlist_items_instrument_id",
                table: "watchlist_items");

            migrationBuilder.DropIndex(
                name: "ux_watchlist_items_watchlist_id_instrument_id",
                table: "watchlist_items");

            migrationBuilder.DropIndex(
                name: "IX_trades_instrument_id",
                table: "trades");

            migrationBuilder.DropIndex(
                name: "IX_price_alerts_instrument_id",
                table: "price_alerts");

            migrationBuilder.DropIndex(
                name: "IX_positions_instrument_id",
                table: "positions");

            migrationBuilder.DropIndex(
                name: "ux_positions_portfolio_id_instrument_id",
                table: "positions");

            migrationBuilder.DropIndex(
                name: "IX_orders_instrument_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "instrument_id",
                table: "watchlist_items");

            migrationBuilder.DropColumn(
                name: "instrument_id",
                table: "trades");

            migrationBuilder.DropColumn(
                name: "instrument_id",
                table: "price_alerts");

            migrationBuilder.DropColumn(
                name: "instrument_id",
                table: "positions");

            migrationBuilder.DropColumn(
                name: "instrument_id",
                table: "orders");

            migrationBuilder.CreateIndex(
                name: "ux_watchlist_items_watchlist_id_symbol",
                table: "watchlist_items",
                columns: new[] { "watchlist_id", "symbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_positions_portfolio_id_symbol",
                table: "positions",
                columns: new[] { "portfolio_id", "symbol" },
                unique: true);
        }
    }
}
