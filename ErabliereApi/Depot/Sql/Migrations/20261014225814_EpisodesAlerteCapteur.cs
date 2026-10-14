using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class EpisodesAlerteCapteur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EpisodesAlerteCapteur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdAlerteCapteur = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdCapteur = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NomAlerte = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateDebut = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValeurDebut = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    DateFin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ValeurFin = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    RaisonFin = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodesAlerteCapteur", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodesAlerteCapteur_AlerteCapteurs_IdAlerteCapteur",
                        column: x => x.IdAlerteCapteur,
                        principalTable: "AlerteCapteurs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EpisodesAlerteCapteur_Capteurs_IdCapteur",
                        column: x => x.IdCapteur,
                        principalTable: "Capteurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EpisodesAlerteCapteur_IdAlerteCapteur_DateFin",
                table: "EpisodesAlerteCapteur",
                columns: new[] { "IdAlerteCapteur", "DateFin" });

            migrationBuilder.CreateIndex(
                name: "IX_EpisodesAlerteCapteur_IdCapteur_DateDebut",
                table: "EpisodesAlerteCapteur",
                columns: new[] { "IdCapteur", "DateDebut" });

            // Une alerte déjà en cours au déploiement n'a pas d'épisode ouvert. On l'ouvre à la date
            // de la donnée qui l'a commencée, sans valeur, pour que son retour à la normale le ferme.
            migrationBuilder.Sql(@"
INSERT INTO EpisodesAlerteCapteur (Id, IdAlerteCapteur, IdCapteur, NomAlerte, DateDebut)
SELECT NEWID(), Id, IdCapteur, Nom, COALESCE(LastOccurence, DateChangementEtat, SYSDATETIMEOFFSET())
FROM AlerteCapteurs
WHERE EstEnAlerte = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpisodesAlerteCapteur");
        }
    }
}
