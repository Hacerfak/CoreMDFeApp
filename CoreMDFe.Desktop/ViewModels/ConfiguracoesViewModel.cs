using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Configuracoes;
using CoreMDFe.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.IO;

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
        [ObservableProperty] private int _ambienteSelecionadoIndex = 1;

        [ObservableProperty] private string _mensagemSistema = string.Empty;

        // --- RESPONSÁVEL TÉCNICO ---
        [ObservableProperty] private string _respTecCnpj = string.Empty;
        [ObservableProperty] private string _respTecNome = string.Empty;
        [ObservableProperty] private string _respTecTelefone = string.Empty;
        [ObservableProperty] private string _respTecEmail = string.Empty;

        // --- PADRÕES DE EMISSÃO ---
        [ObservableProperty] private bool _gerarQrCode = true;
        [ObservableProperty] private int _modalidadePadrao = 1;
        [ObservableProperty] private int _tipoEmissaoPadrao = 1;
        [ObservableProperty] private int _tipoEmitentePadrao = 1;
        [ObservableProperty] private int _tipoTransportadorPadrao = 1;

        // --- LOGOMARCA ---
        [ObservableProperty] private byte[]? _logomarca;
        [ObservableProperty] private string _textoLogomarca = "Nenhuma logo selecionada";

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
                Cnpj = empresa.Cnpj ?? string.Empty;
                Nome = empresa.Nome ?? string.Empty;
                Fantasia = empresa.NomeFantasia ?? string.Empty;
                Ie = empresa.InscricaoEstadual ?? string.Empty;
                Rntrc = empresa.RNTRC ?? string.Empty;

                if (empresa.Configuracao != null)
                {
                    CaminhoCertificado = empresa.Configuracao.CaminhoArquivoCertificado ?? string.Empty;
                    SenhaCertificado = empresa.Configuracao.SenhaCertificado ?? string.Empty;
                    ManterCertificadoCache = empresa.Configuracao.ManterCertificadoEmCache;
                    UfEmitente = empresa.Configuracao.UfEmitente ?? "SP";
                    AmbienteSelecionadoIndex = empresa.Configuracao.TipoAmbiente == 1 ? 0 : 1;

                    RespTecCnpj = empresa.Configuracao.RespTecCnpj ?? string.Empty;
                    RespTecNome = empresa.Configuracao.RespTecNome ?? string.Empty;
                    RespTecTelefone = empresa.Configuracao.RespTecTelefone ?? string.Empty;
                    RespTecEmail = empresa.Configuracao.RespTecEmail ?? string.Empty;

                    GerarQrCode = empresa.Configuracao.GerarQrCode;
                    // Conversão de volta para o ComboBox (Base 0)
                    ModalidadePadrao = Math.Max(0, empresa.Configuracao.ModalidadePadrao - 1);
                    TipoEmissaoPadrao = Math.Max(0, empresa.Configuracao.TipoEmissaoPadrao - 1);
                    TipoEmitentePadrao = Math.Max(0, empresa.Configuracao.TipoEmitentePadrao - 1);
                    TipoTransportadorPadrao = empresa.Configuracao.TipoTransportadorPadrao;

                    // Carrega a Logo do Banco
                    Logomarca = empresa.Configuracao.Logomarca;
                    if (Logomarca != null && Logomarca.Length > 0)
                        TextoLogomarca = "Logo atual carregada com sucesso!";
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
        private async Task ProcurarLogo()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Selecione a Logo da Empresa",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("Imagens") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg" } } }
                });

                if (files.Count > 0)
                {
                    try
                    {
                        Logomarca = await File.ReadAllBytesAsync(files[0].Path.LocalPath);
                        TextoLogomarca = $"Logo selecionada: {files[0].Name}";
                    }
                    catch (Exception ex)
                    {
                        MensagemSistema = $"Erro ao ler imagem: {ex.Message}";
                    }
                }
            }
        }

        [RelayCommand]
        public async Task SalvarConfiguracoes()
        {
            MensagemSistema = "Salvando configurações...";

            int tipoAmbienteSefaz = AmbienteSelecionadoIndex == 0 ? 1 : 2;

            // Passamos todos os parâmetros, incluindo a Logomarca!
            var command = new SalvarConfiguracaoCommand(
                Cnpj, Nome, Fantasia, Ie, Rntrc,
                CaminhoCertificado, SenhaCertificado, ManterCertificadoCache,
                tipoAmbienteSefaz, UfEmitente, 0, 5000,
                RespTecCnpj, RespTecNome, RespTecTelefone, RespTecEmail,
                GerarQrCode,
                ModalidadePadrao + 1, // UI(0) -> SEFAZ(1)
                TipoEmissaoPadrao + 1,
                TipoEmitentePadrao + 1,
                TipoTransportadorPadrao,
                Logomarca // <-- AQUI ENVIAMOS PARA O COMMAND
            );

            var sucesso = await _mediator.Send(command);

            MensagemSistema = sucesso ? "Configurações atualizadas com sucesso!" : "Erro ao salvar configurações.";
        }
    }
}