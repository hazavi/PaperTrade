using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PaperTrade.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaperTradeDbContext))]
[Migration("20261007190000_ChartLayouts")]
public sealed class ChartLayouts : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("chart_layouts", table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            user_id = table.Column<Guid>(type: "uuid", nullable: false),
            name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            timeframe = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            state_json = table.Column<string>(type: "character varying(32768)", maxLength: 32768, nullable: false),
            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_chart_layouts", x => x.id);
            table.ForeignKey("fk_chart_layouts_users_user_id", x => x.user_id,
                "users", "id", onDelete: ReferentialAction.Cascade);
        });
        m.CreateIndex("ix_chart_layouts_user_updated", "chart_layouts",
            new[] { "user_id", "updated_at" });
    }

    protected override void Down(MigrationBuilder m) => m.DropTable("chart_layouts");
}
