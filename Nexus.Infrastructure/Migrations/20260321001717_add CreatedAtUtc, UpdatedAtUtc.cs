using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addCreatedAtUtcUpdatedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "modified_at_utc",
                table: "users",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "refund",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "refund",
                newName: "created_at_utc");

            migrationBuilder.RenameColumn(
                name: "modified_at_utc",
                table: "agencies",
                newName: "updated_at_utc");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "tool_executions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at_utc",
                table: "property_types",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "property_types",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "property_images",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at_utc",
                table: "property_addresses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "property_addresses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at_utc",
                table: "properties",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "properties",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at_utc",
                table: "listings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "listings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "inspection_bookings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "enquiries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at_utc",
                table: "agents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "agents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "tool_executions");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "property_types");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "property_types");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "property_images");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "property_addresses");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "property_addresses");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "inspection_bookings");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "enquiries");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "agents");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "users",
                newName: "modified_at_utc");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "refund",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "refund",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "agencies",
                newName: "modified_at_utc");
        }
    }
}
