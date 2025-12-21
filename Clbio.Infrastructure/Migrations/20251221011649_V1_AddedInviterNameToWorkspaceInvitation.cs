using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clbio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V1_AddedInviterNameToWorkspaceInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviterName",
                table: "WorkspaceInvitations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InviterName",
                table: "WorkspaceInvitations");
        }
    }
}
