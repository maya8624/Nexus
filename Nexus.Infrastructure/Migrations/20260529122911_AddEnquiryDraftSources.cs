using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnquiryDraftSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "draft_sources",
                table: "enquiries",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "draft_sources",
                table: "enquiries");
        }
    }
}
