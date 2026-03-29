using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddToolExectuionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tool_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    input_json = table.Column<string>(type: "text", nullable: true),
                    output_json = table.Column<string>(type: "text", nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tool_executions", x => x.id);
                    table.ForeignKey(
                        name: "fk_tool_executions_chat_messages_chat_message_id",
                        column: x => x.chat_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tool_executions_chat_message_id",
                table: "tool_executions",
                column: "chat_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_tool_executions_created_at_utc",
                table: "tool_executions",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_tool_executions_success",
                table: "tool_executions",
                column: "success");

            migrationBuilder.CreateIndex(
                name: "ix_tool_executions_tool_name",
                table: "tool_executions",
                column: "tool_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tool_executions");
        }
    }
}
