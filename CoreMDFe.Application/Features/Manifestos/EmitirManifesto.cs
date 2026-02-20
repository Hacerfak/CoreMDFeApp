using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using MDFe.Classes.Extensoes;
using MDFe.Classes.Flags;
using MDFe.Classes.Informacoes;
using MDFe.Classes.Informacoes.Evento.CorpoEvento;
using MDFe.Classes.Retorno;
using MDFe.Classes.Servicos.Autorizacao;
using MDFe.Servicos.ConsultaNaoEncerradosMDFe;
using MDFe.Servicos.ConsultaProtocoloMDFe;
using MDFe.Servicos.EventosMDFe;
using MDFe.Servicos.RecepcaoMDFe;
using MDFe.Servicos.RetRecepcaoMDFe;
using MDFe.Servicos.StatusServicoMDFe;
using MDFe.Utils.Configuracoes;
using MDFeEletronico = MDFe.Classes.Informacoes.MDFe;

namespace CoreMDFe.Application.Features.Manifestos
{
    // O Command recebe os dados básicos para emissão (pode ser expandido conforme a sua UI for crescendo)
    public record EmitirManifestoCommand(Guid EmpresaId) : IRequest<EmitirManifestoResult>;

    // DTO de resposta para a UI
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
            // 1. Aplica as configurações do Zeus na memória (Certificado, Schemas, etc)
            var configAplicada = await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);
            if (!configAplicada)
                return new EmitirManifestoResult(false, "Falha ao carregar configurações da empresa.", "", "");

            // 2. Busca a empresa no banco de dados
            var empresa = await _dbContext.Empresas
                .Include(e => e.Configuracao)
                .FirstOrDefaultAsync(e => e.Id == request.EmpresaId, cancellationToken);

            if (empresa == null || empresa.Configuracao == null)
                return new EmitirManifestoResult(false, "Empresa ou configurações não encontradas.", "", "");

            // 3. Incrementa a numeração (NSU)
            empresa.Configuracao.UltimaNumeracao++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 4. Monta o MDF-e (Aqui você substituirá pelos dados que virão da UI no Command, deixei os fixos para exemplo)
            var mdfe = new MDFeEletronico();

            #region Identificação (Ide)
            // Tenta converter a string da UF salva no banco para o Enum Estado do Zeus
            _ = Enum.TryParse(empresa.Configuracao.UfEmitente, out Estado ufEmitente);

            mdfe.InfMDFe.Ide.CUF = ufEmitente;
            mdfe.InfMDFe.Ide.TpAmb = (TipoAmbiente)empresa.Configuracao.TipoAmbiente;
            mdfe.InfMDFe.Ide.TpEmit = MDFeTipoEmitente.PrestadorServicoDeTransporte;
            mdfe.InfMDFe.Ide.Mod = ModeloDocumento.MDFe;
            mdfe.InfMDFe.Ide.Serie = (short)empresa.Configuracao.Serie;
            mdfe.InfMDFe.Ide.NMDF = empresa.Configuracao.UltimaNumeracao;
            mdfe.InfMDFe.Ide.CMDF = new Random().Next(11111111, 99999999);
            mdfe.InfMDFe.Ide.Modal = MDFeModal.Rodoviario;
            mdfe.InfMDFe.Ide.DhEmi = DateTime.Now;
            mdfe.InfMDFe.Ide.TpEmis = MDFeTipoEmissao.Normal;
            mdfe.InfMDFe.Ide.ProcEmi = MDFeIdentificacaoProcessoEmissao.EmissaoComAplicativoContribuinte;
            mdfe.InfMDFe.Ide.VerProc = "CoreMDFe_1.0";
            mdfe.InfMDFe.Ide.UFIni = Estado.GO;
            mdfe.InfMDFe.Ide.UFFim = Estado.MT;

            mdfe.InfMDFe.Ide.InfMunCarrega.Add(new MDFeInfMunCarrega { CMunCarrega = "5211701", XMunCarrega = "JANDAIA" });
            #endregion

            #region Emitente (Emit)
            _ = Enum.TryParse(empresa.SiglaUf, out Estado ufEmpresa);

            mdfe.InfMDFe.Emit.CNPJ = empresa.Cnpj;
            mdfe.InfMDFe.Emit.IE = empresa.InscricaoEstadual;
            mdfe.InfMDFe.Emit.XNome = empresa.Nome;
            mdfe.InfMDFe.Emit.XFant = empresa.NomeFantasia;

            mdfe.InfMDFe.Emit.EnderEmit.XLgr = empresa.Logradouro;
            mdfe.InfMDFe.Emit.EnderEmit.Nro = empresa.Numero;
            mdfe.InfMDFe.Emit.EnderEmit.XCpl = empresa.Complemento;
            mdfe.InfMDFe.Emit.EnderEmit.XBairro = empresa.Bairro;
            mdfe.InfMDFe.Emit.EnderEmit.CMun = empresa.CodigoIbgeMunicipio;
            mdfe.InfMDFe.Emit.EnderEmit.XMun = empresa.NomeMunicipio;
            mdfe.InfMDFe.Emit.EnderEmit.CEP = long.Parse(empresa.Cep.Replace("-", ""));
            mdfe.InfMDFe.Emit.EnderEmit.UF = ufEmpresa;
            mdfe.InfMDFe.Emit.EnderEmit.Fone = empresa.Telefone;
            mdfe.InfMDFe.Emit.EnderEmit.Email = empresa.Email;
            #endregion

            #region Modal Rodoviário
            mdfe.InfMDFe.InfModal.VersaoModal = MDFeVersaoModal.Versao300;
            mdfe.InfMDFe.InfModal.Modal = new MDFeRodo
            {
                InfANTT = new MDFeInfANTT { RNTRC = empresa.RNTRC },
                VeicTracao = new MDFeVeicTracao
                {
                    Placa = "KKK9888",
                    RENAVAM = "888888888",
                    UF = Estado.GO,
                    Tara = 222,
                    CapM3 = 222,
                    CapKG = 22,
                    Condutor = new List<MDFeCondutor> { new MDFeCondutor { CPF = "11392381754", XNome = "Motorista Teste" } },
                    TpRod = MDFeTpRod.Outros,
                    TpCar = MDFeTpCar.NaoAplicavel
                }
            };
            #endregion

            #region Documentos (InfDoc) e Totais
            mdfe.InfMDFe.InfDoc.InfMunDescarga = new List<MDFeInfMunDescarga>
            {
                new MDFeInfMunDescarga
                {
                    XMunDescarga = "CUIABA",
                    CMunDescarga = "5103403",
                    InfCTe = new List<MDFeInfCTe> { new MDFeInfCTe { ChCTe = "52161021351378000100577500000000191194518006" } }
                }
            };

            mdfe.InfMDFe.Tot.QCTe = 1;
            mdfe.InfMDFe.Tot.vCarga = 500.00m;
            mdfe.InfMDFe.Tot.CUnid = MDFeCUnid.KG;
            mdfe.InfMDFe.Tot.QCarga = 100.0000m;
            #endregion

            // 5. Envio
            try
            {
                var servicoRecepcao = new ServicoMDFeRecepcao();
                var retornoEnvio = servicoRecepcao.MDFeRecepcaoSinc(mdfe);

                // 6. Salvar Histórico no Banco de Dados
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
                    ChaveAcesso = mdfe.InfMDFe.Id.Substring(4), // Remove "MDFe" da frente
                    XmlAssinado = retornoEnvio.EnvioXmlString,
                    XmlAutorizado = retornoEnvio.RetornoXmlString,
                    CodigoStatus = retornoEnvio.ProtMdFe.InfProt.CStat.ToString(),
                    MotivoStatus = retornoEnvio.ProtMdFe.InfProt.XMotivo ?? "Sem motivo retornado",
                    ProtocoloAutorizacao = retornoEnvio.ProtMdFe.InfProt.NProt ?? string.Empty
                };

                // Define o status interno baseado no retorno da Sefaz (100 = Autorizado)
                historico.Status = historico.CodigoStatus == "100" ? StatusManifesto.Autorizado : StatusManifesto.Rejeitado;

                _dbContext.Manifestos.Add(historico);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new EmitirManifestoResult(
                    Sucesso: historico.Status == StatusManifesto.Autorizado,
                    Mensagem: historico.MotivoStatus,
                    XmlEnvio: retornoEnvio.EnvioXmlString,
                    XmlRetorno: retornoEnvio.RetornoXmlString
                );
            }
            catch (Exception ex)
            {
                return new EmitirManifestoResult(false, $"Erro ao emitir: {ex.Message}", "", "");
            }
        }
    }
}