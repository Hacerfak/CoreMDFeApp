using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Cadastros;
using CoreMDFe.Core.Entities;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations; // Necessário para as tags [Required]
using CoreMDFe.Core.Validations;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    // 1. Mudamos para ObservableValidator
    public partial class CondutoresViewModel : ObservableValidator
    {
        private readonly IMediator _mediator;

        [ObservableProperty] private ObservableCollection<Condutor> _condutores = new();
        [ObservableProperty] private bool _isModalAberto;

        private Guid _condutorEmEdicaoId = Guid.Empty;

        // ====================================================================
        // 2. AQUI ESTÃO AS VARIÁVEIS COM OS TEXTOS DE ERRO EM PORTUGUÊS!
        // Elas substituem o antigo "NovoCondutor"
        // ====================================================================
        [ObservableProperty]
        [Required(ErrorMessage = "O preenchimento do Nome é obrigatório.")]
        [MinLength(3, ErrorMessage = "O Nome deve ter pelo menos 3 caracteres.")]
        private string _nome = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "O preenchimento do CPF é obrigatório.")]
        [Cpf(ErrorMessage = "O CPF informado não é válido. Verifique os números.")]
        private string _cpf = string.Empty;

        public CondutoresViewModel(IMediator mediator)
        {
            _mediator = mediator;
            _ = CarregarListaAsync();
        }

        private async Task CarregarListaAsync()
        {
            var lista = await _mediator.Send(new ListarCondutoresQuery());
            Condutores = new ObservableCollection<Condutor>(lista);
        }

        [RelayCommand]
        private void AbrirModal()
        {
            _condutorEmEdicaoId = Guid.Empty;
            Nome = string.Empty;
            Cpf = string.Empty;

            // 3. Força a validação ao abrir para já pintar os campos de vermelho
            ValidateAllProperties();

            IsModalAberto = true;
        }

        [RelayCommand]
        private void FecharModal() => IsModalAberto = false;

        [RelayCommand]
        private async Task Salvar()
        {
            // Dispara a validação antes de tentar salvar
            ValidateAllProperties();

            // Se ainda houver erros (campos vermelhos), não faz nada
            if (HasErrors) return;

            // Se passou na validação, montamos o objeto Condutor para mandar pro Banco
            var condutor = new Condutor
            {
                Id = _condutorEmEdicaoId,
                Nome = Nome,
                Cpf = Cpf
            };

            await _mediator.Send(new SalvarCondutorCommand(condutor));
            await CarregarListaAsync();
            IsModalAberto = false;
        }

        [RelayCommand]
        private async Task Excluir(Guid id)
        {
            await _mediator.Send(new ExcluirCondutorCommand(id));
            await CarregarListaAsync();
        }

        [RelayCommand]
        private void Editar(Condutor condutor)
        {
            _condutorEmEdicaoId = condutor.Id;
            Nome = condutor.Nome;
            Cpf = condutor.Cpf;

            ValidateAllProperties();

            IsModalAberto = true;
        }
    }
}