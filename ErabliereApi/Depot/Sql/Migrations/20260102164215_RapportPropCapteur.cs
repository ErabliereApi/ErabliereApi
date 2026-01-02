using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class RapportPropCapteur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CapteurId",
                table: "Rapports",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rapports_CapteurId",
                table: "Rapports",
                column: "CapteurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rapports_Capteurs_CapteurId",
                table: "Rapports",
                column: "CapteurId",
                principalTable: "Capteurs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rapports_Capteurs_CapteurId",
                table: "Rapports");

            migrationBuilder.DropIndex(
                name: "IX_Rapports_CapteurId",
                table: "Rapports");

            migrationBuilder.DropColumn(
                name: "CapteurId",
                table: "Rapports");
        }
    }
}
