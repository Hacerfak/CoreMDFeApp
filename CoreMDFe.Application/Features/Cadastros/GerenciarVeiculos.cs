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
    public record ListarVeiculosQuery() : IRequest<List<Veiculo>>;

    // Commands
    public record SalvarVeiculoCommand(Veiculo Veiculo) : IRequest<bool>;
    public record ExcluirVeiculoCommand(Guid Id) : IRequest<bool>;

    public class GerenciarVeiculosHandler :
        IRequestHandler<ListarVeiculosQuery, List<Veiculo>>,
        IRequestHandler<SalvarVeiculoCommand, bool>,
        IRequestHandler<ExcluirVeiculoCommand, bool>
    {
        private readonly IAppDbContext _dbContext;

        public GerenciarVeiculosHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Veiculo>> Handle(ListarVeiculosQuery request, CancellationToken cancellationToken)
        {
            return await _dbContext.Veiculos.ToListAsync(cancellationToken);
        }

        public async Task<bool> Handle(SalvarVeiculoCommand request, CancellationToken cancellationToken)
        {
            if (request.Veiculo.Id == Guid.Empty)
            {
                request.Veiculo.Id = Guid.NewGuid();
                _dbContext.Veiculos.Add(request.Veiculo);
            }
            else
            {
                _dbContext.Veiculos.Update(request.Veiculo);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> Handle(ExcluirVeiculoCommand request, CancellationToken cancellationToken)
        {
            var veiculo = await _dbContext.Veiculos.FindAsync(new object[] { request.Id }, cancellationToken);
            if (veiculo != null)
            {
                _dbContext.Veiculos.Remove(veiculo);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
    }
}