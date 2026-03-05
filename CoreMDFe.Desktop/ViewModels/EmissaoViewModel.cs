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
        [ObservableProperty] private string _corFundoMensagem = "Transparent";
        [ObservableProperty] private string _corBordaMensagem = "Transparent";
        [ObservableProperty] private string _corTextoMensagem = "Black";
        [ObservableProperty] private string _iconeMensagem = "InformationOutline";
        private Guid _empresaAtualId;
        private string _empresaMunicipio = string.Empty;
        private string _empresaIbge = string.Empty;
        private string _empresaUf = string.Empty;
        private ConfiguracaoApp? _configuracaoAtual;

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

        [ObservableProperty] private string _ibgeCarregamentoManual = string.Empty;
        [ObservableProperty] private string _municipioCarregamentoManual = string.Empty;
        [ObservableProperty] private string _ibgeDescarregamentoManual = string.Empty;
        [ObservableProperty] private string _municipioDescarregamentoManual = string.Empty;

        public string ResumoCidadesCarregamento => string.Join(", ", DocumentosFiscais.Select(d => d.MunicipioCarregamento).Where(m => !string.IsNullOrEmpty(m)).Distinct());
        public string ResumoCidadesDescarregamento => string.Join(", ", DocumentosFiscais.Select(d => d.MunicipioDescarga).Where(m => !string.IsNullOrEmpty(m)).Distinct());

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
        [ObservableProperty] private bool _isCarregamentoPosterior; // Novo campo para o Passo 2

        // --- PASSO 3: TRANSPORTE ---
        public string[] ListaModais { get; } = { "1 - Rodoviário", "2 - Aéreo", "3 - Aquaviário", "4 - Ferroviário" };
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsModalRodoviario))] private int _modalSelecionadoIndex;
        public bool IsModalRodoviario => ModalSelecionadoIndex == 0;

        // Dados Rodoviário Básicos
        public ObservableCollection<Veiculo> VeiculosDisponiveis { get; } = new();
        public ObservableCollection<Condutor> CondutoresDisponiveis { get; } = new();
        [ObservableProperty] private Veiculo? _veiculoSelecionado;
        [ObservableProperty] private Condutor? _condutorSelecionado;

        // Opcionais (Avançados)
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
        [ObservableProperty] private string _ceanProduto = string.Empty;

        // CLASSE AUXILIAR DE RANQUEAMENTO
        private class ProdutoAgregado
        {
            public string ChaveDocumento { get; set; } = string.Empty;
            public string Nome { get; set; } = string.Empty;
            public string Ncm { get; set; } = string.Empty;
            public string Cean { get; set; } = string.Empty;
            public decimal Quantidade { get; set; }
        }

        private List<ProdutoAgregado> _produtosAgregados = new();

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

        partial void OnMensagemProcessamentoChanged(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            if (value.Contains("⏳")) // Processando (Amarelo/Laranja)
            {
                CorFundoMensagem = "#FFF3E0"; CorBordaMensagem = "#FFB74D"; CorTextoMensagem = "#E65100"; IconeMensagem = "TimerSand";
            }
            else if (value.Contains("✅")) // Sucesso (Verde)
            {
                CorFundoMensagem = "#E8F5E9"; CorBordaMensagem = "#81C784"; CorTextoMensagem = "#2E7D32"; IconeMensagem = "CheckCircleOutline";
            }
            else if (value.Contains("❌")) // Erro (Vermelho)
            {
                CorFundoMensagem = "#FFEBEE"; CorBordaMensagem = "#E57373"; CorTextoMensagem = "#D32F2F"; IconeMensagem = "CloseCircleOutline";
            }
            else if (value.Contains("⚠️")) // Alerta (Amarelo Claro)
            {
                CorFundoMensagem = "#FFF8E1"; CorBordaMensagem = "#FFE082"; CorTextoMensagem = "#F57C00"; IconeMensagem = "AlertOutline";
            }
            else // Neutro (Azul)
            {
                CorFundoMensagem = "#E3F2FD"; CorBordaMensagem = "#90CAF9"; CorTextoMensagem = "#1565C0"; IconeMensagem = "InformationOutline";
            }
        }

        private async Task CarregarDadosIniciais()
        {
            try
            {
                var veiculos = await _mediator.Send(new ListarVeiculosQuery());
                foreach (var v in veiculos) VeiculosDisponiveis.Add(v);

                var condutores = await _mediator.Send(new ListarCondutoresQuery());
                foreach (var c in condutores) CondutoresDisponiveis.Add(c);

                var empresa = await _dbContext.Empresas.Include(e => e.Configuracao).FirstOrDefaultAsync();
                if (empresa != null)
                {
                    _empresaAtualId = empresa.Id;
                    // Guarda os dados do emitente para o Carregamento Posterior
                    _empresaMunicipio = empresa.NomeMunicipio ?? string.Empty;
                    _empresaIbge = empresa.CodigoIbgeMunicipio.ToString();
                    _empresaUf = empresa.SiglaUf ?? string.Empty;
                    if (empresa.Configuracao != null)
                    {
                        _configuracaoAtual = empresa.Configuracao;

                        TipoEmitenteIndex = Math.Max(0, empresa.Configuracao.TipoEmitentePadrao - 1);
                        TipoEmissaoIndex = Math.Max(0, empresa.Configuracao.TipoEmissaoPadrao - 1);
                        ModalSelecionadoIndex = Math.Max(0, empresa.Configuracao.ModalidadePadrao - 1);
                        TipoTransportadorIndex = empresa.Configuracao.TipoTransportadorPadrao;

                        // Injeção de Padrões: Veículo e Motorista
                        if (empresa.Configuracao.VeiculoPadraoId.HasValue)
                            VeiculoSelecionado = VeiculosDisponiveis.FirstOrDefault(v => v.Id == empresa.Configuracao.VeiculoPadraoId.Value);

                        if (empresa.Configuracao.CondutorPadraoId.HasValue)
                            CondutorSelecionado = CondutoresDisponiveis.FirstOrDefault(c => c.Id == empresa.Configuracao.CondutorPadraoId.Value);

                        // Injeção de Padrões: Seguro
                        if (!string.IsNullOrEmpty(empresa.Configuracao.SeguroCnpjSeguradoraPadrao))
                        {
                            SeguradoraCnpj = empresa.Configuracao.SeguroCnpjSeguradoraPadrao;
                            SeguradoraNome = empresa.Configuracao.SeguroNomeSeguradoraPadrao ?? "";
                            NumeroApolice = empresa.Configuracao.SeguroApolicePadrao ?? "";
                            IsSeguroAberto = true; // Abre a aba automaticamente
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"[WIZARD - ERRO] Falha ao inicializar: {ex.Message}"); }
        }

        [RelayCommand]
        private void Avancar()
        {
            // Validação no Passo 1
            if (PassoAtual == 1)
            {
                if (IsCarregamentoPosterior && (string.IsNullOrWhiteSpace(IbgeCarregamentoManual) || IbgeCarregamentoManual == "0"))
                {
                    MensagemProcessamento = "⚠️ O cadastro da sua Empresa está sem o Código IBGE e Município. Edite nas Configurações primeiro.";
                    return;
                }

                if (!IsCarregamentoPosterior && !DocumentosFiscais.Any())
                {
                    MensagemProcessamento = "⚠️ Importe pelo menos um XML ou marque a opção de Carregamento Posterior.";
                    return;
                }
            }

            if (PassoAtual < 4) PassoAtual++;
        }
        [RelayCommand] private void Voltar() { if (PassoAtual > 1) PassoAtual--; }

        [RelayCommand]
        private void NovaEmissao()
        {
            DocumentosFiscais.Clear();
            QuantidadeNFe = 0; QuantidadeCTe = 0;
            ValorTotalCarga = 0; PesoTotalCarga = 0;
            IsAutorizado = false; IsProcessando = false;
            ManifestoAutorizadoId = null;
            XmlEnvio = ""; XmlRetorno = ""; MensagemProcessamento = "";
            NomeProdutoPredominante = ""; NcmProduto = "";
            _produtosAgregados.Clear();
            CeanProduto = ""; // Limpa produtos anteriores
            PassoAtual = 1;
            AtualizarTotaisEResumos();
        }

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

        [RelayCommand]
        private void RemoverDocumento(DocumentoMDFeDto documento)
        {
            if (documento != null && DocumentosFiscais.Contains(documento))
            {
                DocumentosFiscais.Remove(documento);

                // Remove os produtos atrelados à nota que foi excluída
                _produtosAgregados.RemoveAll(p => p.ChaveDocumento == documento.Chave);

                if (documento.Tipo == 55) QuantidadeNFe--;
                else if (documento.Tipo == 57) QuantidadeCTe--;

                AtualizarTotaisEResumos();
            }
        }

        private void ExtrairDadosNFe(XDocument doc)
        {
            var infNFe = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infNFe");
            if (infNFe == null) return;
            var chave = infNFe.Attribute("Id")?.Value.Replace("NFe", "");
            if (string.IsNullOrEmpty(chave) || DocumentosFiscais.Any(d => d.Chave == chave)) return;

            var ide = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "ide"); // Novo
            var vNf = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "vNF")?.Value;
            var pesoB = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "pesoB")?.Value;
            var emit = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "emit")?.Descendants().FirstOrDefault(x => x.Name.LocalName == "enderEmit");
            var dest = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dest"); // Alterado para pegar a tag pai
            var enderDest = dest?.Descendants().FirstOrDefault(x => x.Name.LocalName == "enderDest");

            var detNodes = doc.Descendants().Where(x => x.Name.LocalName == "det");
            foreach (var det in detNodes)
            {
                var p = det.Elements().FirstOrDefault(x => x.Name.LocalName == "prod");
                if (p != null)
                {
                    var nome = p.Elements().FirstOrDefault(x => x.Name.LocalName == "xProd")?.Value ?? "";
                    var ncm = p.Elements().FirstOrDefault(x => x.Name.LocalName == "NCM")?.Value ?? "";
                    var cean = p.Elements().FirstOrDefault(x => x.Name.LocalName == "cEAN")?.Value ?? "";
                    var qComStr = p.Elements().FirstOrDefault(x => x.Name.LocalName == "qCom")?.Value ?? "0";

                    decimal.TryParse(qComStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal qCom);

                    _produtosAgregados.Add(new ProdutoAgregado
                    {
                        ChaveDocumento = chave,
                        Nome = nome,
                        Ncm = ncm,
                        Cean = cean,
                        Quantidade = qCom
                    });
                }
            }

            DocumentosFiscais.Add(new DocumentoMDFeDto
            {
                Chave = chave,
                Tipo = 55, // Mod. 55
                Numero = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "nNF")?.Value ?? "", // Novo
                Serie = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "serie")?.Value ?? "", // Novo
                NomeDestinatario = dest?.Elements().FirstOrDefault(x => x.Name.LocalName == "xNome")?.Value ?? "Não Informado", // Novo
                Valor = ParseDecimal(vNf),
                Peso = ParseDecimal(pesoB),
                IbgeCarregamento = ParseLong(emit?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMun")?.Value),
                MunicipioCarregamento = emit?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMun")?.Value?.ToUpper() ?? "",
                UfCarregamento = emit?.Elements().FirstOrDefault(x => x.Name.LocalName == "UF")?.Value?.ToUpper() ?? "",
                IbgeDescarga = ParseLong(enderDest?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMun")?.Value),
                MunicipioDescarga = enderDest?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMun")?.Value?.ToUpper() ?? "",
                UfDescarga = enderDest?.Elements().FirstOrDefault(x => x.Name.LocalName == "UF")?.Value?.ToUpper() ?? ""
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
            var dest = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dest"); // Novo

            var infCarga = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infCarga");
            if (infCarga != null)
            {
                var proPred = infCarga.Elements().FirstOrDefault(x => x.Name.LocalName == "proPred")?.Value ?? "";
                if (!string.IsNullOrEmpty(proPred))
                {
                    _produtosAgregados.Add(new ProdutoAgregado
                    {
                        ChaveDocumento = chave,
                        Nome = proPred,
                        Ncm = "",
                        Cean = "",
                        Quantidade = ParseDecimal(qCarga) // No CTe, usamos o peso bruto/quantidade de carga
                    });
                }
            }

            DocumentosFiscais.Add(new DocumentoMDFeDto
            {
                Chave = chave,
                Tipo = 57, // Mod. 57
                Numero = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "nCT")?.Value ?? "", // Novo
                Serie = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "serie")?.Value ?? "", // Novo
                NomeDestinatario = dest?.Elements().FirstOrDefault(x => x.Name.LocalName == "xNome")?.Value ?? "Não Informado", // Novo
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

            // MAGIA DO RANQUEAMENTO (AGRUPA -> SOMA AS QTD -> PEGA O MAIOR)
            if (_produtosAgregados.Any())
            {
                var prodPredominante = _produtosAgregados
                    .GroupBy(p => new { p.Nome, p.Ncm, p.Cean })
                    .Select(g => new
                    {
                        g.Key.Nome,
                        g.Key.Ncm,
                        g.Key.Cean,
                        TotalQuantidade = g.Sum(x => x.Quantidade)
                    })
                    .OrderByDescending(x => x.TotalQuantidade)
                    .FirstOrDefault();

                if (prodPredominante != null)
                {
                    NomeProdutoPredominante = prodPredominante.Nome;
                    NcmProduto = prodPredominante.Ncm;
                    CeanProduto = prodPredominante.Cean == "SEM GTIN" ? "" : prodPredominante.Cean;
                    IsProdutoPredominanteAberto = true;
                }
            }

            OnPropertyChanged(nameof(ResumoCidadesCarregamento));
            OnPropertyChanged(nameof(ResumoCidadesDescarregamento));
        }

        // Este método mágico roda sempre que o IsCarregamentoPosterior muda (True/False)
        partial void OnIsCarregamentoPosteriorChanged(bool value)
        {
            if (value)
            {
                // Injeta automaticamente o município do emitente
                MunicipioCarregamentoManual = _empresaMunicipio;
                IbgeCarregamentoManual = _empresaIbge;
                MunicipioDescarregamentoManual = _empresaMunicipio;
                IbgeDescarregamentoManual = _empresaIbge;

                UfCarregamento = _empresaUf;
                UfDescarregamento = _empresaUf;

                // Limpa os XMLs bipados para não dar rejeição na SEFAZ
                DocumentosFiscais.Clear();
                QuantidadeNFe = 0;
                QuantidadeCTe = 0;
                ValorTotalCarga = 0;
                PesoTotalCarga = 0;
            }
        }

        [RelayCommand]
        private void CancelarEmissao()
        {
            // Limpa todos os dados da tela usando a estrutura já existente
            NovaEmissao();

            // Heurística 1: Visibilidade do Status do Sistema (Feedback imediato da ação)
            MensagemProcessamento = "⚠️ Emissão cancelada. Os dados não salvos foram descartados.";
        }

        [RelayCommand]
        private async Task Emitir()
        {
            if (VeiculoSelecionado == null || CondutorSelecionado == null)
            {
                MensagemProcessamento = "⚠️ Selecione Veículo e Condutor no Passo 3."; return;
            }

            if (IsCarregamentoPosterior && DocumentosFiscais.Any())
            {
                MensagemProcessamento = "⚠️ SEFAZ: MDF-e com Carregamento Posterior NÃO pode conter Notas. Limpe os XMLs."; return;
            }

            IsProcessando = true;
            MensagemProcessamento = "⏳ Transmitindo MDF-e para a SEFAZ. Aguarde...";
            await Task.Delay(50); // Garante que a UI exiba a mensagem e a ProgressBar antes de "travar" a thread de comunicação

            var command = new EmitirManifestoCommand(
                EmpresaId: _empresaAtualId,
                Documentos: DocumentosFiscais.ToList(),
                UfCarregamento: UfCarregamento, UfDescarregamento: UfDescarregamento,
                TipoEmitente: TipoEmitenteIndex + 1, TipoTransportador: TipoTransportadorIndex,
                Modal: ModalSelecionadoIndex + 1, TipoEmissao: TipoEmissaoIndex + 1,
                UfsPercurso: UfsPercurso, DataInicioViagem: DataInicioViagem,
                IsCanalVerde: IsCanalVerde, IsCarregamentoPosterior: IsCarregamentoPosterior,
                VeiculoTracao: VeiculoSelecionado, Condutor: CondutorSelecionado,
                Reboque1: Reboque1Selecionado, Reboque2: Reboque2Selecionado, Reboque3: Reboque3Selecionado,
                HasSeguro: IsSeguroAberto, SeguradoraCnpj: SeguradoraCnpj, SeguradoraNome: SeguradoraNome,
                NumeroApolice: NumeroApolice, NumeroAverbacao: NumeroAverbacao,
                HasProdutoPredominante: IsProdutoPredominanteAberto, TipoCarga: (TipoCargaIndex + 1).ToString("D2"),
                NomeProdutoPredominante: NomeProdutoPredominante, NcmProduto: NcmProduto, CeanProduto: CeanProduto,
                HasCiotValePedagio: IsCiotValePedagioAberto, Ciot: Ciot, CpfCnpjCiot: CpfCnpjCiot,
                CnpjFornecedorValePedagio: CnpjFornecedorValePedagio, CnpjPagadorValePedagio: CnpjPagadorValePedagio,
                IbgeCarregamentoManual: IbgeCarregamentoManual, MunicipioCarregamentoManual: MunicipioCarregamentoManual,
                IbgeDescarregamentoManual: IbgeDescarregamentoManual, MunicipioDescarregamentoManual: MunicipioDescarregamentoManual
            );

            try
            {
                // Task.Run JOGA todo o processamento de XML, Assinatura e Sefaz para Background
                var result = await Task.Run(() => _mediator.Send(command));

                MensagemProcessamento = result.Sucesso ? "✅ MDF-e Autorizado com Sucesso!" : $"❌ Rejeição SEFAZ: {result.Mensagem}";
                XmlRetorno = result.XmlRetorno;
                ManifestoAutorizadoId = result.ManifestoId;
                IsAutorizado = result.Sucesso;
            }
            catch (Exception ex)
            {
                MensagemProcessamento = $"❌ Erro interno na emissão: {ex.Message}";
            }
            finally
            {
                IsProcessando = false;
            }
        }

        [RelayCommand]
        private async Task Imprimir()
        {
            if (ManifestoAutorizadoId == null) return;

            MensagemProcessamento = "⏳ Gerando PDF de Impressão...";
            await Task.Delay(50);

            try
            {
                var result = await Task.Run(() => _mediator.Send(new GerarPdfManifestoCommand(ManifestoAutorizadoId.Value)));

                if (result.Sucesso)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(result.CaminhoPdf) { UseShellExecute = true });
                    MensagemProcessamento = "✅ PDF Gerado e aberto com sucesso!";
                }
                else
                {
                    MensagemProcessamento = $"❌ Falha ao gerar PDF: {result.Mensagem}";
                }
            }
            catch (Exception ex) { MensagemProcessamento = $"❌ Erro ao abrir PDF: {ex.Message}"; }
        }

        // --- CORREÇÃO DO BOTÃO "EDITAR REJEITADO" ---
        public void CarregarRascunhoDeRejeitado(ManifestoEletronico manifestoRejeitado)
        {
            NovaEmissao(); // Limpa a tela

            // 1. Carrega Rota básica diretamente do Banco (Sempre vai ter, mesmo sem XML)
            UfCarregamento = manifestoRejeitado.UfOrigem ?? "";
            UfDescarregamento = manifestoRejeitado.UfDestino ?? "";
            IsCarregamentoPosterior = manifestoRejeitado.IndicadorCarregamentoPosterior;

            // 2. Só tenta extrair os XMLs das notas se o sistema tiver chegado a assinar
            if (!string.IsNullOrEmpty(manifestoRejeitado.XmlAssinado))
            {
                try
                {
                    var doc = XDocument.Parse(manifestoRejeitado.XmlAssinado);
                    var infNFes = doc.Descendants().Where(x => x.Name.LocalName == "infNFe");
                    foreach (var nfe in infNFes)
                    {
                        DocumentosFiscais.Add(new DocumentoMDFeDto { Chave = nfe.Elements().FirstOrDefault(x => x.Name.LocalName == "chNFe")?.Value ?? "", Tipo = 55, MunicipioCarregamento = "RECUPERADO", MunicipioDescarga = "RECUPERADO" });
                        QuantidadeNFe++;
                    }
                    var placa = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "placa")?.Value;
                    if (placa != null) VeiculoSelecionado = VeiculosDisponiveis.FirstOrDefault(v => v.Placa == placa);
                }
                catch { }
            }

            AtualizarTotaisEResumos();
            PassoAtual = 1;
            MensagemProcessamento = "⚠️ Rascunho do MDF-e rejeitado carregado com sucesso!";
        }
    }
}