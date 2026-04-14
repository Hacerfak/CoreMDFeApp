using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using MDFe.Servicos.ConsultaProtocoloMDFe;
using MDFe.Utils.Configuracoes; // Adicionado para acessar a MDFeConfiguracao
using CoreMDFe.Application.Mediator;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreMDFe.Application.Features.Manifestos
{
    // --- COMMAND & RESULT ---
    public record ConsultarSituacaoManifestoCommand(Guid ManifestoId) : IRequest<ConsultarSituacaoManifestoResult>;
    public record ConsultarSituacaoManifestoResult(bool Sucesso, string Mensagem);

    // --- HANDLER ---
    public class ConsultarSituacaoManifestoHandler : IRequestHandler<ConsultarSituacaoManifestoCommand, ConsultarSituacaoManifestoResult>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IMediator _mediator;

        public ConsultarSituacaoManifestoHandler(IAppDbContext dbContext, IMediator mediator)
        {
            _dbContext = dbContext;
            _mediator = mediator;
        }

        public async Task<ConsultarSituacaoManifestoResult> Handle(ConsultarSituacaoManifestoCommand request, CancellationToken cancellationToken)
        {
            // 1. Aplica as configurações e carrega o Certificado da Empresa logada via MediatR
            await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);

            // 2. Busca o manifesto no banco local
            var manifesto = await _dbContext.Manifestos
                .FirstOrDefaultAsync(m => m.Id == request.ManifestoId, cancellationToken);

            if (manifesto == null)
                return new ConsultarSituacaoManifestoResult(false, "Manifesto não encontrado no banco de dados.");

            if (string.IsNullOrEmpty(manifesto.ChaveAcesso))
                return new ConsultarSituacaoManifestoResult(false, "MDF-e não possui Chave de Acesso para consulta na SEFAZ.");

            // Limpa a chave de acesso para evitar rejeições do validador interno
            string chaveLimpa = manifesto.ChaveAcesso.Replace("MDFe", "").Replace("NFe", "").Replace("CTe", "").Trim();

            // =====================================================================
            // WORKAROUND: Bug do Zeus com extração de Eventos na Consulta
            // =====================================================================
            // Salva o estado atual da configuração
            bool salvarXmlOriginal = MDFeConfiguracao.Instancia.IsSalvarXml;

            // DESLIGA o salvamento para impedir que o Zeus tente gerar arquivos
            // dos eventos de Encerramento/Cancelamento (o que causa o erro de Versão)
            MDFeConfiguracao.Instancia.IsSalvarXml = false;

            try
            {
                // 3. Conecta na SEFAZ via Zeus
                var servicoConsulta = new ServicoMDFeConsultaProtocolo();
                var retorno = servicoConsulta.MDFeConsultaProtocolo(chaveLimpa);

                if (retorno == null)
                    return new ConsultarSituacaoManifestoResult(false, "Sem resposta da SEFAZ.");

                // Extrai os dados do retorno oficial da Sefaz
                int cStat = retorno.CStat;
                string xMotivo = retorno.XMotivo ?? "Situação Desconhecida";

                // =========================================================================
                // 4. HEURÍSTICA DE RECUPERAÇÃO: Sincronização automática de Status!
                // Se houver divergência entre a Sefaz e o banco local, arrumamos aqui.
                // =========================================================================
                bool bancoFoiSincronizado = false;

                // 101 = Cancelado na SEFAZ
                if (cStat == 101 && manifesto.Status != StatusManifesto.Cancelado)
                {
                    manifesto.Status = StatusManifesto.Cancelado;

                    if (retorno.ProtMDFe?.InfProt?.NProt != null)
                        manifesto.ProtocoloCancelamento = retorno.ProtMDFe.InfProt.NProt;

                    bancoFoiSincronizado = true;
                }
                // 132 = Encerrado na SEFAZ
                else if (cStat == 132 && manifesto.Status != StatusManifesto.Encerrado)
                {
                    manifesto.Status = StatusManifesto.Encerrado;

                    if (retorno.ProtMDFe?.InfProt?.NProt != null)
                        manifesto.ProtocoloEncerramento = retorno.ProtMDFe.InfProt.NProt;

                    bancoFoiSincronizado = true;
                }
                // 100 = Autorizado na SEFAZ (mas estava pendente no banco)
                else if (cStat == 100 && manifesto.Status != StatusManifesto.Autorizado)
                {
                    manifesto.Status = StatusManifesto.Autorizado;
                    bancoFoiSincronizado = true;
                }

                // Salva a correção no banco de dados se necessário
                if (bancoFoiSincronizado)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return new ConsultarSituacaoManifestoResult(true, $"Status do MDF-e: {cStat} - {xMotivo}\n(ℹ️ O status no sistema estava defasado e foi corrigido automaticamente!)");
                }

                return new ConsultarSituacaoManifestoResult(true, $"Status do MDF-e: {cStat} - {xMotivo}");
            }
            catch (Exception ex)
            {
                string erroDescritivo = ex.Message;
                if (ex.InnerException != null)
                    erroDescritivo += $" | Detalhe: {ex.InnerException.Message}";

                Log.Error($"Erro ao consultar situação do manifesto: {erroDescritivo}");
                return new ConsultarSituacaoManifestoResult(false, $"{erroDescritivo}");
            }
            finally
            {
                // =====================================================================
                // RESTAURA a configuração para não quebrar a emissão de novos manifestos!
                // =====================================================================
                MDFeConfiguracao.Instancia.IsSalvarXml = salvarXmlOriginal;
            }
        }
    }
}