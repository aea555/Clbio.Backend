using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clbio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V1_AddWorkspaceStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Workspaces",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "Permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Permissions");
        }
    }
}
