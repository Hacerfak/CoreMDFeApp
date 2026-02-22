using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Configuracoes;
using CoreMDFe.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class ConfiguracoesViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly IAppDbContext _dbContext;

        [ObservableProperty] private string _cnpj = string.Empty;
        [ObservableProperty] private string _nome = string.Empty;
        [ObservableProperty] private string _fantasia = string.Empty;
        [ObservableProperty] private string _ie = string.Empty;
        [ObservableProperty] private string _rntrc = string.Empty;

        [ObservableProperty] private string _caminhoCertificado = string.Empty;
        [ObservableProperty] private string _senhaCertificado = string.Empty;
        [ObservableProperty] private bool _manterCertificadoCache;

        [ObservableProperty] private string _ufEmitente = "SP";

        public ObservableCollection<string> ListaAmbientes { get; } = new() { "1 - Produção", "2 - Homologação" };
        [ObservableProperty] private int _ambienteSelecionadoIndex = 1; // Padrão 1 = Homologação (que é o 2 na Sefaz)

        [ObservableProperty] private string _mensagemSistema = string.Empty;

        public ConfiguracoesViewModel(IMediator mediator, IAppDbContext dbContext)
        {
            _mediator = mediator;
            _dbContext = dbContext;
            _ = CarregarDadosAtuais();
        }

        private async Task CarregarDadosAtuais()
        {
            var empresa = await _dbContext.Empresas.Include(e => e.Configuracao).FirstOrDefaultAsync();
            if (empresa != null)
            {
                Cnpj = empresa.Cnpj;
                Nome = empresa.Nome;
                Fantasia = empresa.NomeFantasia;
                Ie = empresa.InscricaoEstadual;
                Rntrc = empresa.RNTRC;

                if (empresa.Configuracao != null)
                {
                    CaminhoCertificado = empresa.Configuracao.CaminhoArquivoCertificado;
                    SenhaCertificado = empresa.Configuracao.SenhaCertificado;
                    ManterCertificadoCache = empresa.Configuracao.ManterCertificadoEmCache;
                    UfEmitente = empresa.Configuracao.UfEmitente;
                    AmbienteSelecionadoIndex = empresa.Configuracao.TipoAmbiente == 1 ? 0 : 1;
                }
            }
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

                if (files.Count > 0) CaminhoCertificado = files[0].Path.LocalPath;
            }
        }

        [RelayCommand]
        public async Task SalvarConfiguracoes()
        {
            MensagemSistema = "Salvando configurações...";

            int tipoAmbienteSefaz = AmbienteSelecionadoIndex == 0 ? 1 : 2; // 1=Prod, 2=Homol

            var command = new SalvarConfiguracaoCommand(
                Cnpj, Nome, Fantasia, Ie, Rntrc,
                CaminhoCertificado, SenhaCertificado, ManterCertificadoCache,
                tipoAmbienteSefaz, UfEmitente, 0, 5000
            );

            var sucesso = await _mediator.Send(command);

            MensagemSistema = sucesso ? "Configurações atualizadas com sucesso!" : "Erro ao salvar configurações.";
        }
    }
}