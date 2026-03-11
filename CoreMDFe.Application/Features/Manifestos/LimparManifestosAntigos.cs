using CoreMDFe.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreMDFe.Application.Features.Manifestos
{
    // O comando recebe o limite de dias (Padrão 30)
    public record LimparManifestosAntigosCommand(int DiasLimiar = 30) : IRequest<bool>;

    public class LimparManifestosAntigosHandler : IRequestHandler<LimparManifestosAntigosCommand, bool>
    {
        private readonly IAppDbContext _dbContext;

        public LimparManifestosAntigosHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(LimparManifestosAntigosCommand request, CancellationToken cancellationToken)
        {
            Log.Information("[LIMPEZA] Iniciando limpeza dos arquivos/banco de dados...");
            try
            {
                var dataLimite = DateTime.Today.AddDays(-request.DiasLimiar);

                // 1. Limpeza do Banco de Dados
                var manifestosParaExcluir = await _dbContext.Manifestos
                    .Where(m => m.DataEmissao < dataLimite)
                    .ToListAsync(cancellationToken);

                if (manifestosParaExcluir.Any())
                {
                    _dbContext.Manifestos.RemoveRange(manifestosParaExcluir);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                // 2. Limpeza Física dos Arquivos do Disco (XMLs e PDFs)
                var pastaBase = AppDomain.CurrentDomain.BaseDirectory;
                string[] pastasParaLimpar = { "Recibos", "PDFs" };

                foreach (var nomePasta in pastasParaLimpar)
                {
                    var caminhoPasta = Path.Combine(pastaBase, nomePasta);
                    if (Directory.Exists(caminhoPasta))
                    {
                        var arquivos = Directory.GetFiles(caminhoPasta);
                        foreach (var arquivo in arquivos)
                        {
                            // Exclui fisicamente qualquer arquivo mais velho que a data limite
                            if (File.GetCreationTime(arquivo) < dataLimite)
                            {
                                try { File.Delete(arquivo); } catch { /* Ignora caso arquivo esteja em uso no SO */ }
                            }
                        }
                    }
                }

                Log.Information("[LIMPEZA] Limpeza concluída com sucesso!");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[LIMPEZA] Falha ao limpar arquivos/banco: {ex.Message}");
                return false;
            }
        }
    }
}