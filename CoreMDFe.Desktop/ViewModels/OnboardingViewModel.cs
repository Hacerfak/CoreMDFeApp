using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Onboarding;
using MediatR;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class OnboardingViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        // Controle de Passos (Heurística 8: Design Minimalista - Mostra só o necessário por vez)
        [ObservableProperty] private int _passoAtual = 1;
        public bool IsPasso1 => PassoAtual == 1;
        public bool IsPasso2 => PassoAtual == 2;

        // Feedback Visual (Heurística 1: Visibilidade do Status do Sistema)
        [ObservableProperty] private bool _isCarregando;
        [ObservableProperty] private string _mensagemCarregando = "Aguarde...";
        [ObservableProperty] private string _mensagemErro = string.Empty;

        // Passo 1: Dados do Certificado
        [ObservableProperty] private string _caminhoCertificado = string.Empty;
        [ObservableProperty] private string _senhaCertificado = string.Empty;
        [ObservableProperty] private string _ufSelecionada = "SP";

        // Passo 2: Dados da Empresa (Preenchidos pela SEFAZ - Heurística 6: Reconhecimento em vez de recordação)
        [ObservableProperty] private string _cnpj = string.Empty;
        [ObservableProperty] private string _razaoSocial = string.Empty;
        [ObservableProperty] private string _nomeFantasia = string.Empty;
        [ObservableProperty] private string _inscricaoEstadual = string.Empty;
        [ObservableProperty] private string _logradouro = string.Empty;
        [ObservableProperty] private string _numero = string.Empty;
        [ObservableProperty] private string _bairro = string.Empty;
        [ObservableProperty] private string _cep = string.Empty;
        [ObservableProperty] private string _municipio = string.Empty;
        [ObservableProperty] private long _ibge = 0;

        // Dados que o usuário precisa preencher manualmente
        [ObservableProperty] private string _rntrc = string.Empty;
        [ObservableProperty] private string _telefone = string.Empty;
        [ObservableProperty] private string _email = string.Empty;

        public string[] ListaUFs { get; } = { "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO" };

        public OnboardingViewModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [RelayCommand]
        private async Task ProcurarCertificado()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Selecione o Certificado Digital A1",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("Certificado PFX") { Patterns = new[] { "*.pfx", "*.p12" } } }
                });

                if (files.Count > 0)
                {
                    CaminhoCertificado = files[0].Path.LocalPath;
                    MensagemErro = string.Empty;
                }
            }
        }

        [RelayCommand]
        private async Task ConsultarSefaz()
        {
            // Heurística 5: Prevenção de Erros
            if (string.IsNullOrWhiteSpace(CaminhoCertificado) || string.IsNullOrWhiteSpace(SenhaCertificado))
            {
                MensagemErro = "Por favor, selecione o certificado e informe a senha antes de prosseguir.";
                return;
            }

            IsCarregando = true;
            MensagemCarregando = "Conectando à SEFAZ para buscar os dados...";
            MensagemErro = string.Empty;

            var command = new ConsultarDadosSefazCommand(CaminhoCertificado, SenhaCertificado, UfSelecionada);
            var result = await _mediator.Send(command);

            if (result.Sucesso && result.DadosEmpresa != null)
            {
                // Preenche os dados recebidos da Sefaz
                Cnpj = result.DadosEmpresa.Cnpj;
                RazaoSocial = result.DadosEmpresa.RazaoSocial;
                NomeFantasia = result.DadosEmpresa.RazaoSocial; // Padrão
                InscricaoEstadual = result.DadosEmpresa.Ie;
                Logradouro = result.DadosEmpresa.Logradouro;
                Numero = result.DadosEmpresa.Numero;
                Bairro = result.DadosEmpresa.Bairro;
                Cep = result.DadosEmpresa.Cep;
                Municipio = result.DadosEmpresa.Municipio;
                Ibge = result.DadosEmpresa.Ibge;

                // Avança para o Passo 2
                PassoAtual = 2;
                OnPropertyChanged(nameof(IsPasso1));
                OnPropertyChanged(nameof(IsPasso2));
            }
            else
            {
                // Heurística 9: Ajudar os usuários a reconhecer, diagnosticar e recuperar-se de erros
                MensagemErro = result.Mensagem;
            }

            IsCarregando = false;
        }

        [RelayCommand]
        private void VoltarPasso1()
        {
            // Heurística 3: Controle e Liberdade do Usuário
            PassoAtual = 1;
            OnPropertyChanged(nameof(IsPasso1));
            OnPropertyChanged(nameof(IsPasso2));
        }

        [RelayCommand]
        private async Task FinalizarCadastro()
        {
            if (string.IsNullOrWhiteSpace(Cnpj) || string.IsNullOrWhiteSpace(RazaoSocial))
            {
                MensagemErro = "Os dados obrigatórios da empresa não estão preenchidos.";
                return;
            }

            IsCarregando = true;
            MensagemCarregando = "Criando ambiente isolado para a empresa...";
            MensagemErro = string.Empty;

            var command = new ProvisionarNovaEmpresaCommand(
                Cnpj, RazaoSocial, NomeFantasia, InscricaoEstadual, Rntrc, Email, Telefone,
                Logradouro, Numero, Bairro, Cep, Municipio, Ibge, UfSelecionada,
                CaminhoCertificado, SenhaCertificado
            );

            var result = await _mediator.Send(command);

            IsCarregando = false;

            if (result.Sucesso)
            {
                var mainViewModel = App.Services!.GetRequiredService<MainViewModel>();
                mainViewModel.NavegarParaDashboard(result.CaminhoBanco);
            }
            else
            {
                MensagemErro = result.Mensagem;
            }
        }
    }
}