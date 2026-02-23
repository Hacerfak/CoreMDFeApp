using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Consultas;
using MediatR;
using System;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class ResumoViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        [ObservableProperty] private int _totalMesAtual;
        [ObservableProperty] private int _autorizados;
        [ObservableProperty] private int _encerrados;
        [ObservableProperty] private int _cancelados;
        [ObservableProperty] private int _rejeitados;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TemManifestosEmAberto))]
        private int _emAberto;

        public bool TemManifestosEmAberto => EmAberto > 0;

        [ObservableProperty] private string _mesAtualTexto = string.Empty;

        public ResumoViewModel(IMediator mediator)
        {
            _mediator = mediator;
            MesAtualTexto = DateTime.Now.ToString("MMMM 'de' yyyy").ToUpper();
            _ = CarregarDados();
        }

        [RelayCommand]
        public async Task CarregarDados()
        {
            try
            {
                var stats = await _mediator.Send(new ConsultarEstatisticasResumoQuery());

                TotalMesAtual = stats.TotalMesAtual;
                Autorizados = stats.Autorizados;
                Encerrados = stats.Encerrados;
                Cancelados = stats.Cancelados;
                Rejeitados = stats.Rejeitados;
                EmAberto = stats.EmAberto;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RESUMO - ERRO] Falha ao carregar estatísticas: {ex.Message}");
            }
        }
    }
}