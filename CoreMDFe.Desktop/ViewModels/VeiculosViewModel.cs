using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Cadastros;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class VeiculosViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly IAppDbContext _dbContext;

        [ObservableProperty] private ObservableCollection<Veiculo> _veiculos = new();
        [ObservableProperty] private bool _isModalAberto;
        [ObservableProperty] private Veiculo _novoVeiculo = new();

        [ObservableProperty] private bool _isVeiculoAvancadoAberto;

        // NOVO: Controle do Tipo de Veículo
        public ObservableCollection<string> ListaTiposVeiculo { get; } = new() { "0 - Tração", "1 - Reboque" };
        [ObservableProperty] private int _tipoVeiculoSelecionadoIndex = 0; // Padrão: Tração

        [ObservableProperty] private int _rodadoSelecionadoIndex = 2; // Padrão: Cavalo Mecânico
        [ObservableProperty] private int _carroceriaSelecionadaIndex = 2; // Padrão: Fechada

        private string _ufPadraoEmpresa = "SP";

        public ObservableCollection<string> ListaTiposRodado { get; } = new() { "01 - Truck", "02 - Toco", "03 - Cavalo Mecânico", "04 - VAN", "05 - Utilitário", "06 - Outros" };
        public ObservableCollection<string> ListaTiposCarroceria { get; } = new() { "00 - Não aplicável", "01 - Aberta", "02 - Fechada/Baú", "03 - Granelera", "04 - Porta Container", "05 - Sider" };
        public string[] ListaUFs { get; } = { "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO" };

        public VeiculosViewModel(IMediator mediator, IAppDbContext dbContext)
        {
            _mediator = mediator;
            _dbContext = dbContext;
            _ = InicializarDadosAsync();
        }

        private async Task InicializarDadosAsync()
        {
            var empresa = await _dbContext.Empresas.FirstOrDefaultAsync();
            if (empresa != null) _ufPadraoEmpresa = empresa.SiglaUf;

            await CarregarListaAsync();
        }

        private async Task CarregarListaAsync()
        {
            var lista = await _mediator.Send(new ListarVeiculosQuery());
            Veiculos = new ObservableCollection<Veiculo>(lista);
        }

        [RelayCommand]
        private void AbrirModal()
        {
            NovoVeiculo = new Veiculo { UfLicenciamento = _ufPadraoEmpresa };
            IsVeiculoAvancadoAberto = false;
            TipoVeiculoSelecionadoIndex = 0; // Reseta para Tração
            RodadoSelecionadoIndex = 2;
            CarroceriaSelecionadaIndex = 2;
            IsModalAberto = true;
        }

        [RelayCommand]
        private void FecharModal() => IsModalAberto = false;

        [RelayCommand]
        private async Task Salvar()
        {
            if (string.IsNullOrWhiteSpace(NovoVeiculo.Placa)) return;

            // Salva o tipo do veículo (0 ou 1)
            NovoVeiculo.TipoVeiculo = TipoVeiculoSelecionadoIndex;

            NovoVeiculo.TipoRodado = (RodadoSelecionadoIndex + 1).ToString("D2");
            NovoVeiculo.TipoCarroceria = CarroceriaSelecionadaIndex.ToString("D2");

            await _mediator.Send(new SalvarVeiculoCommand(NovoVeiculo));
            await CarregarListaAsync();
            IsModalAberto = false;
        }

        [RelayCommand]
        private async Task Excluir(Guid id)
        {
            await _mediator.Send(new ExcluirVeiculoCommand(id));
            await CarregarListaAsync();
        }

        [RelayCommand]
        private void Editar(Veiculo veiculo)
        {
            NovoVeiculo = new Veiculo
            {
                Id = veiculo.Id,
                Placa = veiculo.Placa,
                Renavam = veiculo.Renavam,
                UfLicenciamento = veiculo.UfLicenciamento,
                TaraKg = veiculo.TaraKg,
                CapacidadeKg = veiculo.CapacidadeKg,
                CapacidadeM3 = veiculo.CapacidadeM3, // Carrega o M3
                TipoVeiculo = veiculo.TipoVeiculo,
                TipoRodado = veiculo.TipoRodado,
                TipoCarroceria = veiculo.TipoCarroceria,
                DataCriacao = veiculo.DataCriacao
            };

            // Sincroniza o ComboBox de Tipo de Veículo
            TipoVeiculoSelecionadoIndex = veiculo.TipoVeiculo;

            if (int.TryParse(veiculo.TipoRodado, out int rodado))
                RodadoSelecionadoIndex = Math.Max(0, rodado - 1);

            if (int.TryParse(veiculo.TipoCarroceria, out int carroceria))
                CarroceriaSelecionadaIndex = Math.Max(0, carroceria);

            IsModalAberto = true;
        }
    }
}