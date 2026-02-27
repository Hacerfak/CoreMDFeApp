using Microsoft.EntityFrameworkCore;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;

namespace CoreMDFe.Infrastructure.Data
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<ConfiguracaoApp> Configuracoes { get; set; }
        public DbSet<Condutor> Condutores { get; set; }
        public DbSet<Veiculo> Veiculos { get; set; }
        public DbSet<ManifestoEletronico> Manifestos { get; set; }

        // --- NOVAS TABELAS DO MDF-e ---
        public DbSet<ManifestoPercurso> ManifestoPercursos { get; set; }
        public DbSet<ManifestoMunicipioCarregamento> ManifestoMunicipiosCarregamento { get; set; }
        public DbSet<ManifestoMunicipioDescarregamento> ManifestoMunicipiosDescarregamento { get; set; }
        public DbSet<ManifestoDocumentoFiscal> ManifestoDocumentosFiscais { get; set; }
        public DbSet<ManifestoVeiculo> ManifestoVeiculos { get; set; }
        public DbSet<ManifestoCondutor> ManifestoCondutores { get; set; }
        public DbSet<ManifestoCiot> ManifestoCiots { get; set; }
        public DbSet<ManifestoValePedagio> ManifestoValesPedagio { get; set; }
        public DbSet<ManifestoContratante> ManifestoContratantes { get; set; }
        public DbSet<ManifestoSeguro> ManifestoSeguros { get; set; }
        public DbSet<ManifestoPagamento> ManifestoPagamentos { get; set; }
        public DbSet<ManifestoAutorizadoDownload> ManifestoAutorizadosDownload { get; set; }

        private readonly string _databasePath;

        // Construtor para uso em tempo de execução (Dinâmico por Empresa / Tenant)
        public AppDbContext(string databasePath)
        {
            _databasePath = databasePath;
        }

        // Construtor vazio necessário apenas para as Migrations (Design-Time)
        public AppDbContext()
        {
            _databasePath = "design_time.db";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={_databasePath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeamentos originais que você já tinha
            modelBuilder.Entity<Empresa>()
                .HasOne(e => e.Configuracao)
                .WithOne(c => c.Empresa)
                .HasForeignKey<ConfiguracaoApp>(c => c.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManifestoEletronico>()
                .HasOne(m => m.Empresa)
                .WithMany()
                .HasForeignKey(m => m.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConfiguracaoApp>()
                .Property(c => c.TimeOut)
                .HasDefaultValue(5000);

            // --- GARANTIA DE EXCLUSÃO EM CASCATA PARA AS NOVAS TABELAS DO MDF-E ---
            // Isso garante que se um Manifesto for excluído, todas as suas listas internas somem do banco.
            modelBuilder.Entity<ManifestoEletronico>()
                .HasMany(m => m.Percursos).WithOne(p => p.Manifesto).HasForeignKey(p => p.ManifestoId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManifestoEletronico>()
                .HasMany(m => m.MunicipiosCarregamento).WithOne(m => m.Manifesto).HasForeignKey(m => m.ManifestoId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManifestoEletronico>()
                .HasMany(m => m.MunicipiosDescarregamento).WithOne(m => m.Manifesto).HasForeignKey(m => m.ManifestoId).OnDelete(DeleteBehavior.Cascade);

            // Notas fiscais pertencem ao município de descarregamento
            modelBuilder.Entity<ManifestoMunicipioDescarregamento>()
                .HasMany(m => m.Documentos).WithOne(d => d.Municipio).HasForeignKey(d => d.MunicipioDescarregamentoId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManifestoEletronico>()
                .HasMany(m => m.Veiculos).WithOne(v => v.Manifesto).HasForeignKey(v => v.ManifestoId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManifestoEletronico>()
                .HasMany(m => m.Condutores).WithOne(c => c.Manifesto).HasForeignKey(c => c.ManifestoId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManifestoEletronico>()
                .HasMany(m => m.Ciots).WithOne(c => c.Manifesto).HasForeignKey(c => c.ManifestoId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManifestoEletronico>()
                .HasMany(m => m.ValesPedagio).WithOne(v => v.Manifesto).HasForeignKey(v => v.ManifestoId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManifestoEletronico>()
                .HasMany(m => m.Contratantes).WithOne(c => c.Manifesto).HasForeignKey(c => c.ManifestoId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManifestoEletronico>()
                .HasMany(m => m.Seguros).WithOne(s => s.Manifesto).HasForeignKey(s => s.ManifestoId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManifestoEletronico>()
                .HasMany(m => m.Pagamentos).WithOne(p => p.Manifesto).HasForeignKey(p => p.ManifestoId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManifestoEletronico>()
                .HasMany(m => m.AutorizadosDownload).WithOne(a => a.Manifesto).HasForeignKey(a => a.ManifestoId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}