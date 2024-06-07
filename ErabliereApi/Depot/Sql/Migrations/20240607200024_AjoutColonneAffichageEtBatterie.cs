using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AjoutColonneAffichageEtBatterie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AfficherPredictionMeteoHeure",
                table: "Erabliere",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AfficherPredictionMeteoJour",
                table: "Erabliere",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "DimensionPanneauImage",
                table: "Erabliere",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAccessTime",
                table: "Customers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "BatteryLevel",
                table: "Capteurs",
                type: "tinyint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfficherPredictionMeteoHeure",
                table: "Erabliere");

            migrationBuilder.DropColumn(
                name: "AfficherPredictionMeteoJour",
                table: "Erabliere");

            migrationBuilder.DropColumn(
                name: "DimensionPanneauImage",
                table: "Erabliere");

            migrationBuilder.DropColumn(
                name: "LastAccessTime",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BatteryLevel",
                table: "Capteurs");
        }
    }
}
