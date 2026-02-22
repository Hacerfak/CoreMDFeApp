using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Core.Interfaces;
using MDFe.Servicos.ConsultaNaoEncerradosMDFe;

namespace CoreMDFe.Application.Features.Consultas
{
    public record ConsultarNaoEncerradosQuery(Guid EmpresaId) : IRequest<ConsultarNaoEncerradosResult>;

    public record ManifestoNaoEncerradoDto(string ChaveAcesso, string Protocolo);
    public record ConsultarNaoEncerradosResult(bool Sucesso, string Mensagem, List<ManifestoNaoEncerradoDto> ManifestosPendentes);

    public class ConsultarNaoEncerradosHandler : IRequestHandler<ConsultarNaoEncerradosQuery, ConsultarNaoEncerradosResult>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IMediator _mediator;

        public ConsultarNaoEncerradosHandler(IAppDbContext dbContext, IMediator mediator)
        {
            _dbContext = dbContext;
            _mediator = mediator;
        }

        public async Task<ConsultarNaoEncerradosResult> Handle(ConsultarNaoEncerradosQuery request, CancellationToken cancellationToken)
        {
            await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);

            try
            {

                var configAplicada = await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);
                if (!configAplicada) return new ConsultarNaoEncerradosResult(false, "Configure o certificado antes de consultar.", new List<ManifestoNaoEncerradoDto>());

                // Como o AppDbContext é Transient e isolado por Tenant, podemos pegar a primeira empresa sem medo!
                var empresa = await _dbContext.Empresas.FirstOrDefaultAsync(cancellationToken);
                if (empresa == null) return new ConsultarNaoEncerradosResult(false, "Empresa não encontrada no banco isolado.", new List<ManifestoNaoEncerradoDto>());

                var servicoNaoEncerrados = new ServicoMDFeConsultaNaoEncerrados();
                var retorno = servicoNaoEncerrados.MDFeConsultaNaoEncerrados(empresa.Cnpj);

                var lista = new List<ManifestoNaoEncerradoDto>();

                // A propriedade InfMDFe vem nula se não houver pendentes
                if (retorno.CStat == 111 && retorno.InfMDFe != null)
                {
                    lista.AddRange(retorno.InfMDFe.Select(x => new ManifestoNaoEncerradoDto(x.ChMDFe, x.NProt)));
                }

                return new ConsultarNaoEncerradosResult(true, retorno.XMotivo ?? "", lista);
            }
            catch (Exception ex)
            {
                return new ConsultarNaoEncerradosResult(false, ex.Message, new List<ManifestoNaoEncerradoDto>());
            }
        }
    }
}