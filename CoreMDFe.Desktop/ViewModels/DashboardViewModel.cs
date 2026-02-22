using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using CoreMDFe.Application.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace CoreMDFe.Desktop.ViewModels
{
    public enum Page
    {
        Dashboard, Emissao, Veiculos, Condutores, Historico, Configuracoes
    }

    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IAppDbContext _dbContext;
        private readonly CurrentTenantService _tenantService;

        // Controle de Abas
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDashboard))]
        [NotifyPropertyChangedFor(nameof(IsWizardVisible))]
        [NotifyPropertyChangedFor(nameof(IsVeiculos))]
        [NotifyPropertyChangedFor(nameof(IsCondutores))]
        [NotifyPropertyChangedFor(nameof(IsHistorico))]
        [NotifyPropertyChangedFor(nameof(IsConfiguracoes))]
        private Page _paginaAtual = Page.Dashboard;

        public bool IsDashboard => PaginaAtual == Page.Dashboard;
        public bool IsWizardVisible => PaginaAtual == Page.Emissao;
        public bool IsVeiculos => PaginaAtual == Page.Veiculos;
        public bool IsCondutores => PaginaAtual == Page.Condutores;
        public bool IsHistorico => PaginaAtual == Page.Historico;
        public bool IsConfiguracoes => PaginaAtual == Page.Configuracoes;

        // Listas
        [ObservableProperty] private ObservableCollection<Veiculo> _veiculos = new();
        [ObservableProperty] private ObservableCollection<Condutor> _condutores = new();

        public DashboardViewModel(IAppDbContext dbContext, CurrentTenantService tenantService)
        {
            _dbContext = dbContext;
            _tenantService = tenantService;

            _ = CarregarDadosIniciais();
        }

        private async Task CarregarDadosIniciais()
        {
            // Carrega os dados EXCLUSIVOS da empresa selecionada!
            var veiculosDb = await _dbContext.Veiculos.ToListAsync();
            var condutoresDb = await _dbContext.Condutores.ToListAsync();

            Veiculos = new ObservableCollection<Veiculo>(veiculosDb);
            Condutores = new ObservableCollection<Condutor>(condutoresDb);
        }

        // --- Comandos de Navegação do Seu Layout ---
        [RelayCommand] private void AbrirDashboard() => PaginaAtual = Page.Dashboard;
        [RelayCommand] private void IniciarEmissao() => PaginaAtual = Page.Emissao;
        [RelayCommand] private void AbrirVeiculos() => PaginaAtual = Page.Veiculos;
        [RelayCommand] private void AbrirCondutores() => PaginaAtual = Page.Condutores;
        [RelayCommand] private void AbrirHistorico() => PaginaAtual = Page.Historico;
        [RelayCommand] private void AbrirConfiguracoes() => PaginaAtual = Page.Configuracoes;

        [RelayCommand]
        private void VoltarAoSeletor()
        {
            _tenantService.ClearTenant();
            var mainVm = App.Services!.GetRequiredService<MainViewModel>();
            mainVm.NavegarParaSeletor();
        }
    }
}