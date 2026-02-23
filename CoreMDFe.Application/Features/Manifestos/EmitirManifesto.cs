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
        bool HasProdutoPredominante, string TipoCarga, string NomeProdutoPredominante, string NcmProduto,
        bool HasCiotValePedagio, string Ciot, string CpfCnpjCiot, string CnpjFornecedorValePedagio, string CnpjPagadorValePedagio,
        string? RespTecCnpj = null, string? RespTecNome = null, string? RespTecTelefone = null, string? RespTecEmail = null
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

            empresa.Configuracao.UltimaNumeracao++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var mdfe = new MDFeEletronico();

            #region Identificação (Ide)
            _ = Enum.TryParse(empresa.Configuracao.UfEmitente, out Estado ufEmitente);
            _ = Enum.TryParse(request.UfCarregamento, out Estado ufOrigem);
            _ = Enum.TryParse(request.UfDescarregamento, out Estado ufDestino);

            mdfe.InfMDFe.Ide.CUF = ufEmitente;
            mdfe.InfMDFe.Ide.TpAmb = (TipoAmbiente)empresa.Configuracao.TipoAmbiente;
            mdfe.InfMDFe.Ide.TpEmit = request.TipoEmitente > 0 ? (MDFeTipoEmitente)request.TipoEmitente : (MDFeTipoEmitente)empresa.Configuracao.TipoEmitentePadrao;
            mdfe.InfMDFe.Ide.Modal = request.Modal > 0 ? (MDFeModal)request.Modal : (MDFeModal)empresa.Configuracao.ModalidadePadrao;
            mdfe.InfMDFe.Ide.TpEmis = request.TipoEmissao > 0 ? (MDFeTipoEmissao)request.TipoEmissao : (MDFeTipoEmissao)empresa.Configuracao.TipoEmissaoPadrao;

            var tpTranspFinal = request.TipoTransportador > 0 ? (MDFeTpTransp)request.TipoTransportador : (MDFeTpTransp)empresa.Configuracao.TipoTransportadorPadrao;
            if (tpTranspFinal > 0) mdfe.InfMDFe.Ide.TpTransp = tpTranspFinal;

            mdfe.InfMDFe.Ide.Mod = ModeloDocumento.MDFe;
            mdfe.InfMDFe.Ide.Serie = (short)empresa.Configuracao.Serie;
            mdfe.InfMDFe.Ide.NMDF = empresa.Configuracao.UltimaNumeracao;
            mdfe.InfMDFe.Ide.CMDF = new Random().Next(11111111, 99999999);
            mdfe.InfMDFe.Ide.DhEmi = DateTime.Now;
            mdfe.InfMDFe.Ide.ProcEmi = MDFeIdentificacaoProcessoEmissao.EmissaoComAplicativoContribuinte;
            mdfe.InfMDFe.Ide.VerProc = "CoreMDFe_1.0";
            mdfe.InfMDFe.Ide.UFIni = ufOrigem;
            mdfe.InfMDFe.Ide.UFFim = ufDestino;

            var carregamentos = request.Documentos.GroupBy(d => new { d.IbgeCarregamento, d.MunicipioCarregamento });
            foreach (var c in carregamentos)
                if (c.Key.IbgeCarregamento > 0)
                    mdfe.InfMDFe.Ide.InfMunCarrega.Add(new MDFeInfMunCarrega { CMunCarrega = c.Key.IbgeCarregamento.ToString(), XMunCarrega = c.Key.MunicipioCarregamento });

            if (request.DataInicioViagem.HasValue) mdfe.InfMDFe.Ide.DhIniViagem = request.DataInicioViagem.Value.DateTime;
            if (request.IsCanalVerde) mdfe.InfMDFe.Ide.IndCanalVerde = "1";
            if (request.IsCarregamentoPosterior) mdfe.InfMDFe.Ide.IndCarregaPosterior = "1";

            if (!string.IsNullOrWhiteSpace(request.UfsPercurso))
            {
                var ufs = request.UfsPercurso.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var uf in ufs)
                    if (Enum.TryParse(uf, out Estado estPercurso))
                        mdfe.InfMDFe.Ide.InfPercurso.Add(new MDFeInfPercurso { UFPer = estPercurso });
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

            #region Responsável Técnico
            string rCnpj = !string.IsNullOrEmpty(request.RespTecCnpj) ? request.RespTecCnpj : empresa.Configuracao.RespTecCnpj;
            string rNome = !string.IsNullOrEmpty(request.RespTecNome) ? request.RespTecNome : empresa.Configuracao.RespTecNome;
            string rFone = !string.IsNullOrEmpty(request.RespTecTelefone) ? request.RespTecTelefone : empresa.Configuracao.RespTecTelefone;
            string rEmail = !string.IsNullOrEmpty(request.RespTecEmail) ? request.RespTecEmail : empresa.Configuracao.RespTecEmail;

            if (!string.IsNullOrWhiteSpace(rCnpj))
            {
                mdfe.InfMDFe.InfRespTec = new MDFeInfRespTec
                {
                    CNPJ = rCnpj.Replace(".", "").Replace("/", "").Replace("-", ""),
                    XContato = rNome,
                    Email = rEmail,
                    Fone = rFone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "")
                };
            }
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
                        InfSeg = new MDFeInfSeg { CNPJ = request.SeguradoraCnpj.Replace(".", "").Replace("/", "").Replace("-", ""), XSeg = request.SeguradoraNome },
                        NApol = request.NumeroApolice,
                        NAver = new List<string> { request.NumeroAverbacao } // Restaurado corretamente aqui
                    }
                };
            }
            #endregion

            #region Totais (Tot)
            mdfe.InfMDFe.Tot.QNFe = request.Documentos.Count(d => d.Tipo == 55);
            mdfe.InfMDFe.Tot.QCTe = request.Documentos.Count(d => d.Tipo == 57);
            if (mdfe.InfMDFe.Tot.QNFe == 0) mdfe.InfMDFe.Tot.QNFe = null;
            if (mdfe.InfMDFe.Tot.QCTe == 0) mdfe.InfMDFe.Tot.QCTe = null;
            mdfe.InfMDFe.Tot.vCarga = request.Documentos.Sum(d => d.Valor);
            mdfe.InfMDFe.Tot.CUnid = MDFeCUnid.KG;
            mdfe.InfMDFe.Tot.QCarga = request.Documentos.Sum(d => d.Peso);
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
                    UfOrigem = mdfe.InfMDFe.Ide.UFIni.ToString(),
                    UfDestino = mdfe.InfMDFe.Ide.UFFim.ToString(),
                    Modalidade = (int)mdfe.InfMDFe.Ide.Modal,
                    TipoAmbiente = empresa.Configuracao.TipoAmbiente,
                    ChaveAcesso = mdfe.InfMDFe.Id?.Length >= 47 ? mdfe.InfMDFe.Id.Substring(4) : "",
                    XmlAssinado = retornoEnvio.EnvioXmlString ?? "",
                    ReciboAutorizacao = retornoEnvio.RetornoXmlString ?? "",
                    ProtocoloAutorizacao = retornoEnvio.ProtMdFe?.InfProt?.NProt ?? "",
                    CodigoStatus = retornoEnvio?.CStat.ToString() ?? "0",
                    MotivoStatus = retornoEnvio?.XMotivo ?? "Sem comunicação"
                };

                historico.Status = historico.CodigoStatus == "100" ? StatusManifesto.Autorizado : StatusManifesto.Rejeitado;
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