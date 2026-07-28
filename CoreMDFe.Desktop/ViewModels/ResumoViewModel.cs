using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Consultas;
using CoreMDFe.Application.Mediator;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
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

        // --- PROPRIEDADES DO LIVECHARTS2 ---
        [ObservableProperty]
        private ObservableCollection<ISeries> _series = new();

        [ObservableProperty]
        private Axis[] _xAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] _yAxes = Array.Empty<Axis>();

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

                // --- CONFIGURAÇÃO DO GRÁFICO DE LINHAS MODERNIZADO ---
                var maxValor = metricas.EvolucaoDiaria.Any() ? metricas.EvolucaoDiaria.Max(x => x.QuantidadeManifestos) : 0;
                var valores = metricas.EvolucaoDiaria.Select(x => x.QuantidadeManifestos).ToArray();
                var datas = metricas.EvolucaoDiaria.Select(x => x.Data).ToArray();

                Series = new ObservableCollection<ISeries>
{
    new LineSeries<int>
    {
        Values = valores,
        Name = "MDF-es Emitidos",
        Stroke = new SolidColorPaint(SKColor.Parse("#8C9EFF")) { StrokeThickness = 3 },
        Fill = new SolidColorPaint(SKColor.Parse("#8C9EFF").WithAlpha(40)),
        
        // VISUAL MODERNIZADO: 
        // Bolinhas menores e mais discretas. (Coloque 0 se preferir uma linha totalmente lisa)
        GeometrySize = 4,
        GeometryFill = new SolidColorPaint(SKColors.White),
        GeometryStroke = new SolidColorPaint(SKColor.Parse("#311B92")) { StrokeThickness = 2 },
        LineSmoothness = 0.5
    }
};

                XAxes = new Axis[]
                {
    new Axis
    {
        Labels = datas,
        LabelsPaint = new SolidColorPaint(SKColors.Gray),
        TextSize = 11,
        MinStep = 1,
        // Remove a linha de grade vertical para deixar mais limpo
        SeparatorsPaint = null
    }
                };

                YAxes = new Axis[]
                {
    new Axis
    {
        MinLimit = 0, // NUNCA permite números negativos
        MinStep = 1,  // FORÇA números inteiros (0, 1, 2, 3)
        // Se estiver tudo zerado, força o gráfico a ter uma altura visual até 5
        MaxLimit = maxValor == 0 ? 5 : null,

        LabelsPaint = new SolidColorPaint(SKColors.Gray),
        TextSize = 11,
        // Linhas horizontais bem suaves e sutis
        SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#F0F0F0")) { StrokeThickness = 1 }
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