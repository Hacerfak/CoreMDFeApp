using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;

namespace CoreMDFe.Application.Features.Configuracoes
{
    // O Request (Command): Carrega os dados vindos da tela (Avalonia)
    public record SalvarConfiguracaoCommand(
        string Cnpj, string Nome, string Fantasia, string Ie, string Rntrc,
        string CaminhoCertificado, string SenhaCertificado, bool ManterCertificadoCache,
        int TipoAmbiente, string UfEmitente, int VersaoLayout, int TimeOut
    ) : IRequest<bool>;

    // O Handler: Executa a lógica de negócio
    public class SalvarConfiguracaoHandler : IRequestHandler<SalvarConfiguracaoCommand, bool>
    {
        private readonly IAppDbContext _dbContext;

        // Injeção de dependência
        public SalvarConfiguracaoHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(SalvarConfiguracaoCommand request, CancellationToken cancellationToken)
        {
            // Pega a primeira empresa/configuração ou cria uma nova (estamos assumindo 1 empresa para simplificar, como no seu teste)
            var empresa = await _dbContext.Empresas
                .Include(e => e.Configuracao)
                .FirstOrDefaultAsync(cancellationToken);

            if (empresa == null)
            {
                empresa = new Empresa { Configuracao = new ConfiguracaoApp() };
                _dbContext.Empresas.Add(empresa);
            }

            // Mapeamento dos dados
            empresa.Cnpj = request.Cnpj;
            empresa.Nome = request.Nome;
            empresa.NomeFantasia = request.Fantasia;
            empresa.InscricaoEstadual = request.Ie;
            empresa.RNTRC = request.Rntrc;

            empresa.Configuracao!.CaminhoArquivoCertificado = request.CaminhoCertificado;
            empresa.Configuracao.SenhaCertificado = request.SenhaCertificado;
            empresa.Configuracao.ManterCertificadoEmCache = request.ManterCertificadoCache;

            empresa.Configuracao.TipoAmbiente = request.TipoAmbiente;
            empresa.Configuracao.UfEmitente = request.UfEmitente;
            empresa.Configuracao.VersaoLayout = request.VersaoLayout;
            empresa.Configuracao.TimeOut = request.TimeOut;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}