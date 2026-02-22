using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Cadastros;
using CoreMDFe.Application.Features.Manifestos;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using CoreMDFe.Application.Features.Consultas;
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
        public ObservableCollection<DocumentoMDFeDto> DocumentosFiscais { get; } = new();
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
        private int _modalSelecionadoIndex = 0;
        public bool IsModalRodoviario => ModalSelecionadoIndex == 0;

        // Dados Rodoviário Básicos
        public ObservableCollection<Veiculo> VeiculosDisponiveis { get; } = new();
        public ObservableCollection<Condutor> CondutoresDisponiveis { get; } = new();
        [ObservableProperty] private Veiculo? _veiculoSelecionado;
        [ObservableProperty] private Condutor? _condutorSelecionado;

        // Opcionais: Reboques
        [ObservableProperty] private bool _isReboquesAberto;
        [ObservableProperty] private Veiculo? _reboque1Selecionado;
        [ObservableProperty] private Veiculo? _reboque2Selecionado;
        [ObservableProperty] private Veiculo? _reboque3Selecionado;

        // Opcionais: Seguro
        [ObservableProperty] private bool _isSeguroAberto;
        [ObservableProperty] private string _seguradoraCnpj = string.Empty;
        [ObservableProperty] private string _seguradoraNome = string.Empty;
        [ObservableProperty] private string _numeroApolice = string.Empty;
        [ObservableProperty] private string _numeroAverbacao = string.Empty;

        // Opcionais: Produto Predominante
        [ObservableProperty] private bool _isProdutoPredominanteAberto;
        public string[] ListaTiposCarga { get; } = { "01-Granel sólido", "02-Granel líquido", "03-Frigorificada", "04-Conteinerizada", "05-Carga Geral", "06-Neogranel", "07-Perigosa (Sólido)", "08-Perigosa (Líquido)", "09-Perigosa (Frigorificada)", "10-Perigosa (Conteinerizada)", "11-Perigosa (Carga Geral)", "12-Granel pressurizada" };
        [ObservableProperty] private int _tipoCargaIndex = 4; // Carga Geral
        [ObservableProperty] private string _nomeProdutoPredominante = string.Empty;
        [ObservableProperty] private string _ncmProduto = string.Empty;

        // Opcionais: CIOT / Vale Pedágio
        [ObservableProperty] private bool _isCiotValePedagioAberto;
        [ObservableProperty] private string _ciot = string.Empty;
        [ObservableProperty] private string _cpfCnpjCiot = string.Empty;
        [ObservableProperty] private string _cnpjFornecedorValePedagio = string.Empty;
        [ObservableProperty] private string _cnpjPagadorValePedagio = string.Empty;

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

                foreach (var file in files) ProcessarArquivoXml(file.Path.LocalPath);
            }
        }

        private void ProcessarArquivoXml(string caminhoArquivo)
        {
            try
            {
                var doc = XDocument.Load(caminhoArquivo);
                var isNFe = doc.Descendants().Any(x => x.Name.LocalName == "infNFe");
                var isCTe = doc.Descendants().Any(x => x.Name.LocalName == "infCte");

                if (isNFe) ExtrairDadosNFe(doc);
                else if (isCTe) ExtrairDadosCTe(doc);

                AtualizarTotaisEResumos();
            }
            catch (Exception ex) { Console.WriteLine($"Erro ao ler XML {caminhoArquivo}: {ex.Message}"); }
        }

        private void ExtrairDadosNFe(XDocument doc)
        {
            var infNFe = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infNFe");
            if (infNFe == null) return;

            var chave = infNFe.Attribute("Id")?.Value.Replace("NFe", "");
            if (string.IsNullOrEmpty(chave) || DocumentosFiscais.Any(d => d.Chave == chave)) return;

            var vNf = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "vNF")?.Value;
            var pesoB = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "pesoB")?.Value;

            var enderEmit = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "emit")?.Descendants().FirstOrDefault(x => x.Name.LocalName == "enderEmit");
            var enderDest = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dest")?.Descendants().FirstOrDefault(x => x.Name.LocalName == "enderDest");

            // === NOVO: Extrair o Produto Predominante (NFe) ===
            var prod = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "prod");
            if (prod != null && string.IsNullOrEmpty(NomeProdutoPredominante))
            {
                NomeProdutoPredominante = prod.Elements().FirstOrDefault(x => x.Name.LocalName == "xProd")?.Value ?? "";
                NcmProduto = prod.Elements().FirstOrDefault(x => x.Name.LocalName == "NCM")?.Value ?? "";

                // Se achou, já abre a aba de Produto para o usuário ver a mágica
                if (!string.IsNullOrEmpty(NomeProdutoPredominante)) IsProdutoPredominanteAberto = true;
            }

            var dto = new DocumentoMDFeDto
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
            var infQ = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infQ" && x.Elements().Any(e => e.Name.LocalName == "cUnid" && e.Value == "01"));
            var qCarga = infQ?.Elements().FirstOrDefault(x => x.Name.LocalName == "qCarga")?.Value;

            var ide = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "ide");

            // === NOVO: Extrair o Produto Predominante (CTe) ===
            var infCarga = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infCarga");
            if (infCarga != null && string.IsNullOrEmpty(NomeProdutoPredominante))
            {
                NomeProdutoPredominante = infCarga.Elements().FirstOrDefault(x => x.Name.LocalName == "proPred")?.Value ?? "";

                // Se achou, já abre a aba de Produto
                if (!string.IsNullOrEmpty(NomeProdutoPredominante)) IsProdutoPredominanteAberto = true;
            }

            var dto = new DocumentoMDFeDto
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

            if (DocumentosFiscais.Any())
            {
                UfCarregamento = DocumentosFiscais.First().UfCarregamento;
                UfDescarregamento = DocumentosFiscais.Last().UfDescarga;
            }

            OnPropertyChanged(nameof(ResumoCidadesCarregamento));
            OnPropertyChanged(nameof(ResumoCidadesDescarregamento));
        }

        [RelayCommand]
        private async Task Emitir()
        {
            if (VeiculoSelecionado == null || CondutorSelecionado == null)
            {
                MensagemProcessamento = "Por favor, selecione ao menos o Veículo de Tração e o Condutor Principal no Passo 3.";
                return;
            }

            IsProcessando = true;

            // ==============================================================
            // 1. VERIFICAR STATUS DA SEFAZ (Feedback Inteligente)
            // ==============================================================
            MensagemProcessamento = "Verificando disponibilidade dos servidores da SEFAZ...";

            // Invoca a nossa query que checa o Webservice
            var statusSefaz = await _mediator.Send(new ConsultarStatusServicoQuery());

            if (!statusSefaz.Online)
            {
                // Se a sefaz tiver offline, nem tenta enviar o xml
                MensagemProcessamento = $"⚠️ SEFAZ Indisponível no momento.\nMotivo: {statusSefaz.Mensagem}\nTente novamente em alguns minutos.";
                IsProcessando = false;
                return;
            }

            // ==============================================================
            // 2. SEFAZ ONLINE: ENVIAR MANIFESTO
            // ==============================================================
            MensagemProcessamento = "Sefaz Operacional! Assinando e transmitindo o MDF-e...";

            var command = new EmitirManifestoCommand(
                EmpresaId: _empresaAtualId,
                Documentos: DocumentosFiscais.ToList(),
                UfCarregamento: UfCarregamento,
                UfDescarregamento: UfDescarregamento,
                TipoEmitente: TipoEmitenteIndex + 1, // 1, 2 ou 3
                TipoTransportador: TipoTransportadorIndex, // 0 a 3
                Modal: 1, // Rodoviário Fixo
                UfsPercurso: UfsPercurso,
                DataInicioViagem: DataInicioViagem,
                IsCanalVerde: IsCanalVerde,
                IsCarregamentoPosterior: IsCarregamentoPosterior,
                VeiculoTracao: VeiculoSelecionado,
                Condutor: CondutorSelecionado,
                Reboque1: Reboque1Selecionado,
                Reboque2: Reboque2Selecionado,
                Reboque3: Reboque3Selecionado,
                HasSeguro: IsSeguroAberto,
                SeguradoraCnpj: SeguradoraCnpj,
                SeguradoraNome: SeguradoraNome,
                NumeroApolice: NumeroApolice,
                NumeroAverbacao: NumeroAverbacao,
                HasProdutoPredominante: IsProdutoPredominanteAberto,
                TipoCarga: (TipoCargaIndex + 1).ToString("D2"),
                NomeProdutoPredominante: NomeProdutoPredominante,
                NcmProduto: NcmProduto,
                HasCiotValePedagio: IsCiotValePedagioAberto,
                Ciot: Ciot,
                CpfCnpjCiot: CpfCnpjCiot,
                CnpjFornecedorValePedagio: CnpjFornecedorValePedagio,
                CnpjPagadorValePedagio: CnpjPagadorValePedagio
            );

            var result = await _mediator.Send(command);

            MensagemProcessamento = result.Sucesso ? "✅ MDF-e Autorizado com Sucesso!" : $"❌ Rejeição SEFAZ: {result.Mensagem}";
            XmlEnvio = result.XmlEnvio;
            XmlRetorno = result.XmlRetorno;

            IsProcessando = false;
        }
    }
}