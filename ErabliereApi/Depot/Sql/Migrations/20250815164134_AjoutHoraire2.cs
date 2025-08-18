using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AjoutHoraire2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Horaire_Erabliere_ErabliereId",
                table: "Horaire");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Horaire",
                table: "Horaire");

            migrationBuilder.RenameTable(
                name: "Horaire",
                newName: "Horaires");

            migrationBuilder.RenameIndex(
                name: "IX_Horaire_ErabliereId",
                table: "Horaires",
                newName: "IX_Horaires_ErabliereId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Horaires",
                table: "Horaires",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Horaires_Erabliere_ErabliereId",
                table: "Horaires",
                column: "ErabliereId",
                principalTable: "Erabliere",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Horaires_Erabliere_ErabliereId",
                table: "Horaires");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Horaires",
                table: "Horaires");

            migrationBuilder.RenameTable(
                name: "Horaires",
                newName: "Horaire");

            migrationBuilder.RenameIndex(
                name: "IX_Horaires_ErabliereId",
                table: "Horaire",
                newName: "IX_Horaire_ErabliereId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Horaire",
                table: "Horaire",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Horaire_Erabliere_ErabliereId",
                table: "Horaire",
                column: "ErabliereId",
                principalTable: "Erabliere",
                principalColumn: "Id");
        }
    }
}
