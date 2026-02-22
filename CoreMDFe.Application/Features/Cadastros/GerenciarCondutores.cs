using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;

namespace CoreMDFe.Application.Features.Cadastros
{
    // Queries
    public record ListarCondutoresQuery() : IRequest<List<Condutor>>;

    // Commands
    public record SalvarCondutorCommand(Condutor Condutor) : IRequest<bool>;
    public record ExcluirCondutorCommand(Guid Id) : IRequest<bool>;

    public class GerenciarCondutoresHandler :
        IRequestHandler<ListarCondutoresQuery, List<Condutor>>,
        IRequestHandler<SalvarCondutorCommand, bool>,
        IRequestHandler<ExcluirCondutorCommand, bool>
    {
        private readonly IAppDbContext _dbContext;

        public GerenciarCondutoresHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Condutor>> Handle(ListarCondutoresQuery request, CancellationToken cancellationToken)
        {
            return await _dbContext.Condutores.ToListAsync(cancellationToken);
        }

        public async Task<bool> Handle(SalvarCondutorCommand request, CancellationToken cancellationToken)
        {
            if (request.Condutor.Id == Guid.Empty)
            {
                request.Condutor.Id = Guid.NewGuid();
                _dbContext.Condutores.Add(request.Condutor);
            }
            else
            {
                _dbContext.Condutores.Update(request.Condutor);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> Handle(ExcluirCondutorCommand request, CancellationToken cancellationToken)
        {
            var condutor = await _dbContext.Condutores.FindAsync(new object[] { request.Id }, cancellationToken);
            if (condutor != null)
            {
                _dbContext.Condutores.Remove(condutor);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
    }
}