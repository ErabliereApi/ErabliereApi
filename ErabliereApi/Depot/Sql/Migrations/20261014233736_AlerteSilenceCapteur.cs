using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AlerteSilenceCapteur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "EpisodesAlerteCapteur",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateDerniereReception",
                table: "Capteurs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DelaiSilenceMinutes",
                table: "AlerteCapteurs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EnSilenceDepuis",
                table: "AlerteCapteurs",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "EpisodesAlerteCapteur");

            migrationBuilder.DropColumn(
                name: "DateDerniereReception",
                table: "Capteurs");

            migrationBuilder.DropColumn(
                name: "DelaiSilenceMinutes",
                table: "AlerteCapteurs");

            migrationBuilder.DropColumn(
                name: "EnSilenceDepuis",
                table: "AlerteCapteurs");
        }
    }
}
