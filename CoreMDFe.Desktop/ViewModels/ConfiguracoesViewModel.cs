using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Configuracoes;
using MediatR;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class ConfiguracoesViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        [ObservableProperty] private string _cnpj = string.Empty;
        [ObservableProperty] private string _nome = string.Empty;
        [ObservableProperty] private string _fantasia = string.Empty;
        [ObservableProperty] private string _ie = string.Empty;
        [ObservableProperty] private string _rntrc = string.Empty;

        [ObservableProperty] private string _caminhoCertificado = string.Empty;
        [ObservableProperty] private string _senhaCertificado = string.Empty;
        [ObservableProperty] private bool _manterCertificadoCache;

        [ObservableProperty] private int _tipoAmbiente = 2; // Padrão: 2 (Homologação)
        [ObservableProperty] private string _ufEmitente = "SP";

        [ObservableProperty] private string _mensagemSistema = string.Empty;

        public ConfiguracoesViewModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [RelayCommand]
        public async Task SalvarConfiguracoes()
        {
            MensagemSistema = "Salvando...";

            // Invoca o Handler que você já criou!
            var command = new SalvarConfiguracaoCommand(
                Cnpj, Nome, Fantasia, Ie, Rntrc,
                CaminhoCertificado, SenhaCertificado, ManterCertificadoCache,
                TipoAmbiente, UfEmitente, 0, 5000
            );

            var sucesso = await _mediator.Send(command);

            MensagemSistema = sucesso ? "Configurações salvas no SQLite com sucesso!" : "Erro ao salvar configurações.";
        }
    }
}