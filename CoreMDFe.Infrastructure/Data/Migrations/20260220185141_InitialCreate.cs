using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreMDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Condutores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Cpf = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Condutores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Cnpj = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    InscricaoEstadual = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    NomeFantasia = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Logradouro = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Complemento = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Bairro = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CodigoIbgeMunicipio = table.Column<long>(type: "INTEGER", nullable: false),
                    NomeMunicipio = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Cep = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    SiglaUf = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    RNTRC = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Veiculos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Placa = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Renavam = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    UfLicenciamento = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    TaraKg = table.Column<int>(type: "INTEGER", nullable: false),
                    CapacidadeKg = table.Column<int>(type: "INTEGER", nullable: false),
                    CapacidadeM3 = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoVeiculo = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoRodado = table.Column<string>(type: "TEXT", nullable: false),
                    TipoCarroceria = table.Column<string>(type: "TEXT", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veiculos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configuracoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DiretorioSalvarXml = table.Column<string>(type: "TEXT", nullable: false),
                    CaminhoSchemas = table.Column<string>(type: "TEXT", nullable: false),
                    IsSalvarXml = table.Column<bool>(type: "INTEGER", nullable: false),
                    TipoAmbiente = table.Column<int>(type: "INTEGER", nullable: false),
                    UfEmitente = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    VersaoLayout = table.Column<int>(type: "INTEGER", nullable: false),
                    Serie = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimaNumeracao = table.Column<long>(type: "INTEGER", nullable: false),
                    TimeOut = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5000),
                    NumeroSerieCertificado = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CaminhoArquivoCertificado = table.Column<string>(type: "TEXT", nullable: false),
                    SenhaCertificado = table.Column<string>(type: "TEXT", nullable: false),
                    ManterCertificadoEmCache = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Configuracoes_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Manifestos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Serie = table.Column<int>(type: "INTEGER", nullable: false),
                    Modelo = table.Column<int>(type: "INTEGER", nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UfOrigem = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    UfDestino = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    Modalidade = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoEmissao = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoAmbiente = table.Column<int>(type: "INTEGER", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    ProtocoloAutorizacao = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    ProtocoloEncerramento = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    ProtocoloCancelamento = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    Recibo = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    CodigoStatus = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    MotivoStatus = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    XmlAssinado = table.Column<string>(type: "TEXT", nullable: false),
                    XmlAutorizado = table.Column<string>(type: "TEXT", nullable: false),
                    XmlEncerramento = table.Column<string>(type: "TEXT", nullable: false),
                    XmlCancelamento = table.Column<string>(type: "TEXT", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manifestos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Manifestos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Configuracoes_EmpresaId",
                table: "Configuracoes",
                column: "EmpresaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Manifestos_EmpresaId",
                table: "Manifestos",
                column: "EmpresaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Condutores");

            migrationBuilder.DropTable(
                name: "Configuracoes");

            migrationBuilder.DropTable(
                name: "Manifestos");

            migrationBuilder.DropTable(
                name: "Veiculos");

            migrationBuilder.DropTable(
                name: "Empresas");
        }
    }
}
