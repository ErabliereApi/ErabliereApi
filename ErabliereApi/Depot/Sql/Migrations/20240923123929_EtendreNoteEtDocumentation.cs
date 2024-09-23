using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class EtendreNoteEtDocumentation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalStorageType",
                table: "Notes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalStorageUrl",
                table: "Notes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Notes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FileSize",
                table: "Notes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Notes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExternalStorageType",
                table: "Documentation",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalStorageUrl",
                table: "Documentation",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Documentation",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FileSize",
                table: "Documentation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Documentation",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalStorageType",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "ExternalStorageUrl",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "ExternalStorageType",
                table: "Documentation");

            migrationBuilder.DropColumn(
                name: "ExternalStorageUrl",
                table: "Documentation");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Documentation");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Documentation");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Documentation");
        }
    }
}
