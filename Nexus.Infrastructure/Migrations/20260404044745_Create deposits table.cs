using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Createdepositstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deposits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    stripe_session_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    stripe_payment_intent_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    paid_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    raw_response = table.Column<string>(type: "text", nullable: false),
                    listing_id1 = table.Column<Guid>(type: "uuid", nullable: true),
                    property_id1 = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deposits", x => x.id);
                    table.ForeignKey(
                        name: "fk_deposits_listings_listing_id",
                        column: x => x.listing_id,
                        principalTable: "listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deposits_listings_listing_id1",
                        column: x => x.listing_id1,
                        principalTable: "listings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_deposits_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deposits_properties_property_id1",
                        column: x => x.property_id1,
                        principalTable: "properties",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_deposits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deposits_users_user_id1",
                        column: x => x.user_id1,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_deposits_idempotency_key",
                table: "deposits",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deposits_listing_id",
                table: "deposits",
                column: "listing_id");

            migrationBuilder.CreateIndex(
                name: "ix_deposits_listing_id1",
                table: "deposits",
                column: "listing_id1");

            migrationBuilder.CreateIndex(
                name: "ix_deposits_property_id",
                table: "deposits",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_deposits_property_id1",
                table: "deposits",
                column: "property_id1");

            migrationBuilder.CreateIndex(
                name: "ix_deposits_status",
                table: "deposits",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_deposits_stripe_session_id",
                table: "deposits",
                column: "stripe_session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deposits_user_id",
                table: "deposits",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_deposits_user_id1",
                table: "deposits",
                column: "user_id1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deposits");
        }
    }
}
