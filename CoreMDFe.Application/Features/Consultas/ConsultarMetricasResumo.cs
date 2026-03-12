using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreMDFe.Application.Features.Consultas
{
    // Agora sem parâmetros, assume automaticamente o mês atual
    public record ConsultarMetricasResumoQuery() : IRequest<MetricasResumoDto>;

    public class EstatisticaDiariaDto
    {
        public string Data { get; set; } = string.Empty;
        public int QuantidadeManifestos { get; set; }
        public int TotalDFes { get; set; }
        public decimal TotalPeso { get; set; }
        public decimal TotalValor { get; set; }
    }

    public class RankingCidadeDto
    {
        public string Cidade { get; set; } = string.Empty;
        public int QuantidadeDescargas { get; set; }
    }

    public class MetricasResumoDto
    {
        public List<EstatisticaDiariaDto> EvolucaoDiaria { get; set; } = new();
        public int TotalDFesPeriodo { get; set; }
        public decimal TotalPesoPeriodo { get; set; }
        public decimal TotalValorPeriodo { get; set; }
        public double MediaDiariaDFes { get; set; }
        public decimal MediaDiariaPeso { get; set; }
        public decimal MediaDiariaValor { get; set; }
        public List<RankingCidadeDto> CidadesDescarregamento { get; set; } = new();
    }

    public class ConsultarMetricasResumoHandler : IRequestHandler<ConsultarMetricasResumoQuery, MetricasResumoDto>
    {
        private readonly IAppDbContext _dbContext;

        public ConsultarMetricasResumoHandler(IAppDbContext dbContext) => _dbContext = dbContext;

        public async Task<MetricasResumoDto> Handle(ConsultarMetricasResumoQuery request, CancellationToken cancellationToken)
        {
            var hoje = DateTime.Today;

            // Define o intervalo: Do dia 1º até o dia de HOJE às 23:59:59
            var dataInicio = new DateTime(hoje.Year, hoje.Month, 1);
            var dataFim = hoje.AddDays(1).AddTicks(-1);

            // Conta quantos dias do mês já se passaram
            int diasDecorridos = hoje.Day;

            // Busca no banco apenas os manifestos do dia 1 até hoje
            var manifestos = await _dbContext.Manifestos
                .Include(m => m.MunicipiosDescarregamento)
                    .ThenInclude(md => md.Documentos)
                .AsNoTracking()
                .Where(m => m.DataEmissao >= dataInicio && m.DataEmissao <= dataFim && m.Status != StatusManifesto.Cancelado && m.Status != StatusManifesto.Rejeitado)
                .ToListAsync(cancellationToken);

            var dto = new MetricasResumoDto();

            // Roda o loop APENAS para os dias decorridos (Ex: do dia 1 ao dia 12)
            for (int i = 1; i <= diasDecorridos; i++)
            {
                var diaAlvo = new DateTime(hoje.Year, hoje.Month, i);
                var manifestosDoDia = manifestos.Where(m => m.DataEmissao.Date == diaAlvo).ToList();

                dto.EvolucaoDiaria.Add(new EstatisticaDiariaDto
                {
                    Data = diaAlvo.ToString("dd/MM"),
                    QuantidadeManifestos = manifestosDoDia.Count,
                    TotalDFes = manifestosDoDia.Sum(m => m.MunicipiosDescarregamento.Sum(md => md.Documentos.Count)),
                    TotalPeso = manifestosDoDia.Sum(m => m.PesoTotalCarga),
                    TotalValor = manifestosDoDia.Sum(m => m.ValorTotalCarga)
                });
            }

            dto.TotalDFesPeriodo = dto.EvolucaoDiaria.Sum(d => d.TotalDFes);
            dto.TotalPesoPeriodo = dto.EvolucaoDiaria.Sum(d => d.TotalPeso);
            dto.TotalValorPeriodo = dto.EvolucaoDiaria.Sum(d => d.TotalValor);

            // Calcula a média exata dividindo pelos dias que já passaram
            dto.MediaDiariaDFes = (double)dto.TotalDFesPeriodo / diasDecorridos;
            dto.MediaDiariaPeso = dto.TotalPesoPeriodo / diasDecorridos;
            dto.MediaDiariaValor = dto.TotalValorPeriodo / diasDecorridos;

            dto.CidadesDescarregamento = manifestos
                .SelectMany(m => m.MunicipiosDescarregamento)
                .Where(md => !string.IsNullOrWhiteSpace(md.NomeMunicipio))
                .GroupBy(md => md.NomeMunicipio)
                .Select(g => new RankingCidadeDto { Cidade = g.Key, QuantidadeDescargas = g.Count() })
                .OrderByDescending(r => r.QuantidadeDescargas)
                .Take(10).ToList();

            return dto;
        }
    }
}