using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AjoutAppareilsInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appareil_Erabliere_ErabliereId",
                table: "Appareil");

            migrationBuilder.DropForeignKey(
                name: "FK_Capteurs_Appareil_AppareilId",
                table: "Capteurs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Appareil",
                table: "Appareil");

            migrationBuilder.RenameTable(
                name: "Appareil",
                newName: "Appareils");

            migrationBuilder.RenameIndex(
                name: "IX_Appareil_ErabliereId",
                table: "Appareils",
                newName: "IX_Appareils_ErabliereId");

            migrationBuilder.AddColumn<Guid>(
                name: "StatutId",
                table: "Appareils",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TempsId",
                table: "Appareils",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Appareils",
                table: "Appareils",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AppareilAdresse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Addr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Addrtype = table.Column<int>(type: "int", nullable: false),
                    Vendeur = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdAppareil = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppareilId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppareilAdresse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppareilAdresse_Appareils_AppareilId",
                        column: x => x.AppareilId,
                        principalTable: "Appareils",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppareilStatut",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Etat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Raison = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RaisonTTL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdAppareil = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppareilStatut", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppareilTempsInfo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateDebut = table.Column<long>(type: "bigint", nullable: false),
                    DateFin = table.Column<long>(type: "bigint", nullable: false),
                    Strtt = table.Column<long>(type: "bigint", nullable: false),
                    Rttvar = table.Column<long>(type: "bigint", nullable: false),
                    To = table.Column<long>(type: "bigint", nullable: false),
                    IdAppareil = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppareilTempsInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NomHostAppareil",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdAppareil = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppareilId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NomHostAppareil", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NomHostAppareil_Appareils_AppareilId",
                        column: x => x.AppareilId,
                        principalTable: "Appareils",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PortEtat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Etat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Raison = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RaisonTTL = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdPortAppareil = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortEtat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortService",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Produit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtraInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Methode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CPEs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdPortEtat = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortService", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortAppareil",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Protocole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EtatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PortServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdAppareil = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppareilId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortAppareil", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortAppareil_Appareils_AppareilId",
                        column: x => x.AppareilId,
                        principalTable: "Appareils",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PortAppareil_PortEtat_EtatId",
                        column: x => x.EtatId,
                        principalTable: "PortEtat",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PortAppareil_PortService_PortServiceId",
                        column: x => x.PortServiceId,
                        principalTable: "PortService",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appareils_StatutId",
                table: "Appareils",
                column: "StatutId");

            migrationBuilder.CreateIndex(
                name: "IX_Appareils_TempsId",
                table: "Appareils",
                column: "TempsId");

            migrationBuilder.CreateIndex(
                name: "IX_AppareilAdresse_AppareilId",
                table: "AppareilAdresse",
                column: "AppareilId");

            migrationBuilder.CreateIndex(
                name: "IX_NomHostAppareil_AppareilId",
                table: "NomHostAppareil",
                column: "AppareilId");

            migrationBuilder.CreateIndex(
                name: "IX_PortAppareil_AppareilId",
                table: "PortAppareil",
                column: "AppareilId");

            migrationBuilder.CreateIndex(
                name: "IX_PortAppareil_EtatId",
                table: "PortAppareil",
                column: "EtatId");

            migrationBuilder.CreateIndex(
                name: "IX_PortAppareil_PortServiceId",
                table: "PortAppareil",
                column: "PortServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appareils_AppareilStatut_StatutId",
                table: "Appareils",
                column: "StatutId",
                principalTable: "AppareilStatut",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appareils_AppareilTempsInfo_TempsId",
                table: "Appareils",
                column: "TempsId",
                principalTable: "AppareilTempsInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Appareils_Erabliere_ErabliereId",
                table: "Appareils",
                column: "ErabliereId",
                principalTable: "Erabliere",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Capteurs_Appareils_AppareilId",
                table: "Capteurs",
                column: "AppareilId",
                principalTable: "Appareils",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appareils_AppareilStatut_StatutId",
                table: "Appareils");

            migrationBuilder.DropForeignKey(
                name: "FK_Appareils_AppareilTempsInfo_TempsId",
                table: "Appareils");

            migrationBuilder.DropForeignKey(
                name: "FK_Appareils_Erabliere_ErabliereId",
                table: "Appareils");

            migrationBuilder.DropForeignKey(
                name: "FK_Capteurs_Appareils_AppareilId",
                table: "Capteurs");

            migrationBuilder.DropTable(
                name: "AppareilAdresse");

            migrationBuilder.DropTable(
                name: "AppareilStatut");

            migrationBuilder.DropTable(
                name: "AppareilTempsInfo");

            migrationBuilder.DropTable(
                name: "NomHostAppareil");

            migrationBuilder.DropTable(
                name: "PortAppareil");

            migrationBuilder.DropTable(
                name: "PortEtat");

            migrationBuilder.DropTable(
                name: "PortService");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Appareils",
                table: "Appareils");

            migrationBuilder.DropIndex(
                name: "IX_Appareils_StatutId",
                table: "Appareils");

            migrationBuilder.DropIndex(
                name: "IX_Appareils_TempsId",
                table: "Appareils");

            migrationBuilder.DropColumn(
                name: "StatutId",
                table: "Appareils");

            migrationBuilder.DropColumn(
                name: "TempsId",
                table: "Appareils");

            migrationBuilder.RenameTable(
                name: "Appareils",
                newName: "Appareil");

            migrationBuilder.RenameIndex(
                name: "IX_Appareils_ErabliereId",
                table: "Appareil",
                newName: "IX_Appareil_ErabliereId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Appareil",
                table: "Appareil",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appareil_Erabliere_ErabliereId",
                table: "Appareil",
                column: "ErabliereId",
                principalTable: "Erabliere",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Capteurs_Appareil_AppareilId",
                table: "Capteurs",
                column: "AppareilId",
                principalTable: "Appareil",
                principalColumn: "Id");
        }
    }
}
