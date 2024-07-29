using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class RapportEtInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Conversations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastOccurence",
                table: "Alertes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastOccurence",
                table: "AlerteCapteurs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Inspection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdErabliere = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErabliereId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Titre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inspection_Erabliere_ErabliereId",
                        column: x => x.ErabliereId,
                        principalTable: "Erabliere",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Rapport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdErabliere = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErabliereId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestParameters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rapport", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rapport_Erabliere_ErabliereId",
                        column: x => x.ErabliereId,
                        principalTable: "Erabliere",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InspectionDonnees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Titre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImgUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImgBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionDonnees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionDonnees_Inspection_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Inspection",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RapportDonnees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Moyenne = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Somme = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RapportDonnees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RapportDonnees_Rapport_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Rapport",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_ErabliereId",
                table: "Inspection",
                column: "ErabliereId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionDonnees_OwnerId",
                table: "InspectionDonnees",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Rapport_ErabliereId",
                table: "Rapport",
                column: "ErabliereId");

            migrationBuilder.CreateIndex(
                name: "IX_RapportDonnees_OwnerId",
                table: "RapportDonnees",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspectionDonnees");

            migrationBuilder.DropTable(
                name: "RapportDonnees");

            migrationBuilder.DropTable(
                name: "Inspection");

            migrationBuilder.DropTable(
                name: "Rapport");

            migrationBuilder.DropColumn(
                name: "LastOccurence",
                table: "Alertes");

            migrationBuilder.DropColumn(
                name: "LastOccurence",
                table: "AlerteCapteurs");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
