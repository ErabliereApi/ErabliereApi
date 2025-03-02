using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AmeliorationRapports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AfficherDansDashboard",
                table: "Rapports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDebut",
                table: "Rapports",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateFin",
                table: "Rapports",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "Max",
                table: "Rapports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Min",
                table: "Rapports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Moyenne",
                table: "Rapports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SeuilTemperature",
                table: "Rapports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Somme",
                table: "Rapports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "UtiliserTemperatureTrioDonnee",
                table: "Rapports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Max",
                table: "DonneesRapports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Min",
                table: "DonneesRapports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfficherDansDashboard",
                table: "Rapports");

            migrationBuilder.DropColumn(
                name: "DateDebut",
                table: "Rapports");

            migrationBuilder.DropColumn(
                name: "DateFin",
                table: "Rapports");

            migrationBuilder.DropColumn(
                name: "Max",
                table: "Rapports");

            migrationBuilder.DropColumn(
                name: "Min",
                table: "Rapports");

            migrationBuilder.DropColumn(
                name: "Moyenne",
                table: "Rapports");

            migrationBuilder.DropColumn(
                name: "SeuilTemperature",
                table: "Rapports");

            migrationBuilder.DropColumn(
                name: "Somme",
                table: "Rapports");

            migrationBuilder.DropColumn(
                name: "UtiliserTemperatureTrioDonnee",
                table: "Rapports");

            migrationBuilder.DropColumn(
                name: "Max",
                table: "DonneesRapports");

            migrationBuilder.DropColumn(
                name: "Min",
                table: "DonneesRapports");
        }
    }
}
