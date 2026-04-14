using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Cadastros;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using CoreMDFe.Core.Validations;
using CoreMDFe.Application.Mediator;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations; // Adicionado para validações
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    // 1. Mudamos a herança para ObservableValidator
    public partial class VeiculosViewModel : ObservableValidator
    {
        private readonly IMediator _mediator;
        private readonly IAppDbContext _dbContext;

        [ObservableProperty] private ObservableCollection<Veiculo> _veiculos = new();
        [ObservableProperty] private bool _isModalAberto;
        [ObservableProperty] private bool _isVeiculoAvancadoAberto;

        private Guid _veiculoEmEdicaoId = Guid.Empty;
        private string _ufPadraoEmpresa = "SP";

        // ====================================================================
        // PROPRIEDADES COM VALIDAÇÃO (Substituem o antigo NovoVeiculo)
        // ====================================================================
        [ObservableProperty]
        [Required(ErrorMessage = "A Placa é obrigatória.")]
        [Placa(ErrorMessage = "A Placa informada é inválida (Use padrão Antigo ou Mercosul).")]
        private string _placa = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "O Renavam é obrigatório.")]
        [Renavam(ErrorMessage = "O Renavam informado não é válido. Verifique os números.")]
        private string _renavam = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Obrigatório.")]
        [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "Apenas números (maior que 0).")]
        private string _taraKg = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Obrigatório.")]
        [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "Apenas números (maior que 0).")]
        private string _capacidadeKg = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Obrigatório.")]
        [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "Apenas números (maior que 0).")]
        private string _capacidadeM3 = string.Empty;

        // Controle dos ComboBoxes
        public ObservableCollection<string> ListaTiposVeiculo { get; } = new() { "0 - Tração", "1 - Reboque" };
        [ObservableProperty] private int _tipoVeiculoSelecionadoIndex = 0;

        [ObservableProperty] private int _rodadoSelecionadoIndex = 2;
        [ObservableProperty] private int _carroceriaSelecionadaIndex = 2;

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
            _veiculoEmEdicaoId = Guid.Empty;
            Placa = string.Empty;
            Renavam = string.Empty;
            TaraKg = string.Empty;
            CapacidadeKg = string.Empty;
            CapacidadeM3 = string.Empty;

            TipoVeiculoSelecionadoIndex = 0;
            RodadoSelecionadoIndex = 2;
            CarroceriaSelecionadaIndex = 2;

            // Mágica: Pinta de vermelho os campos vazios ao abrir
            ValidateAllProperties();

            IsModalAberto = true;
        }

        [RelayCommand]
        private void FecharModal() => IsModalAberto = false;

        [RelayCommand]
        private async Task Salvar()
        {
            ValidateAllProperties();

            // Interrompe se houver alguma caixa de texto vermelha
            if (HasErrors) return;

            // Monta a entidade para salvar no banco
            var veiculo = new Veiculo
            {
                Id = _veiculoEmEdicaoId,
                Placa = Placa.ToUpper(), // Força maiúscula
                Renavam = Renavam,
                UfLicenciamento = _ufPadraoEmpresa,
                TaraKg = int.Parse(TaraKg),
                CapacidadeKg = int.Parse(CapacidadeKg),
                CapacidadeM3 = int.Parse(CapacidadeM3),
                TipoVeiculo = TipoVeiculoSelecionadoIndex,
                TipoRodado = (RodadoSelecionadoIndex + 1).ToString("D2"),
                TipoCarroceria = CarroceriaSelecionadaIndex.ToString("D2")
            };

            await _mediator.Send(new SalvarVeiculoCommand(veiculo));
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
            _veiculoEmEdicaoId = veiculo.Id;
            Placa = veiculo.Placa;
            Renavam = veiculo.Renavam;
            TaraKg = veiculo.TaraKg.ToString();
            CapacidadeKg = veiculo.CapacidadeKg.ToString();
            CapacidadeM3 = veiculo.CapacidadeM3.ToString();

            TipoVeiculoSelecionadoIndex = veiculo.TipoVeiculo;

            if (int.TryParse(veiculo.TipoRodado, out int rodado))
                RodadoSelecionadoIndex = Math.Max(0, rodado - 1);

            if (int.TryParse(veiculo.TipoCarroceria, out int carroceria))
                CarroceriaSelecionadaIndex = Math.Max(0, carroceria);

            ValidateAllProperties();

            IsModalAberto = true;
        }
    }
}