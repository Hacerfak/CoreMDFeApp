using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Consultas;
using CoreMDFe.Application.Mediator;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    // Classe auxiliar para as colunas do gráfico nativo
    public class BarChartItem
    {
        public string Data { get; set; } = string.Empty;
        public int Valor { get; set; }
        public double AlturaBarra { get; set; }
        public bool TemValor => Valor > 0;
    }

    public partial class ResumoViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        // --- ESTATÍSTICAS MENSAIS ---
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

        // --- MÉTRICAS DE BI ---
        [ObservableProperty] private string _mediaDiariaDFes = "0";
        [ObservableProperty] private string _mediaDiariaPeso = "0 kg";
        [ObservableProperty] private string _mediaDiariaValor = "R$ 0,00";
        [ObservableProperty] private string _totalPesoPeriodo = "0 kg";
        [ObservableProperty] private string _totalValorPeriodo = "R$ 0,00";

        [ObservableProperty]
        private ObservableCollection<RankingCidadeDto> _cidadesRanking = new();

        // --- PROPRIEDADE PARA O GRÁFICO NATIVO (O que estava faltando) ---
        [ObservableProperty]
        private ObservableCollection<BarChartItem> _barrasGrafico = new();

        public ResumoViewModel(IMediator mediator)
        {
            _mediator = mediator;
            MesAtualTexto = DateTime.Now.ToString("MMMM 'de' yyyy").ToUpper();
            _ = CarregarDados();
        }

        [RelayCommand]
        public async Task CarregarDados()
        {
            Log.Information("[RESUMO] Carregando estatísticas e métricas de BI...");
            try
            {
                var stats = await _mediator.Send(new ConsultarEstatisticasResumoQuery());
                TotalMesAtual = stats.TotalMesAtual;
                Autorizados = stats.Autorizados;
                Encerrados = stats.Encerrados;
                Cancelados = stats.Cancelados;
                Rejeitados = stats.Rejeitados;
                EmAberto = stats.EmAberto;

                var metricas = await _mediator.Send(new ConsultarMetricasResumoQuery());

                MediaDiariaDFes = metricas.MediaDiariaDFes.ToString("F1");
                MediaDiariaPeso = metricas.MediaDiariaPeso.ToString("N2") + " kg";
                MediaDiariaValor = metricas.MediaDiariaValor.ToString("C2");
                TotalPesoPeriodo = metricas.TotalPesoPeriodo.ToString("N2") + " kg";
                TotalValorPeriodo = metricas.TotalValorPeriodo.ToString("C2");

                CidadesRanking = new ObservableCollection<RankingCidadeDto>(metricas.CidadesDescarregamento);

                // --- LÓGICA DO GRÁFICO NATIVO ---
                var maxValor = metricas.EvolucaoDiaria.Any() ? metricas.EvolucaoDiaria.Max(x => x.QuantidadeManifestos) : 0;
                var maxAlturaPixels = 160.0;

                var listaBarras = metricas.EvolucaoDiaria.Select(x => new BarChartItem
                {
                    Data = x.Data,
                    Valor = x.QuantidadeManifestos,
                    AlturaBarra = maxValor > 0 ? (x.QuantidadeManifestos / (double)maxValor) * maxAlturaPixels : 0
                }).ToList();

                // Atualiza a coleção que a View (XAML) está observando
                BarrasGrafico = new ObservableCollection<BarChartItem>(listaBarras);
            }
            catch (Exception ex)
            {
                Log.Error($"[RESUMO] Falha ao carregar dados: {ex.Message}");
            }
        }
    }
}