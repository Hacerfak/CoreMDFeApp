using CoreMDFe.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace CoreMDFe.Core.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<Empresa> Empresas { get; }
        DbSet<ConfiguracaoApp> Configuracoes { get; }
        DbSet<Condutor> Condutores { get; }
        DbSet<Veiculo> Veiculos { get; }
        DbSet<ManifestoEletronico> Manifestos { get; }

        // --- NOVAS TABELAS DO MDF-e ---
        DbSet<ManifestoPercurso> ManifestoPercursos { get; }
        DbSet<ManifestoMunicipioCarregamento> ManifestoMunicipiosCarregamento { get; }
        DbSet<ManifestoMunicipioDescarregamento> ManifestoMunicipiosDescarregamento { get; }
        DbSet<ManifestoDocumentoFiscal> ManifestoDocumentosFiscais { get; }
        DbSet<ManifestoVeiculo> ManifestoVeiculos { get; }
        DbSet<ManifestoCondutor> ManifestoCondutores { get; }
        DbSet<ManifestoCiot> ManifestoCiots { get; }
        DbSet<ManifestoValePedagio> ManifestoValesPedagio { get; }
        DbSet<ManifestoContratante> ManifestoContratantes { get; }
        DbSet<ManifestoSeguro> ManifestoSeguros { get; }
        DbSet<ManifestoPagamento> ManifestoPagamentos { get; }
        DbSet<ManifestoAutorizadoDownload> ManifestoAutorizadosDownload { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}