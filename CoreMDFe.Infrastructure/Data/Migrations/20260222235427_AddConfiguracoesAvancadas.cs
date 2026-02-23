using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreMDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracoesAvancadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GerarQrCode",
                table: "Configuracoes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModalidadePadrao",
                table: "Configuracoes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RespTecCnpj",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RespTecEmail",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RespTecNome",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RespTecTelefone",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TipoEmissaoPadrao",
                table: "Configuracoes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoEmitentePadrao",
                table: "Configuracoes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoTransportadorPadrao",
                table: "Configuracoes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GerarQrCode",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "ModalidadePadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "RespTecCnpj",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "RespTecEmail",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "RespTecNome",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "RespTecTelefone",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "TipoEmissaoPadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "TipoEmitentePadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "TipoTransportadorPadrao",
                table: "Configuracoes");
        }
    }
}
