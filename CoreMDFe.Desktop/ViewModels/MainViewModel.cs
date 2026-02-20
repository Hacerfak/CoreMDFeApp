using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        [ObservableProperty]
        private string _tituloAbaAtual = "Início";

        [ObservableProperty]
        private bool _isMenuOpen = true;

        // Propriedade que guarda a ViewModel da tela ativa
        [ObservableProperty]
        private ObservableObject _conteudoAtual = null!;

        public MainViewModel(IMediator mediator)
        {
            _mediator = mediator;
            // Define a tela inicial
            ConteudoAtual = new HomeViewModel();
        }

        [RelayCommand]
        private void AbrirEmissao()
        {
            TituloAbaAtual = "Emitir MDF-e";
            // Quando tivermos a tela de emissão, faremos:
            // ConteudoAtual = App.Services!.GetRequiredService<EmissaoViewModel>();
        }

        [RelayCommand]
        private void AbrirConfiguracoes()
        {
            TituloAbaAtual = "Configurações do Sistema";
            // Injeta a ViewModel de configurações na área central
            ConteudoAtual = App.Services!.GetRequiredService<ConfiguracoesViewModel>();
        }

        [RelayCommand]
        private void ConsultarSefaz()
        {
            TituloAbaAtual = "Consultas Sefaz";
        }

        [RelayCommand]
        private void AlternarMenu() => IsMenuOpen = !IsMenuOpen;
    }

    // ViewModel provisória apenas para desenhar a tela inicial de Boas-vindas
    public class HomeViewModel : ObservableObject { }
}