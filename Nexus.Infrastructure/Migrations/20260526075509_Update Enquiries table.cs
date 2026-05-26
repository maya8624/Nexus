using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEnquiriestable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_enquiries_tenants_tenant_id",
                table: "enquiries");

            migrationBuilder.AddForeignKey(
                name: "fk_enquiries_tenants_tenant_id",
                table: "enquiries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_enquiries_tenants_tenant_id",
                table: "enquiries");

            migrationBuilder.AddForeignKey(
                name: "fk_enquiries_tenants_tenant_id",
                table: "enquiries",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id");
        }
    }
}
