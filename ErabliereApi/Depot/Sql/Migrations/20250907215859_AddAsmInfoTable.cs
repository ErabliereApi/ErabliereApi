using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddAsmInfoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IpNetworkAsnInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Network = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Continent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContinentCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    ASN = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    AS_Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AS_Domain = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpNetworkAsnInfos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IpInfos_Ip",
                table: "IpInfos",
                column: "Ip");

            migrationBuilder.CreateIndex(
                name: "IX_IpNetworkAsnInfos_ASN",
                table: "IpNetworkAsnInfos",
                column: "ASN");

            migrationBuilder.CreateIndex(
                name: "IX_IpNetworkAsnInfos_Network",
                table: "IpNetworkAsnInfos",
                column: "Network");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IpNetworkAsnInfos");

            migrationBuilder.DropIndex(
                name: "IX_IpInfos_Ip",
                table: "IpInfos");
        }
    }
}
