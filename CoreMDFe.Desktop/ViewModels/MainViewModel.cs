using CommunityToolkit.Mvvm.ComponentModel;
using CoreMDFe.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private ObservableObject _conteudoAtual = null!;

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            NavegarParaSeletor(); // Tela inicial é sempre o seletor!
        }

        public void NavegarParaSeletor()
        {
            ConteudoAtual = _serviceProvider.GetRequiredService<SeletorEmpresaViewModel>();
        }

        public void NavegarParaOnboarding()
        {
            ConteudoAtual = _serviceProvider.GetRequiredService<OnboardingViewModel>();
        }

        public void NavegarParaDashboard(string dbPath)
        {
            // Dizemos ao sistema inteiro: "A partir de agora, use ESTE banco de dados"
            var tenantService = _serviceProvider.GetRequiredService<CurrentTenantService>();
            tenantService.SetTenant(dbPath);

            // Carrega o layout lindo do Dashboard
            ConteudoAtual = _serviceProvider.GetRequiredService<DashboardViewModel>();
        }
    }
}