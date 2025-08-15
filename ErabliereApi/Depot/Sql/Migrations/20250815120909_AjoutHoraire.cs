using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AjoutHoraire : Migration
    {
        /// <summary>
        /// Type de données pour les colonnes de type horaire
        /// </summary>
        public const string Nvchar12 = "nvarchar(12)";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Horaire",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lundi = table.Column<string>(type: Nvchar12, maxLength: 12, nullable: true),
                    Mardi = table.Column<string>(type: Nvchar12, maxLength: 12, nullable: true),
                    Mercredi = table.Column<string>(type: Nvchar12, maxLength: 12, nullable: true),
                    Jeudi = table.Column<string>(type: Nvchar12, maxLength: 12, nullable: true),
                    Vendredi = table.Column<string>(type: Nvchar12, maxLength: 12, nullable: true),
                    Samedi = table.Column<string>(type: Nvchar12, maxLength: 12, nullable: true),
                    Dimanche = table.Column<string>(type: Nvchar12, maxLength: 12, nullable: true),
                    IdErabliere = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErabliereId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horaire", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Horaire_Erabliere_ErabliereId",
                        column: x => x.ErabliereId,
                        principalTable: "Erabliere",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Horaire_ErabliereId",
                table: "Horaire",
                column: "ErabliereId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Horaire");
        }
    }
}
