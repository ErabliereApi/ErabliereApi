using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AjoutTubelure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Arbres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdErabliere = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Espece = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    DC = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DM = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arbres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Arbres_Erabliere_IdErabliere",
                        column: x => x.IdErabliere,
                        principalTable: "Erabliere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LignesTubelure",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdErabliere = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TypeLigne = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CoordonneesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DC = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DM = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesTubelure", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LignesTubelure_Erabliere_IdErabliere",
                        column: x => x.IdErabliere,
                        principalTable: "Erabliere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Entailles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdErabliere = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    IdArbre = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdLigneTubelure = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DC = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DM = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entailles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entailles_Arbres_IdArbre",
                        column: x => x.IdArbre,
                        principalTable: "Arbres",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Entailles_Erabliere_IdErabliere",
                        column: x => x.IdErabliere,
                        principalTable: "Erabliere",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Entailles_LignesTubelure_IdLigneTubelure",
                        column: x => x.IdLigneTubelure,
                        principalTable: "LignesTubelure",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Arbres_IdErabliere",
                table: "Arbres",
                column: "IdErabliere");

            migrationBuilder.CreateIndex(
                name: "IX_Entailles_IdArbre",
                table: "Entailles",
                column: "IdArbre");

            migrationBuilder.CreateIndex(
                name: "IX_Entailles_IdErabliere",
                table: "Entailles",
                column: "IdErabliere");

            migrationBuilder.CreateIndex(
                name: "IX_Entailles_IdLigneTubelure",
                table: "Entailles",
                column: "IdLigneTubelure");

            migrationBuilder.CreateIndex(
                name: "IX_LignesTubelure_IdErabliere",
                table: "LignesTubelure",
                column: "IdErabliere");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entailles");

            migrationBuilder.DropTable(
                name: "Arbres");

            migrationBuilder.DropTable(
                name: "LignesTubelure");
        }
    }
}
