using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentTypetoInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "document_type",
                table: "invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "document_type",
                table: "invoices");
        }
    }
}
