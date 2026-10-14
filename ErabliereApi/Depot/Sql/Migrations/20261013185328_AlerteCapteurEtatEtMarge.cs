using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AlerteCapteurEtatEtMarge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateChangementEtat",
                table: "AlerteCapteurs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstEnAlerte",
                table: "AlerteCapteurs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Marge",
                table: "AlerteCapteurs",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateChangementEtat",
                table: "AlerteCapteurs");

            migrationBuilder.DropColumn(
                name: "EstEnAlerte",
                table: "AlerteCapteurs");

            migrationBuilder.DropColumn(
                name: "Marge",
                table: "AlerteCapteurs");
        }
    }
}
