using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnumConversionsToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_inspection_bookings_inspection_slot_id_user_id",
                table: "inspection_bookings");

            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_inspection_bookings_slot_user_active;");

            migrationBuilder.Sql("DROP VIEW IF EXISTS v_listings;");

            migrationBuilder.Sql(@"ALTER TABLE tenants ALTER COLUMN status TYPE integer USING
                CASE status WHEN 'Active' THEN 1 WHEN 'Vacating' THEN 2 WHEN 'Former' THEN 3 WHEN 'Prospective' THEN 4 ELSE status::integer END;");

            migrationBuilder.Sql(@"ALTER TABLE listings ALTER COLUMN status TYPE integer USING
                CASE status WHEN 'Draft' THEN 1 WHEN 'Active' THEN 2 WHEN 'UnderOffer' THEN 3 WHEN 'Sold' THEN 4 WHEN 'Leased' THEN 5 WHEN 'Withdrawn' THEN 6 ELSE status::integer END;");

            migrationBuilder.Sql(@"ALTER TABLE listings ALTER COLUMN listing_type TYPE integer USING
                CASE listing_type WHEN 'Sale' THEN 1 WHEN 'Rent' THEN 2 ELSE listing_type::integer END;");

            migrationBuilder.Sql(@"ALTER TABLE leases ALTER COLUMN type TYPE integer USING
                CASE type WHEN 'FixedTerm' THEN 1 WHEN 'Periodic' THEN 2 ELSE type::integer END;");

            migrationBuilder.Sql(@"ALTER TABLE leases ALTER COLUMN status TYPE integer USING
                CASE status WHEN 'Active' THEN 1 WHEN 'Expiring' THEN 2 WHEN 'Periodic' THEN 3 WHEN 'Vacating' THEN 4 WHEN 'Ended' THEN 5 ELSE status::integer END;");

            migrationBuilder.Sql(@"ALTER TABLE inspection_slots ALTER COLUMN status TYPE integer USING
                CASE status WHEN 'Open' THEN 1 WHEN 'Closed' THEN 2 WHEN 'Cancelled' THEN 3 ELSE status::integer END;");

            migrationBuilder.Sql(@"ALTER TABLE inspection_bookings ALTER COLUMN status TYPE integer USING
                CASE status WHEN 'Pending' THEN 1 WHEN 'Confirmed' THEN 2 WHEN 'Cancelled' THEN 3 WHEN 'Completed' THEN 4 WHEN 'NoShow' THEN 5 ELSE status::integer END;");

            migrationBuilder.Sql(@"ALTER TABLE enquiries ALTER COLUMN status TYPE integer USING
                CASE status WHEN 'New' THEN 1 WHEN 'Drafted' THEN 2 WHEN 'Replied' THEN 3 WHEN 'Closed' THEN 4 ELSE status::integer END;");

            migrationBuilder.Sql(@"ALTER TABLE deposits ALTER COLUMN status TYPE integer USING
                CASE status WHEN 'Pending' THEN 1 WHEN 'Paid' THEN 2 WHEN 'Refunded' THEN 3 WHEN 'Failed' THEN 4 ELSE status::integer END;");

            migrationBuilder.Sql(@"ALTER TABLE deposits ALTER COLUMN currency TYPE integer USING
                CASE currency WHEN 'AUD' THEN 0 WHEN 'USD' THEN 1 WHEN 'EUR' THEN 2 WHEN 'GBP' THEN 3 WHEN 'JPY' THEN 4 ELSE currency::integer END;");

            migrationBuilder.Sql(@"ALTER TABLE chat_messages ALTER COLUMN role TYPE integer USING
                CASE role WHEN 'User' THEN 1 WHEN 'Assistant' THEN 2 WHEN 'Tool' THEN 3 WHEN 'System' THEN 4 ELSE role::integer END;");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_bookings_inspection_slot_id_user_id",
                table: "inspection_bookings",
                columns: new[] { "inspection_slot_id", "user_id" },
                unique: true,
                filter: "status != 3 AND is_deleted = false");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_inspection_bookings_slot_user_active
                ON inspection_bookings (inspection_slot_id, user_id)
                WHERE status != 3 AND is_deleted = false;");

            migrationBuilder.Sql(@"
                CREATE VIEW v_listings AS
                SELECT l.id AS listing_id, l.listing_type, l.status AS listing_status, l.price,
                       l.is_published, l.listed_at_utc, l.available_from_utc,
                       p.id AS property_id, p.title, p.description, p.bedrooms, p.bathrooms,
                       p.car_spaces, p.land_size_sqm, p.building_size_sqm, p.year_built,
                       p.is_active, p.pet_friendly,
                       pt.name AS property_type,
                       pa.address_line1, pa.address_line2, pa.suburb, pa.state, pa.postcode,
                       pa.country, pa.latitude, pa.longitude,
                       pi.image_url,
                       ag.id AS agent_id, ag.first_name AS agent_first_name, ag.last_name AS agent_last_name,
                       ag.email AS agent_email, ag.phone_number AS agent_phone,
                       agc.id AS agency_id, agc.name AS agency_name,
                       agc.email AS agency_email, agc.phone_number AS agency_phone
                FROM listings l
                JOIN properties p ON l.property_id = p.id
                JOIN property_addresses pa ON pa.property_id = p.id
                JOIN property_types pt ON pt.id = p.property_type_id
                LEFT JOIN property_images pi ON pi.property_id = p.id AND pi.is_primary = true
                JOIN agents ag ON l.agent_id = ag.id
                JOIN agencies agc ON l.agency_id = agc.id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_inspection_bookings_inspection_slot_id_user_id",
                table: "inspection_bookings");

            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_inspection_bookings_slot_user_active;");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "tenants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "listings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "listing_type",
                table: "listings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "leases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "leases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "inspection_slots",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "inspection_bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "enquiries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "deposits",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "currency",
                table: "deposits",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "chat_messages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_bookings_inspection_slot_id_user_id",
                table: "inspection_bookings",
                columns: new[] { "inspection_slot_id", "user_id" },
                unique: true,
                filter: "status != 'Cancelled' AND is_deleted = false");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_inspection_bookings_slot_user_active
                ON inspection_bookings (inspection_slot_id, user_id)
                WHERE status != 'Cancelled' AND is_deleted = false;");
        }
    }
}
