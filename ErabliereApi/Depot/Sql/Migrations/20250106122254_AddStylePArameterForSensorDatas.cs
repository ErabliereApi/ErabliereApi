using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddStylePArameterForSensorDatas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CapteurStyles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BackgroundColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BorderColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fill = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PointBackgroundColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PointBorderColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tension = table.Column<double>(type: "float", nullable: true),
                    DSetBorderColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UseGradient = table.Column<bool>(type: "bit", nullable: false),
                    G1Stop = table.Column<double>(type: "float", nullable: true),
                    G2Stop = table.Column<double>(type: "float", nullable: true),
                    G3Stop = table.Column<double>(type: "float", nullable: true),
                    G1Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    G2Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    G3Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdCapteur = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapteurStyles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapteurStyles_Capteurs_IdCapteur",
                        column: x => x.IdCapteur,
                        principalTable: "Capteurs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CapteurStyles_IdCapteur",
                table: "CapteurStyles",
                column: "IdCapteur",
                unique: true,
                filter: "[IdCapteur] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapteurStyles");
        }
    }
}
