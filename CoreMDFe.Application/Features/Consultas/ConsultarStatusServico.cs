using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using MDFe.Servicos.StatusServicoMDFe;

namespace CoreMDFe.Application.Features.Consultas
{
    public record ConsultarStatusServicoQuery() : IRequest<ConsultarStatusServicoResult>;

    public record ConsultarStatusServicoResult(bool Online, string Mensagem, string TempoMedio, string XmlRetorno);

    public class ConsultarStatusServicoHandler : IRequestHandler<ConsultarStatusServicoQuery, ConsultarStatusServicoResult>
    {
        private readonly IMediator _mediator;

        public ConsultarStatusServicoHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<ConsultarStatusServicoResult> Handle(ConsultarStatusServicoQuery request, CancellationToken cancellationToken)
        {
            // Garante que o Zeus sabe qual UF e Ambiente consultar
            var configAplicada = await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);
            if (!configAplicada)
                return new ConsultarStatusServicoResult(false, "Configuração não encontrada", "", "");

            try
            {
                var servicoStatus = new ServicoMDFeStatusServico();
                var retorno = servicoStatus.MDFeStatusServico();

                var isOnline = retorno.CStat == 107; // 107 = Serviço em Operação

                return new ConsultarStatusServicoResult(
                    Online: isOnline,
                    Mensagem: retorno.XMotivo ?? "Sem resposta",
                    TempoMedio: retorno.TMed.ToString() + "s",
                    XmlRetorno: retorno.RetornoXmlString
                );
            }
            catch (Exception ex)
            {
                return new ConsultarStatusServicoResult(false, $"Erro na comunicação: {ex.Message}", "", "");
            }
        }
    }
}