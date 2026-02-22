using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Manifestos;
using CoreMDFe.Core.Entities;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class HistoricoViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        [ObservableProperty] private ObservableCollection<ManifestoEletronico> _manifestos = new();
        [ObservableProperty] private bool _estaCarregando;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasErro))]
        private string _mensagemErro = string.Empty;

        // Propriedades booleanas seguras para a Interface Gráfica
        public bool HasErro => !string.IsNullOrEmpty(MensagemErro);
        public bool IsListaVazia => Manifestos == null || Manifestos.Count == 0;

        public HistoricoViewModel(IMediator mediator)
        {
            _mediator = mediator;
            _ = CarregarHistorico();
        }

        [RelayCommand]
        public async Task CarregarHistorico()
        {
            EstaCarregando = true;
            MensagemErro = string.Empty;

            try
            {
                var lista = await _mediator.Send(new ListarManifestosQuery());
                Manifestos = new ObservableCollection<ManifestoEletronico>(lista);

                // Avisa a View para atualizar o texto de "lista vazia"
                OnPropertyChanged(nameof(IsListaVazia));
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao carregar do banco: {ex.Message}";
            }
            finally
            {
                EstaCarregando = false;
            }
        }
    }
}