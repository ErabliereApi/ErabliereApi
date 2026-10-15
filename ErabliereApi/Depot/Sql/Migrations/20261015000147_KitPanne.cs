using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class KitPanne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdErabliere = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdApiKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdCapteurAlimentation = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdCapteurCharge = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdCapteurAutonomie = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateRevocation = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DC = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DM = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kits_Erabliere_IdErabliere",
                        column: x => x.IdErabliere,
                        principalTable: "Erabliere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kits_IdApiKey",
                table: "Kits",
                column: "IdApiKey");

            migrationBuilder.CreateIndex(
                name: "IX_Kits_IdErabliere",
                table: "Kits",
                column: "IdErabliere");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kits");
        }
    }
}
