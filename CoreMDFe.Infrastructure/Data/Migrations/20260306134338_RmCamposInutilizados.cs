using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreMDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RmCamposInutilizados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GerarQrCode",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "ManterCertificadoEmCache",
                table: "Configuracoes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GerarQrCode",
                table: "Configuracoes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ManterCertificadoEmCache",
                table: "Configuracoes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
