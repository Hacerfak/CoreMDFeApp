using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreMDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaPadroesEmissao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CondutorPadraoId",
                table: "Configuracoes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InfoComplementarPadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InfoFiscoPadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PagamentoCnpjInstituicaoPadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PagamentoCpfCnpjContratantePadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PagamentoIndicadorPadrao",
                table: "Configuracoes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PagamentoNomeContratantePadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProdutoEANPadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProdutoNCMPadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProdutoNomePadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProdutoTipoCargaPadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SeguroApolicePadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SeguroCnpjSeguradoraPadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SeguroCpfCnpjPadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SeguroNomeSeguradoraPadrao",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SeguroResponsavelPadrao",
                table: "Configuracoes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "VeiculoPadraoId",
                table: "Configuracoes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CondutorPadraoId",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "InfoComplementarPadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "InfoFiscoPadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "PagamentoCnpjInstituicaoPadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "PagamentoCpfCnpjContratantePadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "PagamentoIndicadorPadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "PagamentoNomeContratantePadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "ProdutoEANPadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "ProdutoNCMPadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "ProdutoNomePadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "ProdutoTipoCargaPadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "SeguroApolicePadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "SeguroCnpjSeguradoraPadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "SeguroCpfCnpjPadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "SeguroNomeSeguradoraPadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "SeguroResponsavelPadrao",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "VeiculoPadraoId",
                table: "Configuracoes");
        }
    }
}
