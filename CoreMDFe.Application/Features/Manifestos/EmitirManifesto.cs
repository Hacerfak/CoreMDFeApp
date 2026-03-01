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
    public class DocumentoMDFeDto
    {
        public string Chave { get; set; } = string.Empty;
        public int Tipo { get; set; }
        public decimal Valor { get; set; }
        public decimal Peso { get; set; }
        public long IbgeCarregamento { get; set; }
        public string MunicipioCarregamento { get; set; } = string.Empty;
        public string UfCarregamento { get; set; } = string.Empty;
        public long IbgeDescarga { get; set; }
        public string MunicipioDescarga { get; set; } = string.Empty;
        public string UfDescarga { get; set; } = string.Empty;
        public override string ToString() => $"{(Tipo == 55 ? "NF-e" : "CT-e")} - {Chave}";
    }

    public record EmitirManifestoCommand(
        Guid EmpresaId,
        List<DocumentoMDFeDto> Documentos,
        string UfCarregamento, string UfDescarregamento,
        int TipoEmitente, int TipoTransportador, int Modal, int TipoEmissao,
        string UfsPercurso, DateTimeOffset? DataInicioViagem, bool IsCanalVerde, bool IsCarregamentoPosterior,
        Veiculo? VeiculoTracao, Condutor? Condutor,
        Veiculo? Reboque1, Veiculo? Reboque2, Veiculo? Reboque3,
        bool HasSeguro, string SeguradoraCnpj, string SeguradoraNome, string NumeroApolice, string NumeroAverbacao,
        bool HasProdutoPredominante, string TipoCarga, string NomeProdutoPredominante, string GTINProdutoPredominante, string NcmProduto,
        bool HasCiotValePedagio, string Ciot, string CpfCnpjCiot, string CnpjFornecedorValePedagio, string CnpjPagadorValePedagio, bool HasLacres, List<Lacre> Lacres,
        // --- NOVOS CAMPOS PARA CARREGAMENTO POSTERIOR ---
        string? IbgeCarregamentoManual = null, string? MunicipioCarregamentoManual = null,
        string? IbgeDescarregamentoManual = null, string? MunicipioDescarregamentoManual = null
    ) : IRequest<EmitirManifestoResult>;

    public record EmitirManifestoResult(bool Sucesso, string Mensagem, string XmlEnvio, string XmlRetorno, Guid? ManifestoId = null);

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
            Console.WriteLine("[EMISSÃO] Iniciando geração do MDF-e...");

            var configAplicada = await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);
            if (!configAplicada) return new EmitirManifestoResult(false, "Falha nas configurações de Certificado.", "", "");

            var empresa = await _dbContext.Empresas.Include(e => e.Configuracao).FirstOrDefaultAsync(e => e.Id == request.EmpresaId, cancellationToken);
            if (empresa == null || empresa.Configuracao == null) return new EmitirManifestoResult(false, "Empresa não encontrada.", "", "");

            await _dbContext.SaveChangesAsync(cancellationToken);

            var mdfe = new MDFeEletronico();

            #region Identificação (Ide)
            _ = Enum.TryParse(empresa.Configuracao.UfEmitente, out Estado ufEmitente);
            _ = Enum.TryParse(request.UfCarregamento, out Estado ufOrigem);
            _ = Enum.TryParse(request.UfDescarregamento, out Estado ufDestino);

            mdfe.InfMDFe.Ide.CUF = ufEmitente;
            mdfe.InfMDFe.Ide.TpAmb = (TipoAmbiente)empresa.Configuracao.TipoAmbiente;
            mdfe.InfMDFe.Ide.TpEmit = request.TipoEmitente > 0 ? (MDFeTipoEmitente)request.TipoEmitente : (MDFeTipoEmitente)empresa.Configuracao.TipoEmitentePadrao;
            var tpTranspFinal = request.TipoTransportador > 0 ? (MDFeTpTransp)request.TipoTransportador : (MDFeTpTransp)empresa.Configuracao.TipoTransportadorPadrao;
            if (tpTranspFinal > 0) mdfe.InfMDFe.Ide.TpTransp = tpTranspFinal; // Opcional
            mdfe.InfMDFe.Ide.Mod = ModeloDocumento.MDFe;
            mdfe.InfMDFe.Ide.Serie = (short)empresa.Configuracao.Serie;
            mdfe.InfMDFe.Ide.NMDF = empresa.Configuracao.UltimaNumeracao;
            mdfe.InfMDFe.Ide.CMDF = new Random().Next(11111111, 99999999);
            mdfe.InfMDFe.Ide.Modal = request.Modal > 0 ? (MDFeModal)request.Modal : (MDFeModal)empresa.Configuracao.ModalidadePadrao;
            mdfe.InfMDFe.Ide.DhEmi = DateTime.Now;
            mdfe.InfMDFe.Ide.TpEmis = request.TipoEmissao > 0 ? (MDFeTipoEmissao)request.TipoEmissao : (MDFeTipoEmissao)empresa.Configuracao.TipoEmissaoPadrao;
            mdfe.InfMDFe.Ide.ProcEmi = MDFeIdentificacaoProcessoEmissao.EmissaoComAplicativoContribuinte;
            mdfe.InfMDFe.Ide.VerProc = "CoreMDFe_1.0";
            mdfe.InfMDFe.Ide.UFIni = ufOrigem;
            mdfe.InfMDFe.Ide.UFFim = ufDestino;

            // Lógica Exclusiva: Carregamento Posterior ou Normal
            if (request.IsCarregamentoPosterior)
            {
                mdfe.InfMDFe.Ide.IndCarregaPosterior = "1"; //Opcional
                if (!string.IsNullOrWhiteSpace(request.IbgeCarregamentoManual))
                {
                    mdfe.InfMDFe.Ide.InfMunCarrega.Add(new MDFeInfMunCarrega
                    {
                        CMunCarrega = request.IbgeCarregamentoManual,
                        XMunCarrega = request.MunicipioCarregamentoManual
                    });
                }
            }
            else
            {
                var carregamentos = request.Documentos.GroupBy(d => new { d.IbgeCarregamento, d.MunicipioCarregamento });
                foreach (var c in carregamentos)
                    if (c.Key.IbgeCarregamento > 0)
                        mdfe.InfMDFe.Ide.InfMunCarrega.Add(new MDFeInfMunCarrega { CMunCarrega = c.Key.IbgeCarregamento.ToString(), XMunCarrega = c.Key.MunicipioCarregamento });
            }

            if (!string.IsNullOrWhiteSpace(request.UfsPercurso))
            {
                var ufs = request.UfsPercurso.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var uf in ufs)
                    if (Enum.TryParse(uf, out Estado estPercurso))
                        mdfe.InfMDFe.Ide.InfPercurso.Add(new MDFeInfPercurso { UFPer = estPercurso });
            }

            if (request.DataInicioViagem.HasValue) mdfe.InfMDFe.Ide.DhIniViagem = request.DataInicioViagem.Value.DateTime; //Opcional
            if (request.IsCanalVerde) mdfe.InfMDFe.Ide.IndCanalVerde = "1"; //Opcional
            #endregion

            #region Emitente (Emit)
            mdfe.InfMDFe.Emit.CNPJ = empresa.Cnpj;
            // mdfe.InfMDFe.Emit.CPF = "99999999999"; // Usar com série específica 920-969 para emitente pessoa física com inscrição estadual. Poderá ser usado também para emissão do Regime Especial da Nota Fiscal Fácil.
            mdfe.InfMDFe.Emit.IE = empresa.InscricaoEstadual; //Opcional
            mdfe.InfMDFe.Emit.XNome = empresa.Nome;
            mdfe.InfMDFe.Emit.XFant = empresa.NomeFantasia; //Opcional
            mdfe.InfMDFe.Emit.EnderEmit.XLgr = empresa.Logradouro;
            mdfe.InfMDFe.Emit.EnderEmit.Nro = empresa.Numero;
            mdfe.InfMDFe.Emit.EnderEmit.XCpl = string.IsNullOrWhiteSpace(empresa.Complemento) ? null : empresa.Complemento; //Opcional
            mdfe.InfMDFe.Emit.EnderEmit.XBairro = empresa.Bairro;
            mdfe.InfMDFe.Emit.EnderEmit.CMun = empresa.CodigoIbgeMunicipio;
            mdfe.InfMDFe.Emit.EnderEmit.XMun = empresa.NomeMunicipio;
            mdfe.InfMDFe.Emit.EnderEmit.CEP = long.Parse(empresa.Cep.Replace("-", "")); //Opcional
            mdfe.InfMDFe.Emit.EnderEmit.UF = ufEmitente;
            mdfe.InfMDFe.Emit.EnderEmit.Fone = empresa.Telefone; //Opcional
            mdfe.InfMDFe.Emit.EnderEmit.Email = empresa.Email; //Opcional
            #endregion

            #region Modal Rodoviário
            mdfe.InfMDFe.InfModal.VersaoModal = MDFeVersaoModal.Versao300;
            var rodo = new MDFeRodo();

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
            mdfe.InfMDFe.InfDoc = new MDFeInfDoc();
            mdfe.InfMDFe.InfDoc.InfMunDescarga = new List<MDFeInfMunDescarga>();

            if (request.IsCarregamentoPosterior)
            {
                // Para Carregamento Posterior, enviamos o município de descarga exigido, mas SEM NENHUMA NOTA
                mdfe.InfMDFe.InfDoc.InfMunDescarga.Add(new MDFeInfMunDescarga
                {
                    CMunDescarga = request.IbgeDescarregamentoManual,
                    XMunDescarga = request.MunicipioDescarregamentoManual,
                    InfNFe = null, // Sem notas
                    InfCTe = null  // Sem notas
                });
            }
            else
            {
                // Emissão Normal (Com notas)
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
                    if (!descarga.InfNFe.Any()) descarga.InfNFe = null;
                    if (!descarga.InfCTe.Any()) descarga.InfCTe = null;
                    mdfe.InfMDFe.InfDoc.InfMunDescarga.Add(descarga);
                }
            }
            #endregion

            #region Produto Predominante e Seguro
            if (request.HasSeguro)
            {
                mdfe.InfMDFe.Seg = new List<MDFeSeg>
                {
                    new MDFeSeg
                    {
                        InfResp = new MDFeInfResp { RespSeg = MDFeRespSeg.EmitenteDoMDFe, CNPJ = empresa.Cnpj },
                        InfSeg = new MDFeInfSeg { CNPJ = request.SeguradoraCnpj.Replace(".", "").Replace("/", "").Replace("-", ""), XSeg = request.SeguradoraNome },
                        NApol = request.NumeroApolice,
                        NAver = new List<string> { request.NumeroAverbacao }
                    }
                };
            }

            if (request.HasProdutoPredominante && !request.IsCarregamentoPosterior)
            {
                mdfe.InfMDFe.ProdPred = new MDFeProdPred
                {
                    TpCarga = (MDFeTpCarga)int.Parse(request.TipoCarga),
                    XProd = request.NomeProdutoPredominante,
                    CEan = string.IsNullOrWhiteSpace(request.GTINProdutoPredominante) ? null : request.GTINProdutoPredominante,
                    Ncm = string.IsNullOrWhiteSpace(request.NcmProduto) ? null : request.NcmProduto
                };
            }
            #endregion

            #region Totais (Tot)
            mdfe.InfMDFe.Tot = new MDFeTot();
            mdfe.InfMDFe.Tot.CUnid = (MDFeCUnid)empresa.Configuracao.CodigoUnidadePesoPadrao;

            if (request.IsCarregamentoPosterior)
            {
                // Para Carregamento Posterior, a SEFAZ exige a tag, mas zerada
                mdfe.InfMDFe.Tot.QNFe = null;
                mdfe.InfMDFe.Tot.QCTe = null;
                mdfe.InfMDFe.Tot.vCarga = 0m;
                mdfe.InfMDFe.Tot.QCarga = 0m;
            }
            else
            {
                // Totais Normais
                mdfe.InfMDFe.Tot.QNFe = request.Documentos.Count(d => d.Tipo == 55);
                mdfe.InfMDFe.Tot.QCTe = request.Documentos.Count(d => d.Tipo == 57);
                if (mdfe.InfMDFe.Tot.QNFe == 0) mdfe.InfMDFe.Tot.QNFe = null;
                if (mdfe.InfMDFe.Tot.QCTe == 0) mdfe.InfMDFe.Tot.QCTe = null;

                mdfe.InfMDFe.Tot.vCarga = request.Documentos.Sum(d => d.Valor);
                mdfe.InfMDFe.Tot.QCarga = request.Documentos.Sum(d => d.Peso);
            }
            #endregion

            #region Lacres
            if (((MDFeModal)request.Modal == MDFeModal.Rodoviario || (MDFeModal)request.Modal == MDFeModal.Ferroviario) && request.HasLacres)
            {
                mdfe.InfMDFe.Lacres = new List<MDFeLacre>();
                foreach (var lacre in request.Lacres)
                {
                    mdfe.InfMDFe.Lacres.Add(new MDFeLacre { NLacre = lacre.Numero });

                }
            }
            #endregion

            #region Autorizados para Download
            if (empresa.Configuracao.isAutorizadosDownload && empresa.Configuracao.AutorizadosDownload?.Any() == true)
            {
                mdfe.InfMDFe.AutXml = new List<MDFeAutXML>();
                foreach (var autorizado in empresa.Configuracao.AutorizadosDownload)
                {
                    var autXml = new MDFeAutXML();

                    if (!string.IsNullOrWhiteSpace(autorizado.CNPJ))
                        autXml.CNPJ = autorizado.CNPJ.Replace(".", "").Replace("/", "").Replace("-", "");
                    else if (!string.IsNullOrWhiteSpace(autorizado.CPF))
                        autXml.CPF = autorizado.CPF.Replace(".", "").Replace("-", "");

                    mdfe.InfMDFe.AutXml.Add(autXml);
                }
            }
            #endregion

            #region Informações adicionais
            mdfe.InfMDFe.InfAdic = new MDFeInfAdic();
            mdfe.InfMDFe.InfAdic.InfAdFisco = "Teste 123 ao fisco";
            mdfe.InfMDFe.InfAdic.InfCpl = "Teste 123 ao complemento";
            #endregion

            #region Responsável Técnico
            mdfe.InfMDFe.InfRespTec = new MDFeInfRespTec
            {
                CNPJ = "64615275000112", // Apenas números
                XContato = "Eder Gross Cichelero",
                Email = "hacerfak@hacerfak.com.br",
                Fone = "54992221877" // Apenas números
            };
            #endregion

            try
            {
                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine("[DEBUG] XML GERADO (PRÉ-ASSINATURA):");
                Console.WriteLine(mdfe);
                Console.WriteLine("--------------------------------------------------");
            }
            catch (Exception ex) { Console.WriteLine($"[ERRO LOG XML] {ex.Message}"); }

            try
            {
                var servicoRecepcao = new ServicoMDFeRecepcao();
                var retornoEnvio = servicoRecepcao.MDFeRecepcaoSinc(mdfe);

                var historico = new ManifestoEletronico
                {
                    EmpresaId = empresa.Id,
                    Numero = (int)mdfe.InfMDFe.Ide.NMDF,
                    Serie = mdfe.InfMDFe.Ide.Serie,
                    DataEmissao = mdfe.InfMDFe.Ide.DhEmi,
                    DataHoraInicioViagem = request.DataInicioViagem?.DateTime ?? DateTime.Now,
                    UfOrigem = mdfe.InfMDFe.Ide.UFIni.ToString(),
                    UfDestino = mdfe.InfMDFe.Ide.UFFim.ToString(),
                    Modalidade = (int)mdfe.InfMDFe.Ide.Modal,
                    TipoAmbiente = empresa.Configuracao.TipoAmbiente,
                    ChaveAcesso = mdfe.InfMDFe.Id?.Length >= 47 ? mdfe.InfMDFe.Id.Substring(4) : "",
                    XmlAssinado = retornoEnvio.EnvioXmlString ?? "",
                    ReciboAutorizacao = retornoEnvio.RetornoXmlString ?? "",
                    ProtocoloAutorizacao = retornoEnvio.ProtMdFe?.InfProt?.NProt ?? "",
                    CodigoStatus = retornoEnvio?.CStat.ToString() ?? "0",
                    MotivoStatus = retornoEnvio?.XMotivo ?? "Sem comunicação",
                    IndicadorCarregamentoPosterior = request.IsCarregamentoPosterior,

                    // Totais Básicos
                    QtdNFe = request.IsCarregamentoPosterior ? 0 : request.Documentos.Count(d => d.Tipo == 55),
                    QtdCTe = request.IsCarregamentoPosterior ? 0 : request.Documentos.Count(d => d.Tipo == 57),
                    ValorTotalCarga = request.IsCarregamentoPosterior ? 0 : request.Documentos.Sum(d => d.Valor),
                    PesoTotalCarga = request.IsCarregamentoPosterior ? 0 : request.Documentos.Sum(d => d.Peso),

                    // Produto Predominante
                    ProdutoTipoCarga = request.HasProdutoPredominante && !request.IsCarregamentoPosterior ? request.TipoCarga : "",
                    ProdutoNome = request.HasProdutoPredominante && !request.IsCarregamentoPosterior ? request.NomeProdutoPredominante : "",
                    ProdutoNCM = request.HasProdutoPredominante && !request.IsCarregamentoPosterior ? request.NcmProduto : ""
                };

                // 1. Relacionamento: Veículos (Tração e Reboques)
                if (request.VeiculoTracao != null)
                {
                    historico.Veiculos.Add(new ManifestoVeiculo
                    {
                        VeiculoBaseId = request.VeiculoTracao.Id,
                        Tipo = 0,
                        Placa = request.VeiculoTracao.Placa,
                        Renavam = request.VeiculoTracao.Renavam ?? "",
                        TaraKg = request.VeiculoTracao.TaraKg,
                        CapacidadeKg = request.VeiculoTracao.CapacidadeKg,
                        CapacidadeM3 = request.VeiculoTracao.CapacidadeM3,
                        TipoRodado = request.VeiculoTracao.TipoRodado ?? "",
                        TipoCarroceria = request.VeiculoTracao.TipoCarroceria ?? "",
                        UfLicenciamento = request.VeiculoTracao.UfLicenciamento ?? ""
                    });
                }

                foreach (var reb in reboques)
                {
                    historico.Veiculos.Add(new ManifestoVeiculo
                    {
                        VeiculoBaseId = reb!.Id,
                        Tipo = 1,
                        Placa = reb.Placa,
                        Renavam = reb.Renavam ?? "",
                        TaraKg = reb.TaraKg,
                        CapacidadeKg = reb.CapacidadeKg,
                        CapacidadeM3 = reb.CapacidadeM3,
                        TipoCarroceria = reb.TipoCarroceria ?? "",
                        UfLicenciamento = reb.UfLicenciamento ?? ""
                    });
                }

                // 2. Relacionamento: Condutores
                if (request.Condutor != null)
                {
                    historico.Condutores.Add(new ManifestoCondutor { CondutorBaseId = request.Condutor.Id, Cpf = request.Condutor.Cpf, Nome = request.Condutor.Nome });
                }

                // 3. Relacionamento: Cidades e Notas Fiscais
                if (request.IsCarregamentoPosterior)
                {
                    // Apenas regista as cidades manuais (Sem Notas)
                    if (long.TryParse(request.IbgeCarregamentoManual, out long codCarrega))
                        historico.MunicipiosCarregamento.Add(new ManifestoMunicipioCarregamento { CodigoIbge = codCarrega, NomeMunicipio = request.MunicipioCarregamentoManual ?? "" });

                    if (long.TryParse(request.IbgeDescarregamentoManual, out long codDescarga))
                        historico.MunicipiosDescarregamento.Add(new ManifestoMunicipioDescarregamento { CodigoIbge = codDescarga, NomeMunicipio = request.MunicipioDescarregamentoManual ?? "" });
                }
                else
                {
                    // Regista Cidades extraídas dos XMLs e atrela as notas aos respetivos municípios
                    foreach (var c in request.Documentos.GroupBy(d => new { d.IbgeCarregamento, d.MunicipioCarregamento }))
                    {
                        historico.MunicipiosCarregamento.Add(new ManifestoMunicipioCarregamento { CodigoIbge = c.Key.IbgeCarregamento, NomeMunicipio = c.Key.MunicipioCarregamento });
                    }

                    foreach (var d in request.Documentos.GroupBy(d => new { d.IbgeDescarga, d.MunicipioDescarga }))
                    {
                        var munDescarga = new ManifestoMunicipioDescarregamento { CodigoIbge = d.Key.IbgeDescarga, NomeMunicipio = d.Key.MunicipioDescarga };
                        foreach (var doc in d)
                        {
                            munDescarga.Documentos.Add(new ManifestoDocumentoFiscal { TipoDocumento = doc.Tipo, ChaveAcesso = doc.Chave });
                        }
                        historico.MunicipiosDescarregamento.Add(munDescarga);
                    }
                }

                // 4. Relacionamento: Percurso
                if (!string.IsNullOrWhiteSpace(request.UfsPercurso))
                {
                    int ordem = 1;
                    foreach (var uf in request.UfsPercurso.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        historico.Percursos.Add(new ManifestoPercurso { UF = uf, Ordem = ordem++ });
                }

                // 5. Relacionamento: Seguro, CIOT e Vale Pedágio
                if (request.HasSeguro)
                {
                    historico.Seguros.Add(new ManifestoSeguro { CnpjSeguradora = request.SeguradoraCnpj, NomeSeguradora = request.SeguradoraNome, NumeroApolice = request.NumeroApolice, NumeroAverbacao = request.NumeroAverbacao, Responsavel = 1 });
                }
                if (request.HasCiotValePedagio && !string.IsNullOrWhiteSpace(request.Ciot))
                    historico.Ciots.Add(new ManifestoCiot { Ciot = request.Ciot, CpfCnpj = request.CpfCnpjCiot });
                if (request.HasCiotValePedagio && !string.IsNullOrWhiteSpace(request.CnpjFornecedorValePedagio))
                    historico.ValesPedagio.Add(new ManifestoValePedagio { CnpjFornecedor = request.CnpjFornecedorValePedagio, CpfCnpjPagador = request.CnpjPagadorValePedagio });
                // 6. Relacionamento: Lacres e Autorizados para Histórico
                if (((MDFeModal)request.Modal == MDFeModal.Rodoviario || (MDFeModal)request.Modal == MDFeModal.Ferroviario) && request.HasLacres && request.Lacres != null)
                {
                    foreach (var lacre in request.Lacres)
                    {
                        historico.Lacres.Add(new ManifestoLacre { Numero = lacre.Numero });
                    }
                }

                if (empresa.Configuracao.isAutorizadosDownload && empresa.Configuracao.AutorizadosDownload?.Any() == true)
                {
                    foreach (var aut in empresa.Configuracao.AutorizadosDownload)
                    {
                        // Pega o que estiver preenchido e limpa as formatações
                        var docFormatado = !string.IsNullOrWhiteSpace(aut.CNPJ) ? aut.CNPJ : aut.CPF;
                        docFormatado = docFormatado.Replace(".", "").Replace("/", "").Replace("-", "");

                        historico.AutorizadosDownload.Add(new ManifestoAutorizadoDownload { CpfCnpj = docFormatado });
                    }
                }

                historico.Status = historico.CodigoStatus == "100" ? StatusManifesto.Autorizado : StatusManifesto.Rejeitado;
                if (historico.Status == StatusManifesto.Autorizado)
                {
                    empresa.Configuracao.UltimaNumeracao++;
                }
                _dbContext.Manifestos.Add(historico);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new EmitirManifestoResult(historico.Status == StatusManifesto.Autorizado, historico.MotivoStatus, historico.XmlAssinado, historico.ReciboAutorizacao, historico.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AÇÃO - ERRO] {ex}");
                return new EmitirManifestoResult(false, $"Erro fatal ao emitir: {ex.Message}", "", "");
            }
        }
    }
}