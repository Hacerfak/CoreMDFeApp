using MediatR;
using Microsoft.EntityFrameworkCore;
using CoreMDFe.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreMDFe.Application.Features.Manifestos
{
    public record ExcluirManifestoCommand(Guid Id) : IRequest<bool>;

    public class ExcluirManifestoHandler : IRequestHandler<ExcluirManifestoCommand, bool>
    {
        private readonly IAppDbContext _dbContext;
        public ExcluirManifestoHandler(IAppDbContext dbContext) => _dbContext = dbContext;

        public async Task<bool> Handle(ExcluirManifestoCommand request, CancellationToken cancellationToken)
        {
            var manifesto = await _dbContext.Manifestos.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
            if (manifesto == null) return false;

            _dbContext.Manifestos.Remove(manifesto);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}