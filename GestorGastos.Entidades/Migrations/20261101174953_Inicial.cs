using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorGastos.Entidades.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comprobantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(36)", nullable: false),
                    NombreArchivoOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BlobNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TamanioBytes = table.Column<long>(type: "bigint", nullable: false),
                    TipoDocumento = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Comercio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FechaComprobante = table.Column<DateOnly>(type: "date", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Moneda = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: true),
                    ConfianzaDescripcion = table.Column<double>(type: "float", nullable: true),
                    ConfianzaMonto = table.Column<double>(type: "float", nullable: true),
                    EditadoManualmente = table.Column<bool>(type: "bit", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MensajeError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comprobantes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialEstados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComprobanteId = table.Column<int>(type: "int", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EstadoNuevo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialEstados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialEstados_Comprobantes_ComprobanteId",
                        column: x => x.ComprobanteId,
                        principalTable: "Comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorialEstados_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_BlobNombre",
                table: "Comprobantes",
                column: "BlobNombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_FechaCarga",
                table: "Comprobantes",
                column: "FechaCarga");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_UsuarioId_Estado",
                table: "Comprobantes",
                columns: new[] { "UsuarioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstados_ComprobanteId",
                table: "HistorialEstados",
                column: "ComprobanteId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstados_UsuarioId",
                table: "HistorialEstados",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialEstados");

            migrationBuilder.DropTable(
                name: "Comprobantes");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
