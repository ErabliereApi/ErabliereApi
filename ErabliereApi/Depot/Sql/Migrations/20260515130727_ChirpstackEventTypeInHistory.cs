using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class ChirpstackEventTypeInHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "ChirpstackMessageHistory",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventType",
                table: "ChirpstackMessageHistory");
        }
    }
}
