using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clbio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V1_TaskStatusRefactored : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionStatus",
                table: "Tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProgressStatus",
                table: "Tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionStatus",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ProgressStatus",
                table: "Tasks");
        }
    }
}
