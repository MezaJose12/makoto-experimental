using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Makoto.Api.Migrations
{
    /// <inheritdoc />
    public partial class ModeloInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "administradores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    intentos_fallidos = table.Column<int>(type: "integer", nullable: false),
                    bloqueado_hasta_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administradores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "condiciones_vocales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    genero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nivel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    f0_objetivo_hz = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    f0_medido_hz = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    duracion_segundos = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    archivo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    voz_tts = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_condiciones_vocales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instrumento_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    instrumento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    texto_es = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    factor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    escala_min = table.Column<short>(type: "smallint", nullable: false),
                    escala_max = table.Column<short>(type: "smallint", nullable: false),
                    invertido = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instrumento_items", x => x.id);
                    table.CheckConstraint("ck_instrumento_items_escala", "escala_min < escala_max");
                });

            migrationBuilder.CreateTable(
                name: "sesiones_experimentales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_participante = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    condicion_vocal_id = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    consentimiento_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    consentimiento_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reservada_hasta_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    inicio_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fin_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesiones_experimentales", x => x.id);
                    table.ForeignKey(
                        name: "FK_sesiones_experimentales_condiciones_vocales_condicion_vocal~",
                        column: x => x.condicion_vocal_id,
                        principalTable: "condiciones_vocales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "datos_participante",
                columns: table => new
                {
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rango_edad = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    genero = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    experiencia_asistentes_voz = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_datos_participante", x => x.sesion_id);
                    table.ForeignKey(
                        name: "FK_datos_participante_sesiones_experimentales_sesion_id",
                        column: x => x.sesion_id,
                        principalTable: "sesiones_experimentales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    genero_percibido = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tono_percibido = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    naturalidad = table.Column<short>(type: "smallint", nullable: false),
                    enviada_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluaciones", x => x.id);
                    table.CheckConstraint("ck_evaluaciones_naturalidad", "naturalidad BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_evaluaciones_sesiones_experimentales_sesion_id",
                        column: x => x.sesion_id,
                        principalTable: "sesiones_experimentales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eventos_telemetria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    sesion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ocurrido_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    latencia_ms = table.Column<int>(type: "integer", nullable: true),
                    metadatos = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_telemetria", x => x.id);
                    table.ForeignKey(
                        name: "FK_eventos_telemetria_sesiones_experimentales_sesion_id",
                        column: x => x.sesion_id,
                        principalTable: "sesiones_experimentales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "respuestas_item",
                columns: table => new
                {
                    evaluacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_respuestas_item", x => new { x.evaluacion_id, x.item_id });
                    table.ForeignKey(
                        name: "FK_respuestas_item_evaluaciones_evaluacion_id",
                        column: x => x.evaluacion_id,
                        principalTable: "evaluaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_respuestas_item_instrumento_items_item_id",
                        column: x => x.item_id,
                        principalTable: "instrumento_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "condiciones_vocales",
                columns: new[] { "id", "archivo", "codigo", "duracion_segundos", "f0_medido_hz", "f0_objetivo_hz", "genero", "nivel", "sha256", "voz_tts" },
                values: new object[,]
                {
                    { 1, "f_grave.mp3", "F_GRAVE", null, null, 175m, "Femenino", "Grave", null, null },
                    { 2, "f_neutra.mp3", "F_NEUTRA", null, null, 210m, "Femenino", "Neutra", null, null },
                    { 3, "f_aguda.mp3", "F_AGUDA", null, null, 255m, "Femenino", "Aguda", null, null },
                    { 4, "m_grave.mp3", "M_GRAVE", null, null, 100m, "Masculino", "Grave", null, null },
                    { 5, "m_neutra.mp3", "M_NEUTRA", null, null, 118m, "Masculino", "Neutra", null, null },
                    { 6, "m_aguda.mp3", "M_AGUDA", null, null, 138m, "Masculino", "Aguda", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_administradores_usuario",
                table: "administradores",
                column: "usuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_condiciones_vocales_codigo",
                table: "condiciones_vocales",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evaluaciones_sesion_id",
                table: "evaluaciones",
                column: "sesion_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_eventos_telemetria_sesion_id_tipo",
                table: "eventos_telemetria",
                columns: new[] { "sesion_id", "tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_instrumento_items_codigo",
                table: "instrumento_items",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_respuestas_item_item_id",
                table: "respuestas_item",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_experimentales_codigo_participante",
                table: "sesiones_experimentales",
                column: "codigo_participante",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_experimentales_condicion_vocal_id_estado",
                table: "sesiones_experimentales",
                columns: new[] { "condicion_vocal_id", "estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "administradores");

            migrationBuilder.DropTable(
                name: "datos_participante");

            migrationBuilder.DropTable(
                name: "eventos_telemetria");

            migrationBuilder.DropTable(
                name: "respuestas_item");

            migrationBuilder.DropTable(
                name: "evaluaciones");

            migrationBuilder.DropTable(
                name: "instrumento_items");

            migrationBuilder.DropTable(
                name: "sesiones_experimentales");

            migrationBuilder.DropTable(
                name: "condiciones_vocales");
        }
    }
}
