using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fixrelationshipissuewithdeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_deposits_listings_listing_id1",
                table: "deposits");

            migrationBuilder.DropForeignKey(
                name: "fk_deposits_properties_property_id1",
                table: "deposits");

            migrationBuilder.DropForeignKey(
                name: "fk_deposits_users_user_id1",
                table: "deposits");

            migrationBuilder.DropIndex(
                name: "ix_deposits_listing_id1",
                table: "deposits");

            migrationBuilder.DropIndex(
                name: "ix_deposits_property_id1",
                table: "deposits");

            migrationBuilder.DropIndex(
                name: "ix_deposits_user_id1",
                table: "deposits");

            migrationBuilder.DropColumn(
                name: "listing_id1",
                table: "deposits");

            migrationBuilder.DropColumn(
                name: "property_id1",
                table: "deposits");

            migrationBuilder.DropColumn(
                name: "user_id1",
                table: "deposits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "listing_id1",
                table: "deposits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "property_id1",
                table: "deposits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id1",
                table: "deposits",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_deposits_listing_id1",
                table: "deposits",
                column: "listing_id1");

            migrationBuilder.CreateIndex(
                name: "ix_deposits_property_id1",
                table: "deposits",
                column: "property_id1");

            migrationBuilder.CreateIndex(
                name: "ix_deposits_user_id1",
                table: "deposits",
                column: "user_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_deposits_listings_listing_id1",
                table: "deposits",
                column: "listing_id1",
                principalTable: "listings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_deposits_properties_property_id1",
                table: "deposits",
                column: "property_id1",
                principalTable: "properties",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_deposits_users_user_id1",
                table: "deposits",
                column: "user_id1",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
