using DFe.Classes.Flags;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using MDFe.Classes.Flags;
using MDFe.Classes.Informacoes;
using MDFe.Servicos.RecepcaoMDFe;
using MDFeEletronico = MDFe.Classes.Informacoes.MDFe;
using DFe.Utils;
using DFe.Classes.Entidades;

namespace CoreMDFe.Application.Features.Manifestos
{
    // DTO que trafega as notas lidas na UI para o motor de emissão
    public class DocumentoMDFeDto
    {
        public string Chave { get; set; } = string.Empty;
        public int Tipo { get; set; } // 55 = NFe, 57 = CTe
        public decimal Valor { get; set; }
        public decimal Peso { get; set; }
        public long IbgeCarregamento { get; set; }
        public string MunicipioCarregamento { get; set; } = string.Empty;
        public string UfCarregamento { get; set; } = string.Empty;
        public long IbgeDescarga { get; set; }
        public string MunicipioDescarga { get; set; } = string.Empty;
        public string UfDescarga { get; set; } = string.Empty;
        public override string ToString() => $"{(Tipo == 55 ? "NF-e" : "CT-e")} - {Chave} | Rota: {UfCarregamento} -> {UfDescarga}";
    }

    // O Comando carrega TODA a carga que a UI coletou (Obrigatórios e Opcionais)
    public record EmitirManifestoCommand(
        Guid EmpresaId,
        List<DocumentoMDFeDto> Documentos,
        string UfCarregamento, string UfDescarregamento,
        int TipoEmitente, int TipoTransportador, int Modal,
        string UfsPercurso, DateTimeOffset? DataInicioViagem, bool IsCanalVerde, bool IsCarregamentoPosterior,
        Veiculo? VeiculoTracao, Condutor? Condutor,
        Veiculo? Reboque1, Veiculo? Reboque2, Veiculo? Reboque3,
        bool HasSeguro, string SeguradoraCnpj, string SeguradoraNome, string NumeroApolice, string NumeroAverbacao,
        bool HasProdutoPredominante, string TipoCarga, string NomeProdutoPredominante, string NcmProduto,
        bool HasCiotValePedagio, string Ciot, string CpfCnpjCiot, string CnpjFornecedorValePedagio, string CnpjPagadorValePedagio
    ) : IRequest<EmitirManifestoResult>;

    public record EmitirManifestoResult(bool Sucesso, string Mensagem, string XmlEnvio, string XmlRetorno);

    public class EmitirManifestoHandler : IRequestHandler<EmitirManifestoCommand, EmitirManifestoResult>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IMediator _mediator;

        public EmitirManifestoHandler(IAppDbContext dbContext, IMediator mediator)
        {
            _dbContext = dbContext;
            _mediator = mediator;
        }

        public async Task<EmitirManifestoResult> Handle(EmitirManifestoCommand request, CancellationToken cancellationToken)
        {
            // 1. Aplica e garante as configurações do certificado
            var configAplicada = await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);
            if (!configAplicada) return new EmitirManifestoResult(false, "Falha ao carregar configurações da empresa (Certificado/Ambiente).", "", "");

            var empresa = await _dbContext.Empresas.Include(e => e.Configuracao).FirstOrDefaultAsync(e => e.Id == request.EmpresaId, cancellationToken);
            if (empresa == null || empresa.Configuracao == null) return new EmitirManifestoResult(false, "Empresa não encontrada.", "", "");

            // Incrementa NSU
            empresa.Configuracao.UltimaNumeracao++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var mdfe = new MDFeEletronico();

            #region Identificação (Ide)
            _ = Enum.TryParse(empresa.Configuracao.UfEmitente, out Estado ufEmitente);
            _ = Enum.TryParse(request.UfCarregamento, out Estado ufOrigem);
            _ = Enum.TryParse(request.UfDescarregamento, out Estado ufDestino);

            mdfe.InfMDFe.Ide.CUF = ufEmitente;
            mdfe.InfMDFe.Ide.TpAmb = (TipoAmbiente)empresa.Configuracao.TipoAmbiente;
            mdfe.InfMDFe.Ide.TpEmit = (MDFeTipoEmitente)request.TipoEmitente;

            if (request.TipoTransportador > 0)
                mdfe.InfMDFe.Ide.TpTransp = (MDFeTpTransp)request.TipoTransportador;

            mdfe.InfMDFe.Ide.Mod = ModeloDocumento.MDFe;
            mdfe.InfMDFe.Ide.Serie = (short)empresa.Configuracao.Serie;
            mdfe.InfMDFe.Ide.NMDF = empresa.Configuracao.UltimaNumeracao;
            mdfe.InfMDFe.Ide.CMDF = new Random().Next(11111111, 99999999);
            mdfe.InfMDFe.Ide.Modal = MDFeModal.Rodoviario; // Fixo no passo atual
            mdfe.InfMDFe.Ide.DhEmi = DateTime.Now;
            mdfe.InfMDFe.Ide.TpEmis = MDFeTipoEmissao.Normal;
            mdfe.InfMDFe.Ide.ProcEmi = MDFeIdentificacaoProcessoEmissao.EmissaoComAplicativoContribuinte;
            mdfe.InfMDFe.Ide.VerProc = "CoreMDFe_1.0";
            mdfe.InfMDFe.Ide.UFIni = ufOrigem;
            mdfe.InfMDFe.Ide.UFFim = ufDestino;

            // Agrupa Cidades de Carregamento únicas baseadas nas notas
            var carregamentos = request.Documentos.GroupBy(d => new { d.IbgeCarregamento, d.MunicipioCarregamento });
            foreach (var c in carregamentos)
            {
                if (c.Key.IbgeCarregamento > 0)
                    mdfe.InfMDFe.Ide.InfMunCarrega.Add(new MDFeInfMunCarrega { CMunCarrega = c.Key.IbgeCarregamento.ToString(), XMunCarrega = c.Key.MunicipioCarregamento });
            }

            // Opcionais Percurso
            if (request.DataInicioViagem.HasValue)
                mdfe.InfMDFe.Ide.DhIniViagem = request.DataInicioViagem.Value.DateTime;

            if (request.IsCanalVerde) mdfe.InfMDFe.Ide.IndCanalVerde = "1";
            if (request.IsCarregamentoPosterior) mdfe.InfMDFe.Ide.IndCarregaPosterior = "1";

            if (!string.IsNullOrWhiteSpace(request.UfsPercurso))
            {
                var ufs = request.UfsPercurso.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var uf in ufs)
                {
                    if (Enum.TryParse(uf, out Estado estPercurso))
                        mdfe.InfMDFe.Ide.InfPercurso.Add(new MDFeInfPercurso { UFPer = estPercurso });
                }
            }
            #endregion

            #region Emitente (Emit)
            mdfe.InfMDFe.Emit.CNPJ = empresa.Cnpj;
            mdfe.InfMDFe.Emit.IE = empresa.InscricaoEstadual;
            mdfe.InfMDFe.Emit.XNome = empresa.Nome;
            mdfe.InfMDFe.Emit.XFant = empresa.NomeFantasia;

            mdfe.InfMDFe.Emit.EnderEmit.XLgr = empresa.Logradouro;
            mdfe.InfMDFe.Emit.EnderEmit.Nro = empresa.Numero;
            mdfe.InfMDFe.Emit.EnderEmit.XCpl = string.IsNullOrWhiteSpace(empresa.Complemento) ? null : empresa.Complemento;
            mdfe.InfMDFe.Emit.EnderEmit.XBairro = empresa.Bairro;
            mdfe.InfMDFe.Emit.EnderEmit.CMun = empresa.CodigoIbgeMunicipio;
            mdfe.InfMDFe.Emit.EnderEmit.XMun = empresa.NomeMunicipio;
            mdfe.InfMDFe.Emit.EnderEmit.CEP = long.Parse(empresa.Cep.Replace("-", ""));
            mdfe.InfMDFe.Emit.EnderEmit.UF = ufEmitente;
            #endregion

            #region Modal Rodoviário
            mdfe.InfMDFe.InfModal.VersaoModal = MDFeVersaoModal.Versao300;
            var rodo = new MDFeRodo();

            // Dados da ANTT (RNTRC, CIOT, Vale Pedagio)
            if (!string.IsNullOrWhiteSpace(empresa.RNTRC) || request.HasCiotValePedagio)
            {
                rodo.InfANTT = new MDFeInfANTT { RNTRC = string.IsNullOrWhiteSpace(empresa.RNTRC) ? null : empresa.RNTRC };

                if (request.HasCiotValePedagio)
                {
                    if (!string.IsNullOrWhiteSpace(request.Ciot))
                    {
                        var ciotObj = new infCIOT { CIOT = request.Ciot };
                        if (request.CpfCnpjCiot.Length == 14) ciotObj.CNPJ = request.CpfCnpjCiot;
                        else ciotObj.CPF = request.CpfCnpjCiot;
                        rodo.InfANTT.InfCIOT.Add(ciotObj);
                    }

                    if (!string.IsNullOrWhiteSpace(request.CnpjFornecedorValePedagio))
                    {
                        rodo.InfANTT.ValePed = new MDFeValePed();
                        rodo.InfANTT.ValePed.Disp.Add(new MDFeDisp
                        {
                            CNPJForn = request.CnpjFornecedorValePedagio,
                            CNPJPg = request.CnpjPagadorValePedagio,
                            // O Zeus tem nCompra como string, o schema diz q é opcional. Preencheremos um padrao se vazio
                            NCompra = "0"
                        });
                    }
                }
            }

            if (request.VeiculoTracao != null && request.Condutor != null)
            {
                _ = Enum.TryParse(request.VeiculoTracao.UfLicenciamento, out Estado ufVeiculo);
                rodo.VeicTracao = new MDFeVeicTracao
                {
                    Placa = request.VeiculoTracao.Placa.Replace("-", "").ToUpper(),
                    RENAVAM = string.IsNullOrWhiteSpace(request.VeiculoTracao.Renavam) ? null : request.VeiculoTracao.Renavam,
                    UF = ufVeiculo,
                    Tara = request.VeiculoTracao.TaraKg > 0 ? request.VeiculoTracao.TaraKg : 10000,
                    CapKG = request.VeiculoTracao.CapacidadeKg > 0 ? request.VeiculoTracao.CapacidadeKg : 20000,
                    TpRod = (MDFeTpRod)int.Parse(request.VeiculoTracao.TipoRodado),
                    TpCar = (MDFeTpCar)int.Parse(request.VeiculoTracao.TipoCarroceria),
                    Condutor = new List<MDFeCondutor> { new MDFeCondutor { CPF = request.Condutor.Cpf.Replace(".", "").Replace("-", ""), XNome = request.Condutor.Nome } }
                };
            }

            // Adiciona Reboques (se existirem)
            var reboques = new[] { request.Reboque1, request.Reboque2, request.Reboque3 }.Where(r => r != null).ToList();
            if (reboques.Any())
            {
                rodo.VeicReboque = new List<MDFeVeicReboque>();
                foreach (var reb in reboques)
                {
                    _ = Enum.TryParse(reb!.UfLicenciamento, out Estado ufReb);
                    rodo.VeicReboque.Add(new MDFeVeicReboque
                    {
                        Placa = reb.Placa.Replace("-", "").ToUpper(),
                        RENAVAM = string.IsNullOrWhiteSpace(reb.Renavam) ? null : reb.Renavam,
                        UF = ufReb,
                        Tara = reb.TaraKg > 0 ? reb.TaraKg : 5000,
                        CapKG = reb.CapacidadeKg > 0 ? reb.CapacidadeKg : 15000,
                        TpCar = (MDFeTpCar)int.Parse(reb.TipoCarroceria)
                    });
                }
            }

            mdfe.InfMDFe.InfModal.Modal = rodo;
            #endregion

            #region Documentos (InfDoc)
            mdfe.InfMDFe.InfDoc.InfMunDescarga = new List<MDFeInfMunDescarga>();
            var cidadesDescarga = request.Documentos.GroupBy(d => new { d.IbgeDescarga, d.MunicipioDescarga });

            foreach (var cidade in cidadesDescarga)
            {
                var descarga = new MDFeInfMunDescarga
                {
                    CMunDescarga = cidade.Key.IbgeDescarga.ToString(),
                    XMunDescarga = cidade.Key.MunicipioDescarga,
                    InfNFe = new List<MDFeInfNFe>(),
                    InfCTe = new List<MDFeInfCTe>()
                };

                foreach (var doc in cidade)
                {
                    if (doc.Tipo == 55) descarga.InfNFe.Add(new MDFeInfNFe { ChNFe = doc.Chave });
                    else if (doc.Tipo == 57) descarga.InfCTe.Add(new MDFeInfCTe { ChCTe = doc.Chave });
                }

                // O Zeus e a Sefaz rejeitam a tag se ela estiver vazia, então anulamos.
                if (!descarga.InfNFe.Any()) descarga.InfNFe = null;
                if (!descarga.InfCTe.Any()) descarga.InfCTe = null;

                mdfe.InfMDFe.InfDoc.InfMunDescarga.Add(descarga);
            }
            #endregion

            #region Produto Predominante e Seguro
            if (request.HasProdutoPredominante)
            {
                mdfe.InfMDFe.ProdPred = new MDFeProdPred
                {
                    TpCarga = (MDFeTpCarga)int.Parse(request.TipoCarga),
                    XProd = request.NomeProdutoPredominante,
                    Ncm = string.IsNullOrWhiteSpace(request.NcmProduto) ? null : request.NcmProduto
                };
            }

            if (request.HasSeguro)
            {
                mdfe.InfMDFe.Seg = new List<MDFeSeg>
                {
                    new MDFeSeg
                    {
                        InfResp = new MDFeInfResp { RespSeg = MDFeRespSeg.EmitenteDoMDFe, CNPJ = empresa.Cnpj },
                        InfSeg = new MDFeInfSeg { CNPJ = request.SeguradoraCnpj, XSeg = request.SeguradoraNome },
                        NApol = request.NumeroApolice,
                        NAver = new List<string> { request.NumeroAverbacao }
                    }
                };
            }
            #endregion

            #region Totais (Tot)
            mdfe.InfMDFe.Tot.QNFe = request.Documentos.Count(d => d.Tipo == 55);
            mdfe.InfMDFe.Tot.QCTe = request.Documentos.Count(d => d.Tipo == 57);

            // Corrige se não houver NFe ou CTe para não gerar tag vazia/zero rejeitada
            if (mdfe.InfMDFe.Tot.QNFe == 0) mdfe.InfMDFe.Tot.QNFe = null;
            if (mdfe.InfMDFe.Tot.QCTe == 0) mdfe.InfMDFe.Tot.QCTe = null;

            mdfe.InfMDFe.Tot.vCarga = request.Documentos.Sum(d => d.Valor);
            mdfe.InfMDFe.Tot.CUnid = MDFeCUnid.KG;
            mdfe.InfMDFe.Tot.QCarga = request.Documentos.Sum(d => d.Peso);
            #endregion

            // 5. Assinar e Transmitir!
            try
            {
                var servicoRecepcao = new ServicoMDFeRecepcao();
                var retornoEnvio = servicoRecepcao.MDFeRecepcaoSinc(mdfe);

                // 6. Salvar Histórico
                var historico = new ManifestoEletronico
                {
                    EmpresaId = empresa.Id,
                    Numero = (int)mdfe.InfMDFe.Ide.NMDF,
                    Serie = mdfe.InfMDFe.Ide.Serie,
                    DataEmissao = mdfe.InfMDFe.Ide.DhEmi,
                    UfOrigem = mdfe.InfMDFe.Ide.UFIni.ToString(),
                    UfDestino = mdfe.InfMDFe.Ide.UFFim.ToString(),
                    Modalidade = (int)mdfe.InfMDFe.Ide.Modal,
                    TipoAmbiente = empresa.Configuracao.TipoAmbiente,
                    ChaveAcesso = mdfe.InfMDFe.Id.Substring(4),
                    XmlAssinado = retornoEnvio.EnvioXmlString ?? "",
                    XmlAutorizado = retornoEnvio.RetornoXmlString ?? "",
                    CodigoStatus = retornoEnvio?.CStat.ToString() ?? "0",
                    MotivoStatus = retornoEnvio?.XMotivo ?? "Sem comunicação"
                };

                historico.Status = historico.CodigoStatus == "100" ? StatusManifesto.Autorizado : StatusManifesto.Rejeitado;

                _dbContext.Manifestos.Add(historico);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new EmitirManifestoResult(historico.Status == StatusManifesto.Autorizado, historico.MotivoStatus, historico.XmlAssinado, historico.XmlAutorizado);
            }
            catch (Exception ex)
            {
                return new EmitirManifestoResult(false, $"Erro fatal ao emitir: {ex.Message}", "", "");
            }
        }
    }
}