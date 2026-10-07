using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PaperTrade.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaperTradeDbContext))]
[Migration("20261007180000_MarginSimulation")]
public sealed class MarginSimulation : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.DropCheckConstraint("ck_portfolios_cash_balance_nonnegative", "portfolios");
        m.AddColumn<bool>("margin_enabled", "portfolios", type: "boolean", nullable: false, defaultValue: false);
        foreach (var column in new[] { "equity_leverage", "forex_leverage", "metal_leverage",
            "commodity_leverage", "index_leverage", "crypto_leverage" })
            m.AddColumn<int>(column, "portfolios", type: "integer", nullable: false, defaultValue: 1);
        m.AddColumn<decimal>("margin_reserved", "positions", type: "numeric(18,2)",
            precision: 18, scale: 2, nullable: false, defaultValue: 0m);
        m.AddColumn<bool>("is_leveraged", "positions", type: "boolean", nullable: false, defaultValue: false);
        m.AddColumn<DateTimeOffset>("last_financed_at", "positions",
            type: "timestamp with time zone", nullable: false,
            defaultValue: DateTimeOffset.UnixEpoch);
        m.Sql("""
            UPDATE positions p SET margin_reserved = CASE
                WHEN i.asset_class = 'Forex' AND i.base_currency = 'USD' THEN p.quantity
                ELSE round(p.quantity * p.average_entry_price, 2) END,
                last_financed_at = p.created_at
            FROM instruments i WHERE p.instrument_id = i.id;
            """);
        m.AddCheckConstraint("ck_positions_margin_reserved_nonnegative", "positions", "margin_reserved >= 0");
        m.CreateTable("financing_charges", table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
            position_id = table.Column<Guid>(type: "uuid", nullable: false),
            symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            charged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_financing_charges", x => x.id);
            table.ForeignKey("fk_financing_charges_portfolios_portfolio_id", x => x.portfolio_id,
                "portfolios", "id", onDelete: ReferentialAction.Cascade);
        });
        m.CreateIndex("ix_financing_charges_portfolio_id", "financing_charges", "portfolio_id");
        m.CreateIndex("ux_financing_charges_position_date", "financing_charges",
            new[] { "position_id", "charged_at" }, unique: true);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropTable("financing_charges");
        m.DropCheckConstraint("ck_positions_margin_reserved_nonnegative", "positions");
        m.DropColumn("margin_reserved", "positions");
        m.DropColumn("is_leveraged", "positions");
        m.DropColumn("last_financed_at", "positions");
        foreach (var column in new[] { "equity_leverage", "forex_leverage", "metal_leverage",
            "commodity_leverage", "index_leverage", "crypto_leverage", "margin_enabled" })
            m.DropColumn(column, "portfolios");
        m.AddCheckConstraint("ck_portfolios_cash_balance_nonnegative", "portfolios", "cash_balance >= 0");
    }
}
