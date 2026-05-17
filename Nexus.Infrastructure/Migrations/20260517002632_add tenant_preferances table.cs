using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addtenant_preferancestable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "pet_friendly",
                table: "properties",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tenant_preferences",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    suburbs = table.Column<string[]>(type: "text[]", nullable: false),
                    max_rent = table.Column<int>(type: "integer", nullable: false),
                    min_beds = table.Column<int>(type: "integer", nullable: false),
                    max_beds = table.Column<int>(type: "integer", nullable: false),
                    pet_friendly = table.Column<bool>(type: "boolean", nullable: false),
                    available_within_days = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_preferences", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_tenant_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_preferences");

            migrationBuilder.DropColumn(
                name: "pet_friendly",
                table: "properties");
        }
    }
}
