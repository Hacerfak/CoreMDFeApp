using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;

namespace CoreMDFe.Application.Features.Manifestos
{
    public record ListarManifestosQuery() : IRequest<List<ManifestoEletronico>>;

    public class ListarManifestosHandler : IRequestHandler<ListarManifestosQuery, List<ManifestoEletronico>>
    {
        private readonly IAppDbContext _dbContext;

        public ListarManifestosHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ManifestoEletronico>> Handle(ListarManifestosQuery request, CancellationToken cancellationToken)
        {
            // Retorna os manifestos ordenados pelo mais recente
            return await _dbContext.Manifestos
                .OrderByDescending(m => m.DataEmissao)
                .ToListAsync(cancellationToken);
        }
    }
}