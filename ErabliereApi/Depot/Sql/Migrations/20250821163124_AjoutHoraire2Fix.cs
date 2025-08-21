using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AjoutHoraire2Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Horaires_Erabliere_ErabliereId",
                table: "Horaires");

            migrationBuilder.DropIndex(
                name: "IX_Horaires_ErabliereId",
                table: "Horaires");

            migrationBuilder.DropColumn(
                name: "ErabliereId",
                table: "Horaires");

            migrationBuilder.CreateIndex(
                name: "IX_Horaires_IdErabliere",
                table: "Horaires",
                column: "IdErabliere");

            migrationBuilder.AddForeignKey(
                name: "FK_Horaires_Erabliere_IdErabliere",
                table: "Horaires",
                column: "IdErabliere",
                principalTable: "Erabliere",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Horaires_Erabliere_IdErabliere",
                table: "Horaires");

            migrationBuilder.DropIndex(
                name: "IX_Horaires_IdErabliere",
                table: "Horaires");

            migrationBuilder.AddColumn<Guid>(
                name: "ErabliereId",
                table: "Horaires",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Horaires_ErabliereId",
                table: "Horaires",
                column: "ErabliereId");

            migrationBuilder.AddForeignKey(
                name: "FK_Horaires_Erabliere_ErabliereId",
                table: "Horaires",
                column: "ErabliereId",
                principalTable: "Erabliere",
                principalColumn: "Id");
        }
    }
}
