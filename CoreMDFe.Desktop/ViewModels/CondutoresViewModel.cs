using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Cadastros;
using CoreMDFe.Core.Entities;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class CondutoresViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        [ObservableProperty] private ObservableCollection<Condutor> _condutores = new();
        [ObservableProperty] private bool _isModalAberto;
        [ObservableProperty] private Condutor _novoCondutor = new();

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
            NovoCondutor = new Condutor();
            IsModalAberto = true;
        }

        [RelayCommand]
        private void FecharModal() => IsModalAberto = false;

        [RelayCommand]
        private async Task Salvar()
        {
            if (string.IsNullOrWhiteSpace(NovoCondutor.Nome) || string.IsNullOrWhiteSpace(NovoCondutor.Cpf)) return;

            await _mediator.Send(new SalvarCondutorCommand(NovoCondutor));
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
            // Cria uma cópia para edição segura
            NovoCondutor = new Condutor
            {
                Id = condutor.Id,
                Nome = condutor.Nome,
                Cpf = condutor.Cpf,
                DataCriacao = condutor.DataCriacao
            };
            IsModalAberto = true;
        }
    }
}