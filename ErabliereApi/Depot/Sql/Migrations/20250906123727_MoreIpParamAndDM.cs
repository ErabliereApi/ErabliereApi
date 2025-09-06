using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class MoreIpParamAndDM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TTL",
                table: "IpInfos",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DC",
                table: "Alertes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DM",
                table: "Alertes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DM",
                table: "AlerteCapteurs",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TTL",
                table: "IpInfos");

            migrationBuilder.DropColumn(
                name: "DC",
                table: "Alertes");

            migrationBuilder.DropColumn(
                name: "DM",
                table: "Alertes");

            migrationBuilder.DropColumn(
                name: "DM",
                table: "AlerteCapteurs");
        }
    }
}
