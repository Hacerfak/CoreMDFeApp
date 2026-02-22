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

        private readonly string _databasePath;

        // Construtor para uso em tempo de execução (Dinâmico por Empresa)
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
        }
    }
}