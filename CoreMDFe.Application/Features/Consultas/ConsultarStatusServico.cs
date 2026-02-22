using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using MDFe.Servicos.StatusServicoMDFe; // Namespace oficial do Zeus para Status

namespace CoreMDFe.Application.Features.Consultas
{
    public record ConsultarStatusServicoQuery() : IRequest<ConsultarStatusServicoResult>;

    public record ConsultarStatusServicoResult(bool Online, string Mensagem, string TempoMedio);

    public class ConsultarStatusServicoHandler : IRequestHandler<ConsultarStatusServicoQuery, ConsultarStatusServicoResult>
    {
        private readonly IMediator _mediator;

        // Injetamos o Mediator para podermos chamar o configurador do Zeus
        public ConsultarStatusServicoHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<ConsultarStatusServicoResult> Handle(ConsultarStatusServicoQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Aplica as configurações e carrega o Certificado da Empresa logada ANTES de consultar!
                var configAplicada = await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);

                if (!configAplicada)
                    return new ConsultarStatusServicoResult(false, "Configuração do ambiente ou certificado não encontrado para esta empresa.", "");

                // 2. Conecta na Sefaz
                var servicoStatus = new ServicoMDFeStatusServico();
                var retorno = servicoStatus.MDFeStatusServico();

                // 107 = Serviço em Operação (Código de Sucesso Padrão da SEFAZ)
                if (retorno != null && retorno != null && retorno.CStat == 107)
                {
                    return new ConsultarStatusServicoResult(true, retorno.XMotivo, retorno.TMed.ToString() ?? "0");
                }

                return new ConsultarStatusServicoResult(false, retorno?.XMotivo ?? "Serviço Inativo ou Falha de Comunicação", "");
            }
            catch (Exception ex)
            {
                string erroDescritivo = ex.Message;
                if (ex.InnerException != null)
                    erroDescritivo += $" | Detalhe: {ex.InnerException.Message}";

                return new ConsultarStatusServicoResult(false, $"Falha na conexão SEFAZ: {erroDescritivo}", "0");

                // Heurística de Prevenção/Recuperação de erro: Devolvemos o erro limpo para a UI
                //return new ConsultarStatusServicoResult(false, $"Erro ao consultar SEFAZ: {ex.Message}", "");
            }
        }
    }
}