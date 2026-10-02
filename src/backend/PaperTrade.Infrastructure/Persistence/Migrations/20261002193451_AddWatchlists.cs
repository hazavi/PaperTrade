using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaperTrade.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchlists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "watchlists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_watchlists", x => x.id);
                    table.ForeignKey(
                        name: "fk_watchlists_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "watchlist_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    watchlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_watchlist_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_watchlist_items_watchlists_watchlist_id",
                        column: x => x.watchlist_id,
                        principalTable: "watchlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_watchlist_items_watchlist_id_symbol",
                table: "watchlist_items",
                columns: new[] { "watchlist_id", "symbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_watchlists_user_id_name",
                table: "watchlists",
                columns: new[] { "user_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "watchlist_items");

            migrationBuilder.DropTable(
                name: "watchlists");
        }
    }
}
