using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;

namespace CoreMDFe.Application.Features.Manifestos
{
    // Adicionamos os parâmetros de data no Request
    public record ListarManifestosQuery(DateTime DataInicio, DateTime DataFim) : IRequest<List<ManifestoEletronico>>;

    public class ListarManifestosHandler : IRequestHandler<ListarManifestosQuery, List<ManifestoEletronico>>
    {
        private readonly IAppDbContext _dbContext;

        public ListarManifestosHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ManifestoEletronico>> Handle(ListarManifestosQuery request, CancellationToken cancellationToken)
        {
            // Ajustamos a data final para incluir todo o dia até às 23:59:59
            var dataFimAjustada = request.DataFim.Date.AddDays(1).AddTicks(-1);

            return await _dbContext.Manifestos
                .Where(m => m.DataEmissao >= request.DataInicio.Date && m.DataEmissao <= dataFimAjustada)
                .OrderByDescending(m => m.DataEmissao)
                .ToListAsync(cancellationToken);
        }
    }
}