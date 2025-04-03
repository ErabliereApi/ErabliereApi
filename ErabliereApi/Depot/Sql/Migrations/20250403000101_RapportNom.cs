using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class RapportNom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCreation",
                table: "Rapports");

            migrationBuilder.AddColumn<string>(
                name: "Nom",
                table: "Rapports",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nom",
                table: "Rapports");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateCreation",
                table: "Rapports",
                type: "datetimeoffset",
                nullable: true);
        }
    }
}
