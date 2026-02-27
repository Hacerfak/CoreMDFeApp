using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreMDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemocaoRespTecn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
