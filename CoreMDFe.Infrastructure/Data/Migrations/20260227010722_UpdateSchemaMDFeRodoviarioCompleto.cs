using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreMDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchemaMDFeRodoviarioCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProprietarioCpfCnpj",
                table: "Veiculos",
                type: "TEXT",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProprietarioIE",
                table: "Veiculos",
                type: "TEXT",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProprietarioNome",
                table: "Veiculos",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProprietarioRNTRC",
                table: "Veiculos",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProprietarioTipo",
                table: "Veiculos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProprietarioUF",
                table: "Veiculos",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CodigoUnidadePeso",
                table: "Manifestos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataHoraInicioViagem",
                table: "Manifestos",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IndicadorCarregamentoPosterior",
                table: "Manifestos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "InformacoesComplementares",
                table: "Manifestos",
                type: "TEXT",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InformacoesFisco",
                table: "Manifestos",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PesoTotalCarga",
                table: "Manifestos",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProdutoEAN",
                table: "Manifestos",
                type: "TEXT",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProdutoNCM",
                table: "Manifestos",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProdutoNome",
                table: "Manifestos",
                type: "TEXT",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProdutoTipoCarga",
                table: "Manifestos",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "QtdCTe",
                table: "Manifestos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QtdMDFe",
                table: "Manifestos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QtdNFe",
                table: "Manifestos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoEmitente",
                table: "Manifestos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoTransportador",
                table: "Manifestos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorTotalCarga",
                table: "Manifestos",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ManifestoAutorizadosDownload",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManifestoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CpfCnpj = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestoAutorizadosDownload", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestoAutorizadosDownload_Manifestos_ManifestoId",
                        column: x => x.ManifestoId,
                        principalTable: "Manifestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManifestoCiots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManifestoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ciot = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    CpfCnpj = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestoCiots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestoCiots_Manifestos_ManifestoId",
                        column: x => x.ManifestoId,
                        principalTable: "Manifestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManifestoCondutores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManifestoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CondutorBaseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Cpf = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestoCondutores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestoCondutores_Manifestos_ManifestoId",
                        column: x => x.ManifestoId,
                        principalTable: "Manifestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManifestoContratantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManifestoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CpfCnpj = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestoContratantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestoContratantes_Manifestos_ManifestoId",
                        column: x => x.ManifestoId,
                        principalTable: "Manifestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManifestoMunicipiosCarregamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManifestoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodigoIbge = table.Column<long>(type: "INTEGER", nullable: false),
                    NomeMunicipio = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestoMunicipiosCarregamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestoMunicipiosCarregamento_Manifestos_ManifestoId",
                        column: x => x.ManifestoId,
                        principalTable: "Manifestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManifestoMunicipiosDescarregamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManifestoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodigoIbge = table.Column<long>(type: "INTEGER", nullable: false),
                    NomeMunicipio = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestoMunicipiosDescarregamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestoMunicipiosDescarregamento_Manifestos_ManifestoId",
                        column: x => x.ManifestoId,
                        principalTable: "Manifestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManifestoPagamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManifestoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NomeContratante = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CpfCnpjContratante = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    ValorTotalViagem = table.Column<decimal>(type: "TEXT", nullable: false),
                    ValorAdiantamento = table.Column<decimal>(type: "TEXT", nullable: false),
                    IndicadorPagamento = table.Column<int>(type: "INTEGER", nullable: false),
                    CnpjInstituicaoPagamento = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    ChavePix = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestoPagamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestoPagamentos_Manifestos_ManifestoId",
                        column: x => x.ManifestoId,
                        principalTable: "Manifestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManifestoPercursos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManifestoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false),
                    UF = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestoPercursos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestoPercursos_Manifestos_ManifestoId",
                        column: x => x.ManifestoId,
                        principalTable: "Manifestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManifestoSeguros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManifestoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Responsavel = table.Column<int>(type: "INTEGER", nullable: false),
                    CpfCnpjResponsavel = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    NomeSeguradora = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CnpjSeguradora = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    NumeroApolice = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NumeroAverbacao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestoSeguros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestoSeguros_Manifestos_ManifestoId",
                        column: x => x.ManifestoId,
                        principalTable: "Manifestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManifestoValesPedagio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManifestoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CnpjFornecedor = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    CpfCnpjPagador = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    NumeroCompra = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestoValesPedagio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestoValesPedagio_Manifestos_ManifestoId",
                        column: x => x.ManifestoId,
                        principalTable: "Manifestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManifestoVeiculos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManifestoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VeiculoBaseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Placa = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Renavam = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    TaraKg = table.Column<int>(type: "INTEGER", nullable: false),
                    CapacidadeKg = table.Column<int>(type: "INTEGER", nullable: false),
                    CapacidadeM3 = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoRodado = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    TipoCarroceria = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    UfLicenciamento = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    PropCpfCnpj = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    PropNome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    PropRNTRC = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    PropIE = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    PropUF = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    PropTipo = table.Column<int>(type: "INTEGER", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestoVeiculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestoVeiculos_Manifestos_ManifestoId",
                        column: x => x.ManifestoId,
                        principalTable: "Manifestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManifestoDocumentosFiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MunicipioDescarregamentoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TipoDocumento = table.Column<int>(type: "INTEGER", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    SegundoCodigoBarra = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    IndicadorReentrega = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestoDocumentosFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManifestoDocumentosFiscais_ManifestoMunicipiosDescarregamento_MunicipioDescarregamentoId",
                        column: x => x.MunicipioDescarregamentoId,
                        principalTable: "ManifestoMunicipiosDescarregamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManifestoAutorizadosDownload_ManifestoId",
                table: "ManifestoAutorizadosDownload",
                column: "ManifestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ManifestoCiots_ManifestoId",
                table: "ManifestoCiots",
                column: "ManifestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ManifestoCondutores_ManifestoId",
                table: "ManifestoCondutores",
                column: "ManifestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ManifestoContratantes_ManifestoId",
                table: "ManifestoContratantes",
                column: "ManifestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ManifestoDocumentosFiscais_MunicipioDescarregamentoId",
                table: "ManifestoDocumentosFiscais",
                column: "MunicipioDescarregamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ManifestoMunicipiosCarregamento_ManifestoId",
                table: "ManifestoMunicipiosCarregamento",
                column: "ManifestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ManifestoMunicipiosDescarregamento_ManifestoId",
                table: "ManifestoMunicipiosDescarregamento",
                column: "ManifestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ManifestoPagamentos_ManifestoId",
                table: "ManifestoPagamentos",
                column: "ManifestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ManifestoPercursos_ManifestoId",
                table: "ManifestoPercursos",
                column: "ManifestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ManifestoSeguros_ManifestoId",
                table: "ManifestoSeguros",
                column: "ManifestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ManifestoValesPedagio_ManifestoId",
                table: "ManifestoValesPedagio",
                column: "ManifestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ManifestoVeiculos_ManifestoId",
                table: "ManifestoVeiculos",
                column: "ManifestoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManifestoAutorizadosDownload");

            migrationBuilder.DropTable(
                name: "ManifestoCiots");

            migrationBuilder.DropTable(
                name: "ManifestoCondutores");

            migrationBuilder.DropTable(
                name: "ManifestoContratantes");

            migrationBuilder.DropTable(
                name: "ManifestoDocumentosFiscais");

            migrationBuilder.DropTable(
                name: "ManifestoMunicipiosCarregamento");

            migrationBuilder.DropTable(
                name: "ManifestoPagamentos");

            migrationBuilder.DropTable(
                name: "ManifestoPercursos");

            migrationBuilder.DropTable(
                name: "ManifestoSeguros");

            migrationBuilder.DropTable(
                name: "ManifestoValesPedagio");

            migrationBuilder.DropTable(
                name: "ManifestoVeiculos");

            migrationBuilder.DropTable(
                name: "ManifestoMunicipiosDescarregamento");

            migrationBuilder.DropColumn(
                name: "ProprietarioCpfCnpj",
                table: "Veiculos");

            migrationBuilder.DropColumn(
                name: "ProprietarioIE",
                table: "Veiculos");

            migrationBuilder.DropColumn(
                name: "ProprietarioNome",
                table: "Veiculos");

            migrationBuilder.DropColumn(
                name: "ProprietarioRNTRC",
                table: "Veiculos");

            migrationBuilder.DropColumn(
                name: "ProprietarioTipo",
                table: "Veiculos");

            migrationBuilder.DropColumn(
                name: "ProprietarioUF",
                table: "Veiculos");

            migrationBuilder.DropColumn(
                name: "CodigoUnidadePeso",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "DataHoraInicioViagem",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "IndicadorCarregamentoPosterior",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "InformacoesComplementares",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "InformacoesFisco",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "PesoTotalCarga",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "ProdutoEAN",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "ProdutoNCM",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "ProdutoNome",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "ProdutoTipoCarga",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "QtdCTe",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "QtdMDFe",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "QtdNFe",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "TipoEmitente",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "TipoTransportador",
                table: "Manifestos");

            migrationBuilder.DropColumn(
                name: "ValorTotalCarga",
                table: "Manifestos");
        }
    }
}
