using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddIDatesInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCreation",
                table: "Rapport");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DC",
                table: "Rapport",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DC",
                table: "Rappels",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DC",
                table: "Inspection",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DC",
                table: "Erabliere",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DC",
                table: "CustomerErablieres",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DC",
                table: "CapteurImage",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DC",
                table: "Appareil",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DC",
                table: "Rapport");

            migrationBuilder.DropColumn(
                name: "DC",
                table: "Rappels");

            migrationBuilder.DropColumn(
                name: "DC",
                table: "Inspection");

            migrationBuilder.DropColumn(
                name: "DC",
                table: "Erabliere");

            migrationBuilder.DropColumn(
                name: "DC",
                table: "CustomerErablieres");

            migrationBuilder.DropColumn(
                name: "DC",
                table: "CapteurImage");

            migrationBuilder.DropColumn(
                name: "DC",
                table: "Appareil");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreation",
                table: "Rapport",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
