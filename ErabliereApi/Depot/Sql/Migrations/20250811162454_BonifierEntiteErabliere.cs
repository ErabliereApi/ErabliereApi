using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class BonifierEntiteErabliere : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Addresse",
                table: "Erabliere",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Base",
                table: "Erabliere",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Erabliere",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionAdministrative",
                table: "Erabliere",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Sommet",
                table: "Erabliere",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Addresse",
                table: "Erabliere");

            migrationBuilder.DropColumn(
                name: "Base",
                table: "Erabliere");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Erabliere");

            migrationBuilder.DropColumn(
                name: "RegionAdministrative",
                table: "Erabliere");

            migrationBuilder.DropColumn(
                name: "Sommet",
                table: "Erabliere");
        }
    }
}
