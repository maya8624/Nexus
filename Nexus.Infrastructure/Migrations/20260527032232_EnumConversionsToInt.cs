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

            migrationBuilder.Sql("DROP VIEW IF EXISTS v_listings;");

            migrationBuilder.Sql("ALTER TABLE tenants ALTER COLUMN status TYPE integer USING status::integer;");
            migrationBuilder.Sql("ALTER TABLE listings ALTER COLUMN status TYPE integer USING status::integer;");
            migrationBuilder.Sql("ALTER TABLE listings ALTER COLUMN listing_type TYPE integer USING listing_type::integer;");
            migrationBuilder.Sql("ALTER TABLE leases ALTER COLUMN type TYPE integer USING type::integer;");
            migrationBuilder.Sql("ALTER TABLE leases ALTER COLUMN status TYPE integer USING status::integer;");
            migrationBuilder.Sql("ALTER TABLE inspection_slots ALTER COLUMN status TYPE integer USING status::integer;");
            migrationBuilder.Sql("ALTER TABLE inspection_bookings ALTER COLUMN status TYPE integer USING status::integer;");
            migrationBuilder.Sql("ALTER TABLE enquiries ALTER COLUMN status TYPE integer USING status::integer;");
            migrationBuilder.Sql("ALTER TABLE deposits ALTER COLUMN status TYPE integer USING status::integer;");
            migrationBuilder.Sql("ALTER TABLE deposits ALTER COLUMN currency TYPE integer USING currency::integer;");
            migrationBuilder.Sql("ALTER TABLE chat_messages ALTER COLUMN role TYPE integer USING role::integer;");

            migrationBuilder.CreateIndex(
                name: "ix_inspection_bookings_inspection_slot_id_user_id",
                table: "inspection_bookings",
                columns: new[] { "inspection_slot_id", "user_id" },
                unique: true,
                filter: "status != 3 AND is_deleted = false");

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
        }
    }
}
