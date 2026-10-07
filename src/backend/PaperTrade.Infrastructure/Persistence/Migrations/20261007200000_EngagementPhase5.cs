using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PaperTrade.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaperTradeDbContext))]
[Migration("20261007200000_EngagementPhase5")]
public sealed class EngagementPhase5 : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.DropCheckConstraint("ck_price_alerts_target_price_positive", "price_alerts");
        m.AddColumn<string>("metric", "price_alerts", type: "character varying(24)", maxLength: 24,
            nullable: false, defaultValue: "price");
        m.AddColumn<bool>("email_alerts_enabled", "users", type: "boolean", nullable: false, defaultValue: false);
        m.CreateTable("competitions", table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            owner_id = table.Column<Guid>(type: "uuid", nullable: false),
            name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            join_code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
            starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_competitions", x => x.id);
            table.ForeignKey("FK_competitions_users_owner_id", x => x.owner_id, "users", "id", onDelete: ReferentialAction.Cascade);
        });
        m.CreateIndex("IX_competitions_owner_id", "competitions", "owner_id");
        m.CreateIndex("IX_competitions_join_code", "competitions", "join_code", unique: true);
        m.CreateTable("competition_members", table => new
        {
            competition_id = table.Column<Guid>(type: "uuid", nullable: false),
            user_id = table.Column<Guid>(type: "uuid", nullable: false),
            joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            starting_equity = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
            ending_equity = table.Column<decimal>(type: "numeric(18,2)", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_competition_members", x => new { x.competition_id, x.user_id });
            table.ForeignKey("FK_competition_members_competitions_competition_id", x => x.competition_id,
                "competitions", "id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_competition_members_users_user_id", x => x.user_id,
                "users", "id", onDelete: ReferentialAction.Cascade);
        });
        m.CreateIndex("IX_competition_members_user_id", "competition_members", "user_id");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropTable("competition_members"); m.DropTable("competitions");
        m.DropColumn("metric", "price_alerts"); m.DropColumn("email_alerts_enabled", "users");
        m.AddCheckConstraint("ck_price_alerts_target_price_positive", "price_alerts", "target_price > 0");
    }
}
