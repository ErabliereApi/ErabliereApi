using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddLastTimeSeenAndClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DevEui",
                table: "ChirpstackSrvConfigs");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "ChirpstackSrvConfigs");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastTimeSeen",
                table: "ChirpstackSrvConfigs",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTimeSeen",
                table: "ChirpstackSrvConfigs");

            migrationBuilder.AddColumn<string>(
                name: "DevEui",
                table: "ChirpstackSrvConfigs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "ChirpstackSrvConfigs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
