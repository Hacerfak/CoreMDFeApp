using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Cadastros;
using CoreMDFe.Application.Features.Manifestos;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CoreMDFe.Desktop.ViewModels
{
    // DTO para armazenar os dados lidos dos XMLs
    public class DocumentoFiscalDto
    {
        public string Chave { get; set; } = string.Empty;
        public int Tipo { get; set; } // 55 = NFe, 57 = CTe
        public decimal Valor { get; set; }
        public decimal Peso { get; set; }

        public long IbgeCarregamento { get; set; }
        public string MunicipioCarregamento { get; set; } = string.Empty;
        public string UfCarregamento { get; set; } = string.Empty;

        public long IbgeDescarga { get; set; }
        public string MunicipioDescarga { get; set; } = string.Empty;
        public string UfDescarga { get; set; } = string.Empty;

        // O que vai aparecer na ListBox da UI
        public override string ToString() => $"{(Tipo == 55 ? "NF-e" : "CT-e")} - {Chave} | Rota: {UfCarregamento} -> {UfDescarga}";
    }

    public partial class EmissaoViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly IAppDbContext _dbContext;
        private Guid _empresaAtualId;

        // --- CONTROLE DO WIZARD ---
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPasso1))]
        [NotifyPropertyChangedFor(nameof(IsPasso2))]
        [NotifyPropertyChangedFor(nameof(IsPasso3))]
        [NotifyPropertyChangedFor(nameof(IsPasso4))]
        private int _passoAtual = 1;

        public bool IsPasso1 => PassoAtual == 1;
        public bool IsPasso2 => PassoAtual == 2;
        public bool IsPasso3 => PassoAtual == 3;
        public bool IsPasso4 => PassoAtual == 4;

        // --- PASSO 1: DOCUMENTOS ---
        public ObservableCollection<DocumentoFiscalDto> DocumentosFiscais { get; } = new();
        [ObservableProperty] private decimal _valorTotalCarga;
        [ObservableProperty] private decimal _pesoTotalCarga;
        [ObservableProperty] private int _quantidadeNFe;
        [ObservableProperty] private int _quantidadeCTe;

        // --- PASSO 2: ROTEIRO E CONFIGURAÇÃO ---
        [ObservableProperty] private string _ufCarregamento = "";
        [ObservableProperty] private string _ufDescarregamento = "";

        public string ResumoCidadesCarregamento => string.Join(", ", DocumentosFiscais.Select(d => d.MunicipioCarregamento).Where(m => !string.IsNullOrEmpty(m)).Distinct());
        public string ResumoCidadesDescarregamento => string.Join(", ", DocumentosFiscais.Select(d => d.MunicipioDescarga).Where(m => !string.IsNullOrEmpty(m)).Distinct());

        public string[] ListaUFs { get; } = { "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO" };
        public string[] ListaTiposEmitente { get; } = { "1 - Prestador de Serviço de Transporte", "2 - Transportador de Carga Própria", "3 - CTe Globalizado" };
        [ObservableProperty] private int _tipoEmitenteIndex = 0;

        public string[] ListaTiposTransportador { get; } = { "Nenhum (Não Informar)", "1 - ETC (Empresa)", "2 - TAC (Autônomo)", "3 - CTC (Cooperativa)" };
        [ObservableProperty] private int _tipoTransportadorIndex = 0;

        [ObservableProperty] private bool _isPercursoAberto;
        [ObservableProperty] private string _ufsPercurso = string.Empty;
        [ObservableProperty] private DateTimeOffset? _dataInicioViagem;
        [ObservableProperty] private bool _isCanalVerde;
        [ObservableProperty] private bool _isCarregamentoPosterior;

        // --- PASSO 3: TRANSPORTE MULTI-MODAL ---
        public string[] ListaModais { get; } = { "1 - Rodoviário", "2 - Aéreo", "3 - Aquaviário", "4 - Ferroviário" };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsModalRodoviario))]
        [NotifyPropertyChangedFor(nameof(IsModalAereo))]
        [NotifyPropertyChangedFor(nameof(IsModalAquaviario))]
        [NotifyPropertyChangedFor(nameof(IsModalFerroviario))]
        private int _modalSelecionadoIndex = 0;

        public bool IsModalRodoviario => ModalSelecionadoIndex == 0;
        public bool IsModalAereo => ModalSelecionadoIndex == 1;
        public bool IsModalAquaviario => ModalSelecionadoIndex == 2;
        public bool IsModalFerroviario => ModalSelecionadoIndex == 3;

        // Dados Rodoviário
        public ObservableCollection<Veiculo> VeiculosDisponiveis { get; } = new();
        public ObservableCollection<Condutor> CondutoresDisponiveis { get; } = new();
        [ObservableProperty] private Veiculo? _veiculoSelecionado;
        [ObservableProperty] private Condutor? _condutorSelecionado;

        // --- PASSO 4: RESUMO E FEEDBACK ---
        [ObservableProperty] private bool _isProcessando;
        [ObservableProperty] private string _mensagemProcessamento = "";
        [ObservableProperty] private string _xmlEnvio = "";
        [ObservableProperty] private string _xmlRetorno = "";

        public EmissaoViewModel(IMediator mediator, IAppDbContext dbContext)
        {
            _mediator = mediator;
            _dbContext = dbContext;
            _ = CarregarCadastrosDb();
        }

        private async Task CarregarCadastrosDb()
        {
            var veiculos = await _mediator.Send(new ListarVeiculosQuery());
            foreach (var v in veiculos) VeiculosDisponiveis.Add(v);

            var condutores = await _mediator.Send(new ListarCondutoresQuery());
            foreach (var c in condutores) CondutoresDisponiveis.Add(c);

            var empresa = await _dbContext.Empresas.FirstOrDefaultAsync();
            if (empresa != null) _empresaAtualId = empresa.Id;
        }

        [RelayCommand] private void Avancar() { if (PassoAtual < 4) PassoAtual++; }
        [RelayCommand] private void Voltar() { if (PassoAtual > 1) PassoAtual--; }

        [RelayCommand]
        private async Task ProcurarDocumentos()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Selecione os arquivos XML (NF-e ou CT-e)",
                    AllowMultiple = true,
                    FileTypeFilter = new[] { new FilePickerFileType("Arquivos XML") { Patterns = new[] { "*.xml" } } }
                });

                foreach (var file in files)
                {
                    ProcessarArquivoXml(file.Path.LocalPath);
                }
            }
        }

        // ==============================================================
        // LÓGICA DE EXTRAÇÃO DOS DADOS DO XML (NF-E e CT-E)
        // ==============================================================
        private void ProcessarArquivoXml(string caminhoArquivo)
        {
            try
            {
                var doc = XDocument.Load(caminhoArquivo);

                // Identifica se é NFe ou CTe independente do namespace usando LocalName
                var isNFe = doc.Descendants().Any(x => x.Name.LocalName == "infNFe");
                var isCTe = doc.Descendants().Any(x => x.Name.LocalName == "infCte");

                if (isNFe) ExtrairDadosNFe(doc);
                else if (isCTe) ExtrairDadosCTe(doc);

                AtualizarTotaisEResumos();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler XML {caminhoArquivo}: {ex.Message}");
            }
        }

        private void ExtrairDadosNFe(XDocument doc)
        {
            var infNFe = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infNFe");
            if (infNFe == null) return;

            var chave = infNFe.Attribute("Id")?.Value.Replace("NFe", "");
            if (string.IsNullOrEmpty(chave) || DocumentosFiscais.Any(d => d.Chave == chave)) return;

            var vNf = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "vNF")?.Value;
            var pesoB = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "pesoB")?.Value;

            // Origem (Emitente)
            var enderEmit = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "emit")?.Descendants().FirstOrDefault(x => x.Name.LocalName == "enderEmit");
            // Destino (Destinatário)
            var enderDest = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dest")?.Descendants().FirstOrDefault(x => x.Name.LocalName == "enderDest");

            var dto = new DocumentoFiscalDto
            {
                Chave = chave,
                Tipo = 55,
                Valor = ParseDecimal(vNf),
                Peso = ParseDecimal(pesoB),

                IbgeCarregamento = ParseLong(enderEmit?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMun")?.Value),
                MunicipioCarregamento = enderEmit?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMun")?.Value?.ToUpper() ?? "",
                UfCarregamento = enderEmit?.Elements().FirstOrDefault(x => x.Name.LocalName == "UF")?.Value?.ToUpper() ?? "",

                IbgeDescarga = ParseLong(enderDest?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMun")?.Value),
                MunicipioDescarga = enderDest?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMun")?.Value?.ToUpper() ?? "",
                UfDescarga = enderDest?.Elements().FirstOrDefault(x => x.Name.LocalName == "UF")?.Value?.ToUpper() ?? ""
            };

            DocumentosFiscais.Add(dto);
            QuantidadeNFe++;
        }

        private void ExtrairDadosCTe(XDocument doc)
        {
            var infCte = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infCte");
            if (infCte == null) return;

            var chave = infCte.Attribute("Id")?.Value.Replace("CTe", "");
            if (string.IsNullOrEmpty(chave) || DocumentosFiscais.Any(d => d.Chave == chave)) return;

            var vCarga = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "vCarga")?.Value;

            // Pega a TAG de Peso Bruto (cUnid = 01)
            var infQ = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infQ" && x.Elements().Any(e => e.Name.LocalName == "cUnid" && e.Value == "01"));
            var qCarga = infQ?.Elements().FirstOrDefault(x => x.Name.LocalName == "qCarga")?.Value;

            var ide = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "ide");

            var dto = new DocumentoFiscalDto
            {
                Chave = chave,
                Tipo = 57,
                Valor = ParseDecimal(vCarga),
                Peso = ParseDecimal(qCarga),

                IbgeCarregamento = ParseLong(ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMunEnv")?.Value),
                MunicipioCarregamento = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMunEnv")?.Value?.ToUpper() ?? "",
                UfCarregamento = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "UFEnv")?.Value?.ToUpper() ?? "",

                IbgeDescarga = ParseLong(ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMunFim")?.Value),
                MunicipioDescarga = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMunFim")?.Value?.ToUpper() ?? "",
                UfDescarga = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "UFFim")?.Value?.ToUpper() ?? ""
            };

            DocumentosFiscais.Add(dto);
            QuantidadeCTe++;
        }

        private decimal ParseDecimal(string? value) => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
        private long ParseLong(string? value) => long.TryParse(value, out var result) ? result : 0;

        private void AtualizarTotaisEResumos()
        {
            ValorTotalCarga = DocumentosFiscais.Sum(d => d.Valor);
            PesoTotalCarga = DocumentosFiscais.Sum(d => d.Peso);

            // Regra do Roteiro Automático:
            // A UF inicial é a da primeira nota inserida. A UF final é a da última.
            if (DocumentosFiscais.Any())
            {
                UfCarregamento = DocumentosFiscais.First().UfCarregamento;
                UfDescarregamento = DocumentosFiscais.Last().UfDescarga;
            }

            // Avisa a UI que as propriedades computadas mudaram
            OnPropertyChanged(nameof(ResumoCidadesCarregamento));
            OnPropertyChanged(nameof(ResumoCidadesDescarregamento));
        }

        [RelayCommand]
        private async Task Emitir()
        {
            IsProcessando = true;
            MensagemProcessamento = "Assinando e Transmitindo MDF-e para a SEFAZ...";

            // Aqui enviaremos os dados da EmissaoViewModel pro Request (faremos na próxima etapa!)
            var result = await _mediator.Send(new EmitirManifestoCommand(_empresaAtualId));

            MensagemProcessamento = result.Sucesso ? "MDF-e Autorizado com Sucesso!" : $"Rejeição: {result.Mensagem}";
            XmlEnvio = result.XmlEnvio;
            XmlRetorno = result.XmlRetorno;

            IsProcessando = false;
        }
    }
}