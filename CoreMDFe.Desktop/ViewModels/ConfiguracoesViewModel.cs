using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Configuracoes;
using CoreMDFe.Application.Features.Onboarding;
using CoreMDFe.Core.Interfaces;
using System.Linq;
using CoreMDFe.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.IO;
using Avalonia.Media.Imaging;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class ConfiguracoesViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly IAppDbContext _dbContext;

        // --- DADOS DA EMPRESA E ENDEREÇO ---
        [ObservableProperty] private string _cnpj = string.Empty;
        [ObservableProperty] private string _nome = string.Empty;
        [ObservableProperty] private string _fantasia = string.Empty;
        [ObservableProperty] private string _ie = string.Empty;
        [ObservableProperty] private string _rntrc = string.Empty;

        [ObservableProperty] private string _logradouro = string.Empty;
        [ObservableProperty] private string _numeroEndereco = string.Empty;
        [ObservableProperty] private string _complemento = string.Empty;
        [ObservableProperty] private string _bairro = string.Empty;
        [ObservableProperty] private string _nomeMunicipio = string.Empty;
        [ObservableProperty] private long _codigoIbgeMunicipio;
        [ObservableProperty] private string _cep = string.Empty;
        [ObservableProperty] private string _telefone = string.Empty;
        [ObservableProperty] private string _email = string.Empty;
        [ObservableProperty] private Bitmap? _logoPreview;

        // --- CERTIFICADO E AMBIENTE ---
        [ObservableProperty] private string _caminhoCertificado = string.Empty;
        [ObservableProperty] private string _senhaCertificado = string.Empty;
        [ObservableProperty] private bool _manterCertificadoCache;
        [ObservableProperty] private string _ufEmitente = "SP";

        public ObservableCollection<string> ListaAmbientes { get; } = new() { "1 - Produção", "2 - Homologação" };
        [ObservableProperty] private int _ambienteSelecionadoIndex = 1;

        // --- ARQUIVOS E PASTAS ---
        [ObservableProperty] private bool _isSalvarXml = true;
        [ObservableProperty] private string _diretorioSalvarXml = string.Empty;
        [ObservableProperty] private string _diretorioSalvarPdf = string.Empty;

        // --- DADOS FISCAIS ---
        [ObservableProperty] private long _ultimaNumeracao = 0;
        [ObservableProperty] private int _serie = 1;

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

        [ObservableProperty] private string _mensagemSistema = string.Empty;

        // --- NOVOS CAMPOS WIZARD RÁPIDO ---
        [ObservableProperty] private ObservableCollection<Veiculo> _veiculosDisponiveis = new();
        [ObservableProperty] private ObservableCollection<Condutor> _condutoresDisponiveis = new();

        [ObservableProperty] private Veiculo? _veiculoPadraoSelecionado;
        [ObservableProperty] private Condutor? _condutorPadraoSelecionado;

        [ObservableProperty] private string _produtoTipoCargaPadrao = string.Empty;
        [ObservableProperty] private string _produtoNomePadrao = string.Empty;
        [ObservableProperty] private string _produtoEANPadrao = string.Empty;
        [ObservableProperty] private string _produtoNCMPadrao = string.Empty;

        [ObservableProperty] private int _seguroResponsavelPadraoIndex = 0; // 0=Emitente, 1=Contratante
        [ObservableProperty] private string _seguroCpfCnpjPadrao = string.Empty;
        [ObservableProperty] private string _seguroNomeSeguradoraPadrao = string.Empty;
        [ObservableProperty] private string _seguroCnpjSeguradoraPadrao = string.Empty;
        [ObservableProperty] private string _seguroApolicePadrao = string.Empty;

        [ObservableProperty] private string _pagamentoNomeContratantePadrao = string.Empty;
        [ObservableProperty] private string _pagamentoCpfCnpjContratantePadrao = string.Empty;
        [ObservableProperty] private int _pagamentoIndicadorPadraoIndex = 0; // 0=À Vista, 1=A Prazo
        [ObservableProperty] private string _pagamentoCnpjInstituicaoPadrao = string.Empty;

        [ObservableProperty] private string _infoFiscoPadrao = string.Empty;
        [ObservableProperty] private string _infoComplementarPadrao = string.Empty;

        public ConfiguracoesViewModel(IMediator mediator, IAppDbContext dbContext)
        {
            _mediator = mediator;
            _dbContext = dbContext;
            _ = CarregarDadosAtuais();
        }

        private void AtualizarPreviewLogo()
        {
            if (Logomarca == null || Logomarca.Length == 0)
            {
                LogoPreview = null;
                return;
            }

            try
            {
                using (var ms = new MemoryStream(Logomarca))
                {
                    LogoPreview = new Bitmap(ms);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gerar prévia da logo: {ex.Message}");
                LogoPreview = null;
            }
        }

        private async Task CarregarDadosAtuais()
        {
            var empresa = await _dbContext.Empresas.Include(e => e.Configuracao).FirstOrDefaultAsync();
            if (empresa != null)
            {
                // Dados da Empresa
                Cnpj = empresa.Cnpj ?? string.Empty;
                Nome = empresa.Nome ?? string.Empty;
                Fantasia = empresa.NomeFantasia ?? string.Empty;
                Ie = empresa.InscricaoEstadual ?? string.Empty;
                Rntrc = empresa.RNTRC ?? string.Empty;
                Logradouro = empresa.Logradouro ?? string.Empty;
                NumeroEndereco = empresa.Numero ?? string.Empty;
                Complemento = empresa.Complemento ?? string.Empty;
                Bairro = empresa.Bairro ?? string.Empty;
                NomeMunicipio = empresa.NomeMunicipio ?? string.Empty;
                CodigoIbgeMunicipio = empresa.CodigoIbgeMunicipio;
                Cep = empresa.Cep ?? string.Empty;
                Telefone = empresa.Telefone ?? string.Empty;
                Email = empresa.Email ?? string.Empty;

                // Carrega listas para ComboBoxes
                var veiculos = await _dbContext.Veiculos.ToListAsync();
                VeiculosDisponiveis = new ObservableCollection<Veiculo>(veiculos);

                var condutores = await _dbContext.Condutores.ToListAsync();
                CondutoresDisponiveis = new ObservableCollection<Condutor>(condutores);

                if (empresa.Configuracao != null)
                {
                    CaminhoCertificado = empresa.Configuracao.CaminhoArquivoCertificado ?? string.Empty;
                    SenhaCertificado = empresa.Configuracao.SenhaCertificado ?? string.Empty;
                    ManterCertificadoCache = empresa.Configuracao.ManterCertificadoEmCache;
                    UfEmitente = empresa.Configuracao.UfEmitente ?? "SP";
                    AmbienteSelecionadoIndex = empresa.Configuracao.TipoAmbiente == 1 ? 0 : 1;
                    UltimaNumeracao = empresa.Configuracao.UltimaNumeracao;
                    Serie = empresa.Configuracao.Serie;

                    IsSalvarXml = empresa.Configuracao.IsSalvarXml;
                    DiretorioSalvarXml = empresa.Configuracao.DiretorioSalvarXml ?? string.Empty;
                    DiretorioSalvarPdf = empresa.Configuracao.DiretorioSalvarPdf ?? string.Empty;

                    GerarQrCode = empresa.Configuracao.GerarQrCode;
                    ModalidadePadrao = Math.Max(0, empresa.Configuracao.ModalidadePadrao - 1);
                    TipoEmissaoPadrao = Math.Max(0, empresa.Configuracao.TipoEmissaoPadrao - 1);
                    TipoEmitentePadrao = Math.Max(0, empresa.Configuracao.TipoEmitentePadrao - 1);
                    TipoTransportadorPadrao = empresa.Configuracao.TipoTransportadorPadrao;

                    Logomarca = empresa.Configuracao.Logomarca;
                    AtualizarPreviewLogo();
                    if (Logomarca != null && Logomarca.Length > 0)
                        TextoLogomarca = "Logo atual carregada com sucesso!";

                    if (empresa.Configuracao.VeiculoPadraoId.HasValue)
                        VeiculoPadraoSelecionado = veiculos.FirstOrDefault(v => v.Id == empresa.Configuracao.VeiculoPadraoId.Value);

                    if (empresa.Configuracao.CondutorPadraoId.HasValue)
                        CondutorPadraoSelecionado = condutores.FirstOrDefault(c => c.Id == empresa.Configuracao.CondutorPadraoId.Value);

                    ProdutoTipoCargaPadrao = empresa.Configuracao.ProdutoTipoCargaPadrao ?? string.Empty;
                    ProdutoNomePadrao = empresa.Configuracao.ProdutoNomePadrao ?? string.Empty;
                    ProdutoEANPadrao = empresa.Configuracao.ProdutoEANPadrao ?? string.Empty;
                    ProdutoNCMPadrao = empresa.Configuracao.ProdutoNCMPadrao ?? string.Empty;

                    SeguroResponsavelPadraoIndex = empresa.Configuracao.SeguroResponsavelPadrao == 2 ? 1 : 0;
                    SeguroCpfCnpjPadrao = empresa.Configuracao.SeguroCpfCnpjPadrao ?? string.Empty;
                    SeguroNomeSeguradoraPadrao = empresa.Configuracao.SeguroNomeSeguradoraPadrao ?? string.Empty;
                    SeguroCnpjSeguradoraPadrao = empresa.Configuracao.SeguroCnpjSeguradoraPadrao ?? string.Empty;
                    SeguroApolicePadrao = empresa.Configuracao.SeguroApolicePadrao ?? string.Empty;

                    PagamentoNomeContratantePadrao = empresa.Configuracao.PagamentoNomeContratantePadrao ?? string.Empty;
                    PagamentoCpfCnpjContratantePadrao = empresa.Configuracao.PagamentoCpfCnpjContratantePadrao ?? string.Empty;
                    PagamentoIndicadorPadraoIndex = empresa.Configuracao.PagamentoIndicadorPadrao;
                    PagamentoCnpjInstituicaoPadrao = empresa.Configuracao.PagamentoCnpjInstituicaoPadrao ?? string.Empty;

                    InfoFiscoPadrao = empresa.Configuracao.InfoFiscoPadrao ?? string.Empty;
                    InfoComplementarPadrao = empresa.Configuracao.InfoComplementarPadrao ?? string.Empty;
                }
            }
        }

        [RelayCommand]
        private async Task ConsultarSefaz()
        {
            if (string.IsNullOrWhiteSpace(Cnpj) || string.IsNullOrWhiteSpace(UfEmitente))
            {
                MensagemSistema = "⚠️ Preencha a UF do Emitente antes de consultar a SEFAZ.";
                return;
            }

            MensagemSistema = "Consultando SEFAZ, aguarde...";
            try
            {
                // Reutilizamos a query que criamos na fase de Onboarding!
                var query = new ConsultarDadosSefazCommand(CaminhoCertificado, SenhaCertificado, UfEmitente);
                var result = await _mediator.Send(query);

                if (result.Sucesso && result.DadosEmpresa != null)
                {
                    Nome = result.DadosEmpresa.RazaoSocial ?? string.Empty;
                    Fantasia = Nome;
                    Ie = result.DadosEmpresa.Ie ?? string.Empty;
                    Cep = result.DadosEmpresa.Cep ?? string.Empty;
                    Logradouro = result.DadosEmpresa.Logradouro ?? string.Empty;
                    NumeroEndereco = result.DadosEmpresa.Numero ?? string.Empty;
                    Complemento = string.Empty;
                    Bairro = result.DadosEmpresa.Bairro ?? string.Empty;
                    NomeMunicipio = result.DadosEmpresa.Municipio ?? string.Empty;
                    CodigoIbgeMunicipio = result.DadosEmpresa.Ibge;

                    MensagemSistema = "✅ Dados da SEFAZ importados com sucesso!";
                }
                else if (result.Sucesso)
                {
                    MensagemSistema = "❌ Resultado da consulta não continha dados da empresa.";
                }
                else
                {
                    MensagemSistema = $"❌ Erro na consulta SEFAZ: {result.Mensagem}";
                }
            }
            catch (Exception ex)
            {
                MensagemSistema = $"❌ Falha: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ProcurarDiretorioXml() => DiretorioSalvarXml = await AbrirFolderPicker("Pasta para XMLs") ?? DiretorioSalvarXml;

        [RelayCommand]
        private async Task ProcurarDiretorioPdf() => DiretorioSalvarPdf = await AbrirFolderPicker("Pasta para PDFs") ?? DiretorioSalvarPdf;

        private async Task<string?> AbrirFolderPicker(string titulo)
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var folders = await desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = titulo, AllowMultiple = false });
                return folders.Count > 0 ? folders[0].Path.LocalPath : null;
            }
            return null;
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
                        AtualizarPreviewLogo();
                        TextoLogomarca = $"Logo selecionada: {files[0].Name}";
                    }
                    catch (Exception ex) { MensagemSistema = $"Erro ao ler imagem: {ex.Message}"; }
                }
            }
        }

        [RelayCommand]
        public async Task SalvarConfiguracoes()
        {
            MensagemSistema = "Salvando configurações...";

            int tipoAmbienteSefaz = AmbienteSelecionadoIndex == 0 ? 1 : 2;

            var command = new SalvarConfiguracaoCommand(
                Cnpj, Nome, Fantasia, Ie, Rntrc,
                Logradouro, NumeroEndereco, Complemento, Bairro, NomeMunicipio, CodigoIbgeMunicipio, Cep, Telefone, Email,
                CaminhoCertificado, SenhaCertificado, ManterCertificadoCache,
                tipoAmbienteSefaz, UfEmitente, UltimaNumeracao, Serie, 5000,
                RespTecCnpj, RespTecNome, RespTecTelefone, RespTecEmail,
                GerarQrCode, ModalidadePadrao + 1, TipoEmissaoPadrao + 1, TipoEmitentePadrao + 1, TipoTransportadorPadrao,
                Logomarca, IsSalvarXml, DiretorioSalvarXml, DiretorioSalvarPdf, VeiculoPadraoSelecionado?.Id, CondutorPadraoSelecionado?.Id,
                ProdutoTipoCargaPadrao, ProdutoNomePadrao, ProdutoEANPadrao, ProdutoNCMPadrao,
                SeguroResponsavelPadraoIndex == 1 ? 2 : 1, SeguroCpfCnpjPadrao, SeguroNomeSeguradoraPadrao, SeguroCnpjSeguradoraPadrao, SeguroApolicePadrao,
                PagamentoNomeContratantePadrao, PagamentoCpfCnpjContratantePadrao, PagamentoIndicadorPadraoIndex, PagamentoCnpjInstituicaoPadrao,
                InfoFiscoPadrao, InfoComplementarPadrao
            );

            var sucesso = await _mediator.Send(command);
            MensagemSistema = sucesso ? "✅ Configurações atualizadas com sucesso!" : "❌ Erro ao salvar configurações.";
        }
    }
}