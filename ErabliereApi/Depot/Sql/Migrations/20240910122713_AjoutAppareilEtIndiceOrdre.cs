using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AjoutAppareilEtIndiceOrdre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IndiceOrdre",
                table: "CustomerErablieres",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AppareilId",
                table: "Capteurs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Appareil",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdErabliere = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErabliereId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appareil", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appareil_Erabliere_ErabliereId",
                        column: x => x.ErabliereId,
                        principalTable: "Erabliere",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Capteurs_AppareilId",
                table: "Capteurs",
                column: "AppareilId");

            migrationBuilder.CreateIndex(
                name: "IX_Appareil_ErabliereId",
                table: "Appareil",
                column: "ErabliereId");

            migrationBuilder.AddForeignKey(
                name: "FK_Capteurs_Appareil_AppareilId",
                table: "Capteurs",
                column: "AppareilId",
                principalTable: "Appareil",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Capteurs_Appareil_AppareilId",
                table: "Capteurs");

            migrationBuilder.DropTable(
                name: "Appareil");

            migrationBuilder.DropIndex(
                name: "IX_Capteurs_AppareilId",
                table: "Capteurs");

            migrationBuilder.DropColumn(
                name: "IndiceOrdre",
                table: "CustomerErablieres");

            migrationBuilder.DropColumn(
                name: "AppareilId",
                table: "Capteurs");
        }
    }
}
