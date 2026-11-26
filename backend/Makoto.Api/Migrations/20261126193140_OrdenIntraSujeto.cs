using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Makoto.Api.Migrations
{
    /// <inheritdoc />
    public partial class OrdenIntraSujeto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "fila_orden",
                table: "sesiones_experimentales",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int[]>(
                name: "orden_condiciones",
                table: "sesiones_experimentales",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<int>(
                name: "pistas_escuchadas",
                table: "sesiones_experimentales",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_experimentales_fila_orden_estado",
                table: "sesiones_experimentales",
                columns: new[] { "fila_orden", "estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sesiones_experimentales_fila_orden_estado",
                table: "sesiones_experimentales");

            migrationBuilder.DropColumn(
                name: "fila_orden",
                table: "sesiones_experimentales");

            migrationBuilder.DropColumn(
                name: "orden_condiciones",
                table: "sesiones_experimentales");

            migrationBuilder.DropColumn(
                name: "pistas_escuchadas",
                table: "sesiones_experimentales");
        }
    }
}
