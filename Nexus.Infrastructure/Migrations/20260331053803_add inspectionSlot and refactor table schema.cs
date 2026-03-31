using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addinspectionSlotandrefactortableschema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_agents_agencies_agency_id",
                table: "agents");

            migrationBuilder.DropForeignKey(
                name: "fk_chat_messages_chat_sessions_chat_session_id",
                table: "chat_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_chat_sessions_users_user_id",
                table: "chat_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_enquiries_agencts_agent_id",
                table: "enquiries");

            migrationBuilder.DropForeignKey(
                name: "fk_enquiries_listings_listing_id",
                table: "enquiries");

            migrationBuilder.DropForeignKey(
                name: "fk_enquiries_properties_property_id",
                table: "enquiries");

            migrationBuilder.DropForeignKey(
                name: "fk_enquiries_users_user_id",
                table: "enquiries");

            migrationBuilder.DropForeignKey(
                name: "fk_inspection_bookings_agencts_agent_id",
                table: "inspection_bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_inspection_bookings_listings_listing_id",
                table: "inspection_bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_inspection_bookings_properties_property_id",
                table: "inspection_bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_inspection_bookings_users_user_id",
                table: "inspection_bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_listings_agencies_agency_id",
                table: "listings");

            migrationBuilder.DropForeignKey(
                name: "fk_listings_agencts_agent_id",
                table: "listings");

            migrationBuilder.DropForeignKey(
                name: "fk_listings_properties_property_id",
                table: "listings");

            migrationBuilder.DropForeignKey(
                name: "fk_properties_agencts_agent_id",
                table: "properties");

            migrationBuilder.DropIndex(
                name: "ix_inspection_bookings_inspection_start_at_utc",
                table: "inspection_bookings");

            migrationBuilder.DropIndex(
                name: "ix_inspection_bookings_user_id",
                table: "inspection_bookings");

            migrationBuilder.DropColumn(
                name: "inspection_end_at_utc",
                table: "inspection_bookings");

            migrationBuilder.DropColumn(
                name: "inspection_start_at_utc",
                table: "inspection_bookings");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "listing_id",
                table: "inspection_bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "agent_id",
                table: "inspection_bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "inspection_slot_id",
                table: "inspection_bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "inspection_bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "inspection_bookings",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "inspection_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspection_slots", x => x.id);
                    table.CheckConstraint("CK_inspection_slots_capacity_positive", "capacity > 0");
                    table.CheckConstraint("CK_inspection_slots_end_after_start", "end_at_utc > start_at_utc");
                    table.ForeignKey(
                        name: "fk_inspection_slots_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspection_slots_listings_listing_id",
                        column: x => x.listing_id,
                        principalTable: "listings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspection_slots_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inspection_slots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inspection_bookings_inspection_slot_id_status",
                table: "inspection_bookings",
                columns: new[] { "inspection_slot_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_inspection_bookings_user_id_status",
                table: "inspection_bookings",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_inspection_slots_agent_id",
                table: "inspection_slots",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_slots_agent_id_start_at_utc",
                table: "inspection_slots",
                columns: new[] { "agent_id", "start_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_inspection_slots_listing_id",
                table: "inspection_slots",
                column: "listing_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_slots_listing_id_start_at_utc",
                table: "inspection_slots",
                columns: new[] { "listing_id", "start_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_inspection_slots_property_id",
                table: "inspection_slots",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_slots_property_id_start_at_utc",
                table: "inspection_slots",
                columns: new[] { "property_id", "start_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_inspection_slots_start_at_utc",
                table: "inspection_slots",
                column: "start_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_slots_status",
                table: "inspection_slots",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_slots_status_start_at_utc",
                table: "inspection_slots",
                columns: new[] { "status", "start_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_inspection_slots_user_id",
                table: "inspection_slots",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_agents_agencies_agency_id",
                table: "agents",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_messages_chat_sessions_chat_session_id",
                table: "chat_messages",
                column: "chat_session_id",
                principalTable: "chat_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_sessions_users_user_id",
                table: "chat_sessions",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_enquiries_agents_agent_id",
                table: "enquiries",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_enquiries_listings_listing_id",
                table: "enquiries",
                column: "listing_id",
                principalTable: "listings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_enquiries_properties_property_id",
                table: "enquiries",
                column: "property_id",
                principalTable: "properties",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_enquiries_users_user_id",
                table: "enquiries",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inspection_bookings_agents_agent_id",
                table: "inspection_bookings",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inspection_bookings_inspection_slots_inspection_slot_id",
                table: "inspection_bookings",
                column: "inspection_slot_id",
                principalTable: "inspection_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inspection_bookings_listings_listing_id",
                table: "inspection_bookings",
                column: "listing_id",
                principalTable: "listings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inspection_bookings_properties_property_id",
                table: "inspection_bookings",
                column: "property_id",
                principalTable: "properties",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inspection_bookings_users_user_id",
                table: "inspection_bookings",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_listings_agencies_agency_id",
                table: "listings",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_listings_agents_agent_id",
                table: "listings",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_listings_properties_property_id",
                table: "listings",
                column: "property_id",
                principalTable: "properties",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_properties_agents_agent_id",
                table: "properties",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_agents_agencies_agency_id",
                table: "agents");

            migrationBuilder.DropForeignKey(
                name: "fk_chat_messages_chat_sessions_chat_session_id",
                table: "chat_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_chat_sessions_users_user_id",
                table: "chat_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_enquiries_agents_agent_id",
                table: "enquiries");

            migrationBuilder.DropForeignKey(
                name: "fk_enquiries_listings_listing_id",
                table: "enquiries");

            migrationBuilder.DropForeignKey(
                name: "fk_enquiries_properties_property_id",
                table: "enquiries");

            migrationBuilder.DropForeignKey(
                name: "fk_enquiries_users_user_id",
                table: "enquiries");

            migrationBuilder.DropForeignKey(
                name: "fk_inspection_bookings_agents_agent_id",
                table: "inspection_bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_inspection_bookings_inspection_slots_inspection_slot_id",
                table: "inspection_bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_inspection_bookings_listings_listing_id",
                table: "inspection_bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_inspection_bookings_properties_property_id",
                table: "inspection_bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_inspection_bookings_users_user_id",
                table: "inspection_bookings");

            migrationBuilder.DropForeignKey(
                name: "fk_listings_agencies_agency_id",
                table: "listings");

            migrationBuilder.DropForeignKey(
                name: "fk_listings_agents_agent_id",
                table: "listings");

            migrationBuilder.DropForeignKey(
                name: "fk_listings_properties_property_id",
                table: "listings");

            migrationBuilder.DropForeignKey(
                name: "fk_properties_agents_agent_id",
                table: "properties");

            migrationBuilder.DropTable(
                name: "inspection_slots");

            migrationBuilder.DropIndex(
                name: "ix_inspection_bookings_inspection_slot_id_status",
                table: "inspection_bookings");

            migrationBuilder.DropIndex(
                name: "ix_inspection_bookings_user_id_status",
                table: "inspection_bookings");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "inspection_slot_id",
                table: "inspection_bookings");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "inspection_bookings");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "inspection_bookings");

            migrationBuilder.AlterColumn<Guid>(
                name: "listing_id",
                table: "inspection_bookings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "agent_id",
                table: "inspection_bookings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "inspection_end_at_utc",
                table: "inspection_bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "inspection_start_at_utc",
                table: "inspection_bookings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "ix_inspection_bookings_inspection_start_at_utc",
                table: "inspection_bookings",
                column: "inspection_start_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_bookings_user_id",
                table: "inspection_bookings",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_agents_agencies_agency_id",
                table: "agents",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_messages_chat_sessions_chat_session_id",
                table: "chat_messages",
                column: "chat_session_id",
                principalTable: "chat_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_sessions_users_user_id",
                table: "chat_sessions",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_enquiries_agencts_agent_id",
                table: "enquiries",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_enquiries_listings_listing_id",
                table: "enquiries",
                column: "listing_id",
                principalTable: "listings",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_enquiries_properties_property_id",
                table: "enquiries",
                column: "property_id",
                principalTable: "properties",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_enquiries_users_user_id",
                table: "enquiries",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_inspection_bookings_agencts_agent_id",
                table: "inspection_bookings",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_inspection_bookings_listings_listing_id",
                table: "inspection_bookings",
                column: "listing_id",
                principalTable: "listings",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_inspection_bookings_properties_property_id",
                table: "inspection_bookings",
                column: "property_id",
                principalTable: "properties",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_inspection_bookings_users_user_id",
                table: "inspection_bookings",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_listings_agencies_agency_id",
                table: "listings",
                column: "agency_id",
                principalTable: "agencies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_listings_agencts_agent_id",
                table: "listings",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_listings_properties_property_id",
                table: "listings",
                column: "property_id",
                principalTable: "properties",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_properties_agencts_agent_id",
                table: "properties",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
