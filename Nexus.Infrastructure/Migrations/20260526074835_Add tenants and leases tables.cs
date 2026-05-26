using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addtenantsandleasestables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "message",
                table: "enquiries");

            migrationBuilder.RenameColumn(
                name: "responded_at_utc",
                table: "enquiries",
                newName: "replied_at_utc");

            migrationBuilder.AlterColumn<Guid>(
                name: "agent_id",
                table: "enquiries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "body",
                table: "enquiries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "draft_reply",
                table: "enquiries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "intent",
                table: "enquiries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "lease_id",
                table: "enquiries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sent_reply",
                table: "enquiries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "enquiries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "leases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    weekly_rent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bond_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    water_included = table.Column<bool>(type: "boolean", nullable: false),
                    water_allowance_litres_per_day = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    vacating_date = table.Column<DateOnly>(type: "date", nullable: true),
                    vacating_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leases", x => x.id);
                    table.ForeignKey(
                        name: "fk_leases_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_leases_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lease_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    lease_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    lease_end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    weekly_rent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bond_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenants_leases_lease_id",
                        column: x => x.lease_id,
                        principalTable: "leases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenants_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenants_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_enquiries_lease_id",
                table: "enquiries",
                column: "lease_id");

            migrationBuilder.CreateIndex(
                name: "ix_enquiries_tenant_id",
                table: "enquiries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_leases_agent_id",
                table: "leases",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "ix_leases_property_id",
                table: "leases",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_leases_status",
                table: "leases",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_leases_tenant_id",
                table: "leases",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_email",
                table: "tenants",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_lease_id",
                table: "tenants",
                column: "lease_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_property_id",
                table: "tenants",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_status",
                table: "tenants",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_user_id",
                table: "tenants",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_enquiries_leases_lease_id",
                table: "enquiries",
                column: "lease_id",
                principalTable: "leases",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_enquiries_tenants_tenant_id",
                table: "enquiries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_leases_tenants_tenant_id",
                table: "leases",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_enquiries_leases_lease_id",
                table: "enquiries");

            migrationBuilder.DropForeignKey(
                name: "fk_enquiries_tenants_tenant_id",
                table: "enquiries");

            migrationBuilder.DropForeignKey(
                name: "fk_leases_tenants_tenant_id",
                table: "leases");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "leases");

            migrationBuilder.DropIndex(
                name: "ix_enquiries_lease_id",
                table: "enquiries");

            migrationBuilder.DropIndex(
                name: "ix_enquiries_tenant_id",
                table: "enquiries");

            migrationBuilder.DropColumn(
                name: "body",
                table: "enquiries");

            migrationBuilder.DropColumn(
                name: "draft_reply",
                table: "enquiries");

            migrationBuilder.DropColumn(
                name: "intent",
                table: "enquiries");

            migrationBuilder.DropColumn(
                name: "lease_id",
                table: "enquiries");

            migrationBuilder.DropColumn(
                name: "sent_reply",
                table: "enquiries");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "enquiries");

            migrationBuilder.RenameColumn(
                name: "replied_at_utc",
                table: "enquiries",
                newName: "responded_at_utc");

            migrationBuilder.AlterColumn<Guid>(
                name: "agent_id",
                table: "enquiries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "message",
                table: "enquiries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }
    }
}
