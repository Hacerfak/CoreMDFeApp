using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreMDFe.Application.Features.Consultas
{
    public record ConsultarEstatisticasResumoQuery() : IRequest<ResumoEstatisticasDto>;

    public class ResumoEstatisticasDto
    {
        public int TotalMesAtual { get; set; }
        public int Autorizados { get; set; }
        public int Encerrados { get; set; }
        public int Cancelados { get; set; }
        public int Rejeitados { get; set; }
        public int EmAberto { get; set; } // Autorizados que AINDA NÃO foram Encerrados
    }

    public class ConsultarEstatisticasResumoHandler : IRequestHandler<ConsultarEstatisticasResumoQuery, ResumoEstatisticasDto>
    {
        private readonly IAppDbContext _dbContext;

        public ConsultarEstatisticasResumoHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ResumoEstatisticasDto> Handle(ConsultarEstatisticasResumoQuery request, CancellationToken cancellationToken)
        {
            var dataInicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // Pega apenas os manifestos do mês atual
            var manifestosDoMes = await _dbContext.Manifestos
                .Where(m => m.DataEmissao >= dataInicioMes)
                .ToListAsync(cancellationToken);

            var dto = new ResumoEstatisticasDto
            {
                TotalMesAtual = manifestosDoMes.Count,
                Autorizados = manifestosDoMes.Count(m => m.Status == StatusManifesto.Autorizado),
                Encerrados = manifestosDoMes.Count(m => m.Status == StatusManifesto.Encerrado),
                Cancelados = manifestosDoMes.Count(m => m.Status == StatusManifesto.Cancelado),
                Rejeitados = manifestosDoMes.Count(m => m.Status == StatusManifesto.Rejeitado),
            };

            // "Em Aberto" são todos os que estão no status Autorizado (independente do mês de emissão)
            dto.EmAberto = await _dbContext.Manifestos
                .CountAsync(m => m.Status == StatusManifesto.Autorizado, cancellationToken);

            return dto;
        }
    }
}