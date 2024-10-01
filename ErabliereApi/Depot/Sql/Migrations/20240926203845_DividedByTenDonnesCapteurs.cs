using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depot.Sql.Migrations
{
    /// <inheritdoc />
    public partial class DividedByTenDonnesCapteurs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "DisplayTop",
                table: "Capteurs",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayType",
                table: "Capteurs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinVaue",
                table: "AlerteCapteurs",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxValue",
                table: "AlerteCapteurs",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            // For each non null data in DonneesCapteur, divide by 10
            migrationBuilder.Sql("UPDATE DonneesCapteur SET Valeur = Valeur / 10 WHERE Valeur IS NOT NULL");

            // For each non null data in AlerteCapteurs, divide by 10
            migrationBuilder.Sql("UPDATE AlerteCapteurs SET MinVaue = MinVaue / 10 WHERE MinVaue IS NOT NULL");
            migrationBuilder.Sql("UPDATE AlerteCapteurs SET MaxValue = MaxValue / 10 WHERE MaxValue IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayTop",
                table: "Capteurs");

            migrationBuilder.DropColumn(
                name: "DisplayType",
                table: "Capteurs");

            // For each non null data in DonneesCapteur, multiply by 10
            migrationBuilder.Sql("UPDATE DonneesCapteur SET Valeur = Valeur * 10 WHERE Valeur IS NOT NULL");

            // For each non null data in AlerteCapteurs, multiply by 10
            migrationBuilder.Sql("UPDATE AlerteCapteurs SET MinVaue = MinVaue * 10 WHERE MinVaue IS NOT NULL");
            migrationBuilder.Sql("UPDATE AlerteCapteurs SET MaxVaue = MaxValue * 10 WHERE MaxValue IS NOT NULL");

            migrationBuilder.AlterColumn<short>(
                name: "MinVaue",
                table: "AlerteCapteurs",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "MaxValue",
                table: "AlerteCapteurs",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);
        }
    }
}
