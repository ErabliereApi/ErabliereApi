using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class ChirpstackMessageHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LastDeviceSeen",
                table: "ChirpstackSrvConfigs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KeepNLastMessage",
                table: "ChirpstackSrvConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeToKeepLastMessage",
                table: "ChirpstackSrvConfigs",
                type: "time",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChirpstackMessageHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChirpStackSrvConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MessageJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Data = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DecodedData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChirpstackMessageHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChirpstackMessageHistory_ChirpstackSrvConfigs_ChirpStackSrvConfigId",
                        column: x => x.ChirpStackSrvConfigId,
                        principalTable: "ChirpstackSrvConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChirpstackMessageHistory_ChirpStackSrvConfigId",
                table: "ChirpstackMessageHistory",
                column: "ChirpStackSrvConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChirpstackMessageHistory");

            migrationBuilder.DropColumn(
                name: "KeepNLastMessage",
                table: "ChirpstackSrvConfigs");

            migrationBuilder.DropColumn(
                name: "TimeToKeepLastMessage",
                table: "ChirpstackSrvConfigs");

            migrationBuilder.AlterColumn<string>(
                name: "LastDeviceSeen",
                table: "ChirpstackSrvConfigs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
