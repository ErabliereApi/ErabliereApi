using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class TablesRapports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rapport_Erabliere_ErabliereId",
                table: "Rapport");

            migrationBuilder.DropForeignKey(
                name: "FK_RapportDonnees_Rapport_OwnerId",
                table: "RapportDonnees");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RapportDonnees",
                table: "RapportDonnees");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rapport",
                table: "Rapport");

            migrationBuilder.RenameTable(
                name: "RapportDonnees",
                newName: "DonneesRapports");

            migrationBuilder.RenameTable(
                name: "Rapport",
                newName: "Rapports");

            migrationBuilder.RenameIndex(
                name: "IX_RapportDonnees_OwnerId",
                table: "DonneesRapports",
                newName: "IX_DonneesRapports_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Rapport_ErabliereId",
                table: "Rapports",
                newName: "IX_Rapports_ErabliereId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DonneesRapports",
                table: "DonneesRapports",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rapports",
                table: "Rapports",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DonneesRapports_Rapports_OwnerId",
                table: "DonneesRapports",
                column: "OwnerId",
                principalTable: "Rapports",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rapports_Erabliere_ErabliereId",
                table: "Rapports",
                column: "ErabliereId",
                principalTable: "Erabliere",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonneesRapports_Rapports_OwnerId",
                table: "DonneesRapports");

            migrationBuilder.DropForeignKey(
                name: "FK_Rapports_Erabliere_ErabliereId",
                table: "Rapports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rapports",
                table: "Rapports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DonneesRapports",
                table: "DonneesRapports");

            migrationBuilder.RenameTable(
                name: "Rapports",
                newName: "Rapport");

            migrationBuilder.RenameTable(
                name: "DonneesRapports",
                newName: "RapportDonnees");

            migrationBuilder.RenameIndex(
                name: "IX_Rapports_ErabliereId",
                table: "Rapport",
                newName: "IX_Rapport_ErabliereId");

            migrationBuilder.RenameIndex(
                name: "IX_DonneesRapports_OwnerId",
                table: "RapportDonnees",
                newName: "IX_RapportDonnees_OwnerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rapport",
                table: "Rapport",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RapportDonnees",
                table: "RapportDonnees",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rapport_Erabliere_ErabliereId",
                table: "Rapport",
                column: "ErabliereId",
                principalTable: "Erabliere",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RapportDonnees_Rapport_OwnerId",
                table: "RapportDonnees",
                column: "OwnerId",
                principalTable: "Rapport",
                principalColumn: "Id");
        }
    }
}
