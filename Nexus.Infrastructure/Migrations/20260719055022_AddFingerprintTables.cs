using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFingerprintTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fingerprints",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    level = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: true),
                    classified_by = table.Column<string>(type: "text", nullable: true),
                    exception_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    message_template = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    operation = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    service_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    total_count = table.Column<int>(type: "integer", nullable: false),
                    first_seen_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    auto_fix_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    github_issue_number = table.Column<int>(type: "integer", nullable: true),
                    github_status = table.Column<string>(type: "text", nullable: false),
                    github_last_commented_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fingerprints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ingest_cursor",
                columns: table => new
                {
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_polled_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingest_cursor", x => x.source);
                });

            migrationBuilder.CreateTable(
                name: "fingerprint_occurrences",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fingerprint_id = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    occurrence_count = table.Column<int>(type: "integer", nullable: false),
                    rendered_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fingerprint_occurrences", x => x.id);
                    table.ForeignKey(
                        name: "fk_fingerprint_occurrences_fingerprints_fingerprint_id",
                        column: x => x.fingerprint_id,
                        principalTable: "fingerprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fingerprint_occurrences_fingerprint_id_occurred_at",
                table: "fingerprint_occurrences",
                columns: new[] { "fingerprint_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fingerprints_github_status_level",
                table: "fingerprints",
                columns: new[] { "github_status", "level" });

            migrationBuilder.CreateIndex(
                name: "ix_fingerprints_hash",
                table: "fingerprints",
                column: "hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fingerprint_occurrences");

            migrationBuilder.DropTable(
                name: "ingest_cursor");

            migrationBuilder.DropTable(
                name: "fingerprints");
        }
    }
}
