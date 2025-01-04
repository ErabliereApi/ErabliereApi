using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayRangeOnCapteur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DisplayMax",
                table: "Capteurs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DisplayMin",
                table: "Capteurs",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayMax",
                table: "Capteurs");

            migrationBuilder.DropColumn(
                name: "DisplayMin",
                table: "Capteurs");
        }
    }
}
