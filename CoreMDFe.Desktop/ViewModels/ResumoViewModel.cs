using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Consultas;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MediatR;
using Serilog;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class ResumoViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        // --- ESTATÍSTICAS MENSAIS (As que você já tinha) ---
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

        // --- NOVAS MÉTRICAS DE BI (Últimos 7 Dias) ---
        [ObservableProperty] private string _mediaDiariaDFes = "0";
        [ObservableProperty] private string _mediaDiariaPeso = "0 kg";
        [ObservableProperty] private string _mediaDiariaValor = "R$ 0,00";
        [ObservableProperty] private string _totalPesoPeriodo = "0 kg";
        [ObservableProperty] private string _totalValorPeriodo = "R$ 0,00";

        [ObservableProperty]
        private ObservableCollection<RankingCidadeDto> _cidadesRanking = new();

        // --- GRÁFICO (LiveCharts) ---
        [ObservableProperty] private ISeries[] _seriesGrafico = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] _eixosXGrafico = Array.Empty<Axis>();

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
                // 1. Carrega Resumo Mensal (Aba Superior)
                var stats = await _mediator.Send(new ConsultarEstatisticasResumoQuery());
                TotalMesAtual = stats.TotalMesAtual;
                Autorizados = stats.Autorizados;
                Encerrados = stats.Encerrados;
                Cancelados = stats.Cancelados;
                Rejeitados = stats.Rejeitados;
                EmAberto = stats.EmAberto;

                // 2. Carrega Métricas de BI dos Últimos 7 Dias (Aba Inferior)
                var metricas = await _mediator.Send(new ConsultarMetricasResumoQuery());

                MediaDiariaDFes = metricas.MediaDiariaDFes.ToString("F1");
                MediaDiariaPeso = metricas.MediaDiariaPeso.ToString("N2") + " kg";
                MediaDiariaValor = metricas.MediaDiariaValor.ToString("C2");
                TotalPesoPeriodo = metricas.TotalPesoPeriodo.ToString("N2") + " kg";
                TotalValorPeriodo = metricas.TotalValorPeriodo.ToString("C2");

                CidadesRanking = new ObservableCollection<RankingCidadeDto>(metricas.CidadesDescarregamento);

                // 3. Monta as Linhas do Gráfico
                SeriesGrafico = new ISeries[]
                {
                    new LineSeries<int>
                    {
                        Values = metricas.EvolucaoDiaria.Select(x => x.QuantidadeManifestos).ToArray(),
                        Name = "MDF-es Emitidos",
                        Fill = new SolidColorPaint(SKColors.Purple.WithAlpha(30)), // Tom combinando com o roxo do sistema
                        Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 3 },
                        GeometrySize = 10,
                        GeometryStroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 2 }
                    }
                };

                EixosXGrafico = new Axis[]
                {
                    new Axis
                    {
                        Labels = metricas.EvolucaoDiaria.Select(x => x.Data).ToArray(),
                        LabelsRotation = 45,
                        TextSize = 12
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error($"[RESUMO] Falha ao carregar dados: {ex.Message}");
            }
        }
    }
}