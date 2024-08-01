using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class SensorStatusAndGeoLocalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Erabliere",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Erabliere",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Capteurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastMessageTime",
                table: "Capteurs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Capteurs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Capteurs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "Online",
                table: "Capteurs",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportFrequency",
                table: "Capteurs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Capteurs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "IdErabliere",
                table: "CapteurImage",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "CapteurImage",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "CapteurImage",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Erabliere");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Erabliere");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Capteurs");

            migrationBuilder.DropColumn(
                name: "LastMessageTime",
                table: "Capteurs");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Capteurs");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Capteurs");

            migrationBuilder.DropColumn(
                name: "Online",
                table: "Capteurs");

            migrationBuilder.DropColumn(
                name: "ReportFrequency",
                table: "Capteurs");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Capteurs");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "CapteurImage");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "CapteurImage");

            migrationBuilder.AlterColumn<Guid>(
                name: "IdErabliere",
                table: "CapteurImage",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
