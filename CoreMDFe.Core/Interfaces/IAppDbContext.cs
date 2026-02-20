using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Core.Entities;

namespace CoreMDFe.Core.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<Empresa> Empresas { get; }
        DbSet<ConfiguracaoApp> Configuracoes { get; }
        DbSet<Condutor> Condutores { get; }
        DbSet<Veiculo> Veiculos { get; }
        DbSet<ManifestoEletronico> Manifestos { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}