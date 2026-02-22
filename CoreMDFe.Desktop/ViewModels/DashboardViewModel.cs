using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Services;
using CoreMDFe.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IAppDbContext _dbContext;
        private readonly CurrentTenantService _tenantService;
        private readonly IServiceProvider _serviceProvider;

        // Dados do cabeçalho do Menu (Heurística 1)
        [ObservableProperty] private string _empresaAtualNome = "Carregando...";
        [ObservableProperty] private string _empresaAtualCnpj = "";
        [ObservableProperty] private string _empresaAtualUf = "";

        // O Coração da Navegação Interna: O conteúdo que vai aparecer na tela central
        [ObservableProperty]
        private ObservableObject _conteudoWorkspace = null!;

        public DashboardViewModel(IAppDbContext dbContext, CurrentTenantService tenantService, IServiceProvider serviceProvider)
        {
            _dbContext = dbContext;
            _tenantService = tenantService;
            _serviceProvider = serviceProvider;

            _ = CarregarDadosIniciais();

            // Inicia sempre na tela de Resumo
            AbrirResumo();
        }

        private async Task CarregarDadosIniciais()
        {
            var empresa = await _dbContext.Empresas.FirstOrDefaultAsync();
            if (empresa != null)
            {
                EmpresaAtualNome = !string.IsNullOrWhiteSpace(empresa.NomeFantasia) ? empresa.NomeFantasia : empresa.Nome;
                EmpresaAtualCnpj = empresa.Cnpj;
                EmpresaAtualUf = empresa.SiglaUf;
            }
        }

        // --- COMANDOS DO MENU LATERAL ---

        [RelayCommand]
        private void AbrirResumo() => ConteudoWorkspace = _serviceProvider.GetRequiredService<ResumoViewModel>();

        [RelayCommand]
        private void AbrirEmissao() => ConteudoWorkspace = _serviceProvider.GetRequiredService<EmissaoViewModel>();

        [RelayCommand]
        private void AbrirVeiculos() => ConteudoWorkspace = _serviceProvider.GetRequiredService<VeiculosViewModel>();

        [RelayCommand]
        private void AbrirCondutores() => ConteudoWorkspace = _serviceProvider.GetRequiredService<CondutoresViewModel>();

        [RelayCommand]
        private void VoltarAoSeletor()
        {
            _tenantService.ClearTenant();
            var mainVm = App.Services!.GetRequiredService<MainViewModel>();
            mainVm.NavegarParaSeletor();
        }

        // Futuros: AbrirEmissao(), AbrirHistorico()...
    }
}