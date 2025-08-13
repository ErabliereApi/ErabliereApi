using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class BonifierEntiteErabliere : Migration
    {
        const string tableName = "Erabliere";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Addresse",
                table: tableName,
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Base",
                table: tableName,
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: tableName,
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionAdministrative",
                table: tableName,
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Sommet",
                table: tableName,
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Addresse",
                table: tableName);

            migrationBuilder.DropColumn(
                name: "Base",
                table: tableName);

            migrationBuilder.DropColumn(
                name: "Description",
                table: tableName);

            migrationBuilder.DropColumn(
                name: "RegionAdministrative",
                table: tableName);

            migrationBuilder.DropColumn(
                name: "Sommet",
                table: tableName);
        }
    }
}
