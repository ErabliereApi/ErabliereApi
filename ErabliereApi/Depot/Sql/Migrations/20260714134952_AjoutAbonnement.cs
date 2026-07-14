using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AjoutAbonnement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Abonnements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Plan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateDebut = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DateFin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DC = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DM = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abonnements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Abonnements_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Abonnements_CustomerId",
                table: "Abonnements",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Abonnements_StripeSubscriptionId",
                table: "Abonnements",
                column: "StripeSubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Abonnements");
        }
    }
}
