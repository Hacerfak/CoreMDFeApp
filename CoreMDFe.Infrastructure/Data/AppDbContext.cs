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

        // Construtor vazio necessário para o Design-Time (Migrations)
        public AppDbContext() { }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Define o arquivo do banco de dados SQLite que será gerado na pasta do executável
                optionsBuilder.UseSqlite("Data Source=core_mdfe_app.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relacionamento 1 para 1: Empresa <-> ConfiguracaoApp
            modelBuilder.Entity<Empresa>()
                .HasOne(e => e.Configuracao)
                .WithOne(c => c.Empresa)
                .HasForeignKey<ConfiguracaoApp>(c => c.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacionamento 1 para N: Empresa <-> Manifestos
            modelBuilder.Entity<ManifestoEletronico>()
                .HasOne(m => m.Empresa)
                .WithMany()
                .HasForeignKey(m => m.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurações padrão adicionais (opcional)
            modelBuilder.Entity<ConfiguracaoApp>()
                .Property(c => c.TimeOut)
                .HasDefaultValue(5000);
        }
    }
}