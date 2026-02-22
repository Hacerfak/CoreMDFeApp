using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using CoreMDFe.Application.Services;
using CoreMDFe.Application.Features.Cadastros;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia;
using MediatR;
using System;

namespace CoreMDFe.Desktop.ViewModels
{
    public enum Page
    {
        Dashboard,
        Emissao,
        Veiculos,
        Condutores,
        Historico,
        Configuracoes
    }

    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IAppDbContext _dbContext;
        private readonly CurrentTenantService _tenantService;
        private readonly IMediator _mediator;

        // --- DADOS DA EMPRESA ATUAL (Heurística 1) ---
        [ObservableProperty] private string _empresaAtualNome = "Carregando...";
        [ObservableProperty] private string _empresaAtualCnpj = "";
        [ObservableProperty] private string _empresaAtualUf = "";

        // --- CONTROLE DE ABAS ---
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

        // --- LISTAS ---
        [ObservableProperty] private ObservableCollection<Veiculo> _veiculos = new();
        [ObservableProperty] private ObservableCollection<Condutor> _condutores = new();

        // --- MODAIS (Cadastros) ---
        [ObservableProperty] private bool _isModalVeiculoAberto;
        [ObservableProperty] private Veiculo _novoVeiculo = new();
        [ObservableProperty] private bool _isVeiculoAvancadoAberto;
        [ObservableProperty] private int _rodadoSelecionadoIndex = 2; // Padrão: Cavalo Mecânico
        [ObservableProperty] private int _carroceriaSelecionadaIndex = 2; // Padrão: Fechada

        [ObservableProperty] private bool _isModalCondutorAberto;
        [ObservableProperty] private Condutor _novoCondutor = new();

        public ObservableCollection<string> ListaTiposRodado { get; } = new() { "01 - Truck", "02 - Toco", "03 - Cavalo Mecânico", "04 - VAN", "05 - Utilitário", "06 - Outros" };
        public ObservableCollection<string> ListaTiposCarroceria { get; } = new() { "00 - Não aplicável", "01 - Aberta", "02 - Fechada/Baú", "03 - Granelera", "04 - Porta Container", "05 - Sider" };



        public DashboardViewModel(IAppDbContext dbContext, CurrentTenantService tenantService, IMediator mediator)
        {
            _dbContext = dbContext;
            _tenantService = tenantService;
            _mediator = mediator;

            _ = CarregarDadosIniciais();
        }

        private async Task CarregarDadosIniciais()
        {
            // Busca a empresa logada
            var empresa = await _dbContext.Empresas.FirstOrDefaultAsync();
            if (empresa != null)
            {
                EmpresaAtualNome = !string.IsNullOrWhiteSpace(empresa.NomeFantasia) ? empresa.NomeFantasia : empresa.Nome;
                EmpresaAtualCnpj = empresa.Cnpj;
                EmpresaAtualUf = empresa.SiglaUf;
            }

            await AtualizarListas();
        }

        private async Task AtualizarListas()
        {
            var veiculosDb = await _mediator.Send(new ListarVeiculosQuery());
            var condutoresDb = await _mediator.Send(new ListarCondutoresQuery());

            Veiculos = new ObservableCollection<Veiculo>(veiculosDb);
            Condutores = new ObservableCollection<Condutor>(condutoresDb);
        }

        // --- COMANDOS VEÍCULOS ---
        [RelayCommand]
        private void AbrirModalVeiculo()
        {
            NovoVeiculo = new Veiculo();
            IsVeiculoAvancadoAberto = false;
            RodadoSelecionadoIndex = 2;
            CarroceriaSelecionadaIndex = 2;
            IsModalVeiculoAberto = true;
        }

        [RelayCommand]
        private async Task SalvarVeiculo()
        {
            if (string.IsNullOrWhiteSpace(NovoVeiculo.Placa)) return;

            NovoVeiculo.TipoRodado = (RodadoSelecionadoIndex + 1).ToString("D2"); // Ex: 01, 02
            NovoVeiculo.TipoCarroceria = CarroceriaSelecionadaIndex.ToString("D2"); // Ex: 00, 01

            if (string.IsNullOrEmpty(NovoVeiculo.UfLicenciamento)) NovoVeiculo.UfLicenciamento = EmpresaAtualUf;

            await _mediator.Send(new SalvarVeiculoCommand(NovoVeiculo));
            await AtualizarListas();
            IsModalVeiculoAberto = false;
        }

        [RelayCommand]
        private async Task ExcluirVeiculo(Guid id)
        {
            await _mediator.Send(new ExcluirVeiculoCommand(id));
            await AtualizarListas();
        }

        // --- COMANDOS CONDUTORES ---
        [RelayCommand]
        private void AbrirModalCondutor()
        {
            NovoCondutor = new Condutor();
            IsModalCondutorAberto = true;
        }

        [RelayCommand]
        private async Task SalvarCondutor()
        {
            if (string.IsNullOrWhiteSpace(NovoCondutor.Nome) || string.IsNullOrWhiteSpace(NovoCondutor.Cpf)) return;

            await _mediator.Send(new SalvarCondutorCommand(NovoCondutor));
            await AtualizarListas();
            IsModalCondutorAberto = false;
        }

        [RelayCommand]
        private async Task ExcluirCondutor(Guid id)
        {
            await _mediator.Send(new ExcluirCondutorCommand(id));
            await AtualizarListas();
        }

        [RelayCommand]
        private void FecharModais()
        {
            IsModalVeiculoAberto = false;
            IsModalCondutorAberto = false;
        }

        // --- COMANDOS DE NAVEGAÇÃO ---
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