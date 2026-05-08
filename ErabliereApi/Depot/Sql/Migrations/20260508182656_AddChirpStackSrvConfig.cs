using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddChirpStackSrvConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChirpstackSrvConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApplicationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApplicationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeviceProfileId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeviceProfileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DevEui = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeviceClassEnabled = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChirpstackSrvConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChirpstackSrvConfigs");
        }
    }
}
