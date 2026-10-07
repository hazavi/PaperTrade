using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PaperTrade.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaperTradeDbContext))]
[Migration("20261007120000_RiskAnalytics")]
public sealed class RiskAnalytics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>("max_daily_loss_percent", "portfolios", type: "numeric(5,2)", nullable: true);
        migrationBuilder.AddColumn<decimal>("max_position_concentration_percent", "portfolios", type: "numeric(5,2)", nullable: true);
        migrationBuilder.AddCheckConstraint("ck_portfolios_max_daily_loss_percent", "portfolios",
            "max_daily_loss_percent IS NULL OR (max_daily_loss_percent > 0 AND max_daily_loss_percent <= 100)");
        migrationBuilder.AddCheckConstraint("ck_portfolios_max_concentration_percent", "portfolios",
            "max_position_concentration_percent IS NULL OR (max_position_concentration_percent > 0 AND max_position_concentration_percent <= 100)");
        migrationBuilder.CreateTable("equity_snapshots", table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
            recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            equity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
            cash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
            realized_pnl = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
            unrealized_pnl = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_equity_snapshots", x => x.id);
            table.ForeignKey("fk_equity_snapshots_portfolios_portfolio_id", x => x.portfolio_id,
                "portfolios", "id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("ix_equity_snapshots_portfolio_id_recorded_at", "equity_snapshots",
            new[] { "portfolio_id", "recorded_at" });
        migrationBuilder.CreateTable("journal_entries", table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
            order_id = table.Column<Guid>(type: "uuid", nullable: false),
            note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_journal_entries", x => x.id);
            table.ForeignKey("fk_journal_entries_orders_order_id", x => x.order_id,
                "orders", "id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("fk_journal_entries_portfolios_portfolio_id", x => x.portfolio_id,
                "portfolios", "id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("ux_journal_entries_order_id", "journal_entries", "order_id", unique: true);
        migrationBuilder.CreateIndex("ix_journal_entries_portfolio_id", "journal_entries", "portfolio_id");
        migrationBuilder.Sql("""
            INSERT INTO equity_snapshots (id, portfolio_id, recorded_at, equity, cash, realized_pnl, unrealized_pnl)
            SELECT gen_random_uuid(), id, created_at, initial_balance, initial_balance, 0, 0 FROM portfolios;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("journal_entries");
        migrationBuilder.DropTable("equity_snapshots");
        migrationBuilder.DropCheckConstraint("ck_portfolios_max_daily_loss_percent", "portfolios");
        migrationBuilder.DropCheckConstraint("ck_portfolios_max_concentration_percent", "portfolios");
        migrationBuilder.DropColumn("max_daily_loss_percent", "portfolios");
        migrationBuilder.DropColumn("max_position_concentration_percent", "portfolios");
    }
}
