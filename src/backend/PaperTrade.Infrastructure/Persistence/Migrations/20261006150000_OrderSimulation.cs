using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PaperTrade.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaperTradeDbContext))]
[Migration("20261006150000_OrderSimulation")]
public sealed class OrderSimulation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("ux_trades_order_id", "trades");
        migrationBuilder.AlterColumn<string>("status", "orders", type: "character varying(20)",
            maxLength: 20, nullable: false, oldClrType: typeof(string),
            oldType: "character varying(12)", oldMaxLength: 12);
        migrationBuilder.AddColumn<decimal>("filled_quantity", "orders", type: "numeric(18,6)",
            precision: 18, scale: 6, nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<Guid>("parent_order_id", "orders", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("expires_at", "orders", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("closed_at", "orders", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<decimal>("fee", "trades", type: "numeric(18,2)",
            precision: 18, scale: 2, nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>("quote_price", "trades", type: "numeric(18,6)",
            precision: 18, scale: 6, nullable: false, defaultValue: 0m);
        migrationBuilder.Sql("""
            UPDATE orders SET filled_quantity = quantity, closed_at = executed_at
            WHERE status = 'Filled';
            UPDATE trades SET quote_price = price;
            """);
        migrationBuilder.CreateIndex("ix_trades_order_id", "trades", "order_id");
        migrationBuilder.CreateIndex("ix_orders_status_created_at", "orders", new[] { "status", "created_at" });
        migrationBuilder.CreateIndex("ix_orders_parent_order_id", "orders", "parent_order_id");
        migrationBuilder.AddForeignKey("fk_orders_orders_parent_order_id", "orders", "parent_order_id",
            "orders", principalColumn: "id", onDelete: ReferentialAction.Cascade);
        migrationBuilder.AddCheckConstraint("ck_orders_filled_quantity_range", "orders",
            "filled_quantity >= 0 AND filled_quantity <= quantity");
        migrationBuilder.AddCheckConstraint("ck_trades_fee_nonnegative", "trades", "fee >= 0");
        migrationBuilder.Sql("""
            CREATE FUNCTION papertrade_reject_execution_update() RETURNS trigger
            LANGUAGE plpgsql AS $$
            BEGIN
                IF TG_OP = 'DELETE' AND pg_trigger_depth() > 1 THEN
                    RETURN OLD;
                END IF;
                RAISE EXCEPTION 'Recorded executions cannot be changed';
            END;
            $$;
            CREATE TRIGGER trg_trades_immutable
            BEFORE UPDATE OR DELETE ON trades
            FOR EACH ROW EXECUTE FUNCTION papertrade_reject_execution_update();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER trg_trades_immutable ON trades;
            DROP FUNCTION papertrade_reject_execution_update();
            """);
        migrationBuilder.DropCheckConstraint("ck_trades_fee_nonnegative", "trades");
        migrationBuilder.DropCheckConstraint("ck_orders_filled_quantity_range", "orders");
        migrationBuilder.DropForeignKey("fk_orders_orders_parent_order_id", "orders");
        migrationBuilder.DropIndex("ix_orders_parent_order_id", "orders");
        migrationBuilder.DropIndex("ix_orders_status_created_at", "orders");
        migrationBuilder.DropIndex("ix_trades_order_id", "trades");
        migrationBuilder.DropColumn("filled_quantity", "orders");
        migrationBuilder.DropColumn("parent_order_id", "orders");
        migrationBuilder.DropColumn("expires_at", "orders");
        migrationBuilder.DropColumn("closed_at", "orders");
        migrationBuilder.DropColumn("fee", "trades");
        migrationBuilder.DropColumn("quote_price", "trades");
        migrationBuilder.AlterColumn<string>("status", "orders", type: "character varying(12)",
            maxLength: 12, nullable: false, oldClrType: typeof(string),
            oldType: "character varying(20)", oldMaxLength: 20);
        migrationBuilder.CreateIndex("ux_trades_order_id", "trades", "order_id", unique: true);
    }
}
