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
using System.Collections.Generic;
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

        public string[] ListaUFs { get; } = { "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO" };

        public string[] ListaTiposEmitente { get; } = { "1 - Prestador de Serviço de Transporte", "2 - Transportador de Carga Própria", "3 - CTe Globalizado" };
        [ObservableProperty] private int _tipoEmitenteIndex;

        public string[] ListaTiposTransportador { get; } = { "0 - Não Informar", "1 - ETC (Empresa)", "2 - TAC (Autônomo)", "3 - CTC (Cooperativa)" };
        [ObservableProperty] private int _tipoTransportadorIndex;

        public string[] ListaTiposEmissao { get; } = { "1 - Normal", "2 - Contingência" };
        [ObservableProperty] private int _tipoEmissaoIndex;

        [ObservableProperty] private bool _isPercursoAberto;
        [ObservableProperty] private string _ufsPercurso = string.Empty;
        [ObservableProperty] private DateTimeOffset? _dataInicioViagem;
        [ObservableProperty] private bool _isCanalVerde;
        [ObservableProperty] private bool _isCarregamentoPosterior;

        // --- PASSO 3: TRANSPORTE ---
        public string[] ListaModais { get; } = { "1 - Rodoviário", "2 - Aéreo", "3 - Aquaviário", "4 - Ferroviário" };
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsModalRodoviario))] private int _modalSelecionadoIndex;
        public bool IsModalRodoviario => ModalSelecionadoIndex == 0;

        // Dados do Responsável Técnico
        [ObservableProperty] private string _respTecCnpj = string.Empty;
        [ObservableProperty] private string _respTecNome = string.Empty;
        [ObservableProperty] private string _respTecTelefone = string.Empty;
        [ObservableProperty] private string _respTecEmail = string.Empty;

        // Dados Rodoviário Básicos
        public ObservableCollection<Veiculo> VeiculosDisponiveis { get; } = new();
        public ObservableCollection<Condutor> CondutoresDisponiveis { get; } = new();
        [ObservableProperty] private Veiculo? _veiculoSelecionado;
        [ObservableProperty] private Condutor? _condutorSelecionado;

        // Opcionais
        [ObservableProperty] private bool _isReboquesAberto;
        [ObservableProperty] private Veiculo? _reboque1Selecionado;
        [ObservableProperty] private Veiculo? _reboque2Selecionado;
        [ObservableProperty] private Veiculo? _reboque3Selecionado;

        [ObservableProperty] private bool _isSeguroAberto;
        [ObservableProperty] private string _seguradoraCnpj = string.Empty;
        [ObservableProperty] private string _seguradoraNome = string.Empty;
        [ObservableProperty] private string _numeroApolice = string.Empty;
        [ObservableProperty] private string _numeroAverbacao = string.Empty;

        [ObservableProperty] private bool _isProdutoPredominanteAberto;
        public string[] ListaTiposCarga { get; } = { "01-Granel sólido", "02-Granel líquido", "03-Frigorificada", "04-Conteinerizada", "05-Carga Geral", "06-Neogranel", "07-Perigosa (Sólido)", "08-Perigosa (Líquido)", "09-Perigosa (Frigorificada)", "10-Perigosa (Conteinerizada)", "11-Perigosa (Carga Geral)", "12-Granel pressurizada" };
        [ObservableProperty] private int _tipoCargaIndex = 4;
        [ObservableProperty] private string _nomeProdutoPredominante = string.Empty;
        [ObservableProperty] private string _ncmProduto = string.Empty;

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
        [ObservableProperty] private Guid? _manifestoAutorizadoId;
        [ObservableProperty] private bool _isAutorizado;

        public EmissaoViewModel(IMediator mediator, IAppDbContext dbContext)
        {
            _mediator = mediator;
            _dbContext = dbContext;
            _ = CarregarDadosIniciais();
        }

        private async Task CarregarDadosIniciais()
        {
            try
            {
                Console.WriteLine("[WIZARD] Carregando cadastros e configurações padrões...");

                var veiculos = await _mediator.Send(new ListarVeiculosQuery());
                foreach (var v in veiculos) VeiculosDisponiveis.Add(v);

                var condutores = await _mediator.Send(new ListarCondutoresQuery());
                foreach (var c in condutores) CondutoresDisponiveis.Add(c);

                var empresa = await _dbContext.Empresas.Include(e => e.Configuracao).FirstOrDefaultAsync();
                if (empresa != null)
                {
                    _empresaAtualId = empresa.Id;
                    if (empresa.Configuracao != null)
                    {
                        Console.WriteLine($"[WIZARD] Config encontrada. Emitente: {empresa.Configuracao.TipoEmitentePadrao}, Modal: {empresa.Configuracao.ModalidadePadrao}");

                        // CONVERSÃO CORRETA: O banco salva 1, 2, 3... mas a UI precisa de 0, 1, 2...
                        // Usamos Math.Max(0, valor - 1) para garantir que nunca fique negativo se o banco tiver 0
                        TipoEmitenteIndex = Math.Max(0, empresa.Configuracao.TipoEmitentePadrao - 1);
                        TipoEmissaoIndex = Math.Max(0, empresa.Configuracao.TipoEmissaoPadrao - 1);
                        ModalSelecionadoIndex = Math.Max(0, empresa.Configuracao.ModalidadePadrao - 1);

                        // O Tipo de Transportador costuma ser 0 (Não informar), 1, 2, 3. Aqui é 1 pra 1.
                        TipoTransportadorIndex = empresa.Configuracao.TipoTransportadorPadrao;

                        RespTecCnpj = empresa.Configuracao.RespTecCnpj ?? "";
                        RespTecNome = empresa.Configuracao.RespTecNome ?? "";
                        RespTecTelefone = empresa.Configuracao.RespTecTelefone ?? "";
                        RespTecEmail = empresa.Configuracao.RespTecEmail ?? "";
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"[WIZARD - ERRO] Falha ao inicializar: {ex.Message}"); }
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

        private void ProcessarArquivoXml(string caminho)
        {
            try
            {
                var doc = XDocument.Load(caminho);
                if (doc.Descendants().Any(x => x.Name.LocalName == "infNFe")) ExtrairDadosNFe(doc);
                else if (doc.Descendants().Any(x => x.Name.LocalName == "infCte")) ExtrairDadosCTe(doc);
                AtualizarTotaisEResumos();
            }
            catch (Exception ex) { Console.WriteLine($"Erro XML: {ex.Message}"); }
        }

        private void ExtrairDadosNFe(XDocument doc)
        {
            var infNFe = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infNFe");
            if (infNFe == null) return;
            var chave = infNFe.Attribute("Id")?.Value.Replace("NFe", "");
            if (string.IsNullOrEmpty(chave) || DocumentosFiscais.Any(d => d.Chave == chave)) return;

            var vNf = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "vNF")?.Value;
            var pesoB = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "pesoB")?.Value;
            var emit = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "emit")?.Descendants().FirstOrDefault(x => x.Name.LocalName == "enderEmit");
            var dest = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dest")?.Descendants().FirstOrDefault(x => x.Name.LocalName == "enderDest");

            var prod = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "prod");
            if (prod != null && string.IsNullOrEmpty(NomeProdutoPredominante))
            {
                NomeProdutoPredominante = prod.Elements().FirstOrDefault(x => x.Name.LocalName == "xProd")?.Value ?? "";
                NcmProduto = prod.Elements().FirstOrDefault(x => x.Name.LocalName == "NCM")?.Value ?? "";
                if (!string.IsNullOrEmpty(NomeProdutoPredominante)) IsProdutoPredominanteAberto = true;
            }

            DocumentosFiscais.Add(new DocumentoMDFeDto
            {
                Chave = chave,
                Tipo = 55,
                Valor = ParseDecimal(vNf),
                Peso = ParseDecimal(pesoB),
                IbgeCarregamento = ParseLong(emit?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMun")?.Value),
                MunicipioCarregamento = emit?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMun")?.Value?.ToUpper() ?? "",
                UfCarregamento = emit?.Elements().FirstOrDefault(x => x.Name.LocalName == "UF")?.Value?.ToUpper() ?? "",
                IbgeDescarga = ParseLong(dest?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMun")?.Value),
                MunicipioDescarga = dest?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMun")?.Value?.ToUpper() ?? "",
                UfDescarga = dest?.Elements().FirstOrDefault(x => x.Name.LocalName == "UF")?.Value?.ToUpper() ?? ""
            });
            QuantidadeNFe++;
        }

        private void ExtrairDadosCTe(XDocument doc)
        {
            var infCte = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infCte");
            if (infCte == null) return;
            var chave = infCte.Attribute("Id")?.Value.Replace("CTe", "");
            if (string.IsNullOrEmpty(chave) || DocumentosFiscais.Any(d => d.Chave == chave)) return;

            var vCarga = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "vCarga")?.Value;
            var qCarga = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infQ" && x.Elements().Any(e => e.Name.LocalName == "cUnid" && e.Value == "01"))?.Elements().FirstOrDefault(x => x.Name.LocalName == "qCarga")?.Value;
            var ide = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "ide");

            var infCarga = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infCarga");
            if (infCarga != null && string.IsNullOrEmpty(NomeProdutoPredominante))
            {
                NomeProdutoPredominante = infCarga.Elements().FirstOrDefault(x => x.Name.LocalName == "proPred")?.Value ?? "";
                if (!string.IsNullOrEmpty(NomeProdutoPredominante)) IsProdutoPredominanteAberto = true;
            }

            DocumentosFiscais.Add(new DocumentoMDFeDto
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
            });
            QuantidadeCTe++;
        }

        private decimal ParseDecimal(string? v) => decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 0;
        private long ParseLong(string? v) => long.TryParse(v, out var r) ? r : 0;

        private void AtualizarTotaisEResumos()
        {
            ValorTotalCarga = DocumentosFiscais.Sum(d => d.Valor);
            PesoTotalCarga = DocumentosFiscais.Sum(d => d.Peso);
            if (DocumentosFiscais.Any())
            {
                UfCarregamento = DocumentosFiscais.First().UfCarregamento;
                UfDescarregamento = DocumentosFiscais.Last().UfDescarga;
            }
        }

        [RelayCommand]
        private async Task Emitir()
        {
            if (VeiculoSelecionado == null || CondutorSelecionado == null)
            {
                MensagemProcessamento = "Selecione Veículo e Condutor no Passo 3."; return;
            }

            IsProcessando = true;
            MensagemProcessamento = "Verificando disponibilidade da SEFAZ...";

            var statusSefaz = await _mediator.Send(new ConsultarStatusServicoQuery());
            if (!statusSefaz.Online)
            {
                MensagemProcessamento = $"⚠️ SEFAZ Indisponível: {statusSefaz.Mensagem}";
                IsProcessando = false; return;
            }

            MensagemProcessamento = "Transmitindo...";

            var command = new EmitirManifestoCommand(
                EmpresaId: _empresaAtualId,
                Documentos: DocumentosFiscais.ToList(),
                UfCarregamento: UfCarregamento,
                UfDescarregamento: UfDescarregamento,
                // CONVERSÃO CORRETA: UI 0 -> SEFAZ 1
                TipoEmitente: TipoEmitenteIndex + 1,
                TipoTransportador: TipoTransportadorIndex,
                Modal: ModalSelecionadoIndex + 1,
                TipoEmissao: TipoEmissaoIndex + 1,
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
                CnpjPagadorValePedagio: CnpjPagadorValePedagio,
                RespTecCnpj: RespTecCnpj,
                RespTecNome: RespTecNome,
                RespTecTelefone: RespTecTelefone,
                RespTecEmail: RespTecEmail
            );

            var result = await _mediator.Send(command);

            MensagemProcessamento = result.Sucesso ? "✅ MDF-e Autorizado!" : $"❌ Rejeição: {result.Mensagem}";
            XmlEnvio = result.XmlEnvio;
            XmlRetorno = result.XmlRetorno;
            ManifestoAutorizadoId = result.ManifestoId;
            IsAutorizado = result.Sucesso;
            IsProcessando = false;
        }

        [RelayCommand]
        private async Task Imprimir()
        {
            if (ManifestoAutorizadoId == null) return;

            MensagemProcessamento = "Gerando PDF...";
            var result = await _mediator.Send(new GerarPdfManifestoCommand(ManifestoAutorizadoId.Value));

            if (result.Sucesso)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(result.CaminhoPdf) { UseShellExecute = true });
                MensagemProcessamento = "✅ PDF Gerado com sucesso!";
            }
            else
            {
                MensagemProcessamento = $"❌ Falha ao gerar PDF: {result.Mensagem}";
            }
        }
    }
}