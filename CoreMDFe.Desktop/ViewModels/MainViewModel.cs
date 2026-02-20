using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        [ObservableProperty]
        private string _tituloAbaAtual = "Início";

        [ObservableProperty]
        private bool _isMenuOpen = true;

        public MainViewModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [RelayCommand]
        private void AbrirEmissao() => TituloAbaAtual = "Emitir MDF-e";

        [RelayCommand]
        private void AbrirConfiguracoes() => TituloAbaAtual = "Configurações do Sistema";

        [RelayCommand]
        private void ConsultarSefaz() => TituloAbaAtual = "Consultas Sefaz";

        [RelayCommand]
        private void AlternarMenu() => IsMenuOpen = !IsMenuOpen;
    }
}