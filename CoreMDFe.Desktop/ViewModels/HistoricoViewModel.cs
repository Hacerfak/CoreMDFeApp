using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage; // Necessário para o explorador de ficheiros
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Manifestos;
using CoreMDFe.Core.Entities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CoreMDFe.Desktop.ViewModels
{
    // --- CLASSE DTO PARA A INTERFACE ---
    public class ManifestoListItem
    {
        public ManifestoEletronico Manifesto { get; }
        public string Placa { get; }
        public string Condutor { get; }

        // --- NOVA REGRA VISUAL: O botão só aparece se for Carregamento Posterior E se o MDF-e estiver Autorizado ---
        public bool PodeIncluirDFe => Manifesto.IndicadorCarregamentoPosterior && Manifesto.Status == StatusManifesto.Autorizado;

        public ManifestoListItem(ManifestoEletronico manifesto)
        {
            Manifesto = manifesto;
            Placa = ExtrairPlaca(manifesto.XmlAssinado);
            Condutor = ExtrairCondutor(manifesto.XmlAssinado);
        }

        private string ExtrairPlaca(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return "-";
            try { return XDocument.Parse(xml).Descendants().FirstOrDefault(x => x.Name.LocalName == "placa")?.Value ?? "-"; } catch { return "-"; }
        }

        private string ExtrairCondutor(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return "-";
            try { return XDocument.Parse(xml).Descendants().FirstOrDefault(x => x.Name.LocalName == "xNome" && x.Parent?.Name.LocalName == "condutor")?.Value ?? "-"; } catch { return "-"; }
        }
    }

    public partial class HistoricoViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly IServiceProvider _serviceProvider;
        [ObservableProperty] private ObservableCollection<ManifestoListItem> _manifestos = new();
        [ObservableProperty] private bool _estaCarregando;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(HasMensagem))] private string _mensagemSistema = string.Empty;

        public bool HasMensagem => !string.IsNullOrEmpty(MensagemSistema);

        // Propriedades dinâmicas para a UI
        [ObservableProperty] private string _corFundoMensagem = "Transparent";
        [ObservableProperty] private string _corBordaMensagem = "Transparent";
        [ObservableProperty] private string _corTextoMensagem = "Black";
        [ObservableProperty] private string _iconeMensagem = "InformationOutline";

        public bool IsListaVazia => Manifestos == null || Manifestos.Count == 0;

        // --- CONTROLES DOS DIÁLOGOS (MODAIS) ---
        [ObservableProperty] private ManifestoEletronico? _manifestoSelecionado;

        [ObservableProperty] private bool _isDialogCancelarAberto;
        [ObservableProperty] private string _justificativaCancelamento = string.Empty;

        [ObservableProperty] private bool _isDialogCondutorAberto;
        [ObservableProperty] private string _novoCondutorNome = string.Empty;
        [ObservableProperty] private string _novoCondutorCpf = string.Empty;

        // --- NOVO: GESTÃO DO DIÁLOGO DE INCLUSÃO DE DF-E ---
        [ObservableProperty] private bool _isDialogDFeAberto;
        [ObservableProperty] private ObservableCollection<DocumentoMDFeDto> _documentosInclusao = new();

        public HistoricoViewModel(IMediator mediator, IServiceProvider serviceProvider)
        {
            _mediator = mediator;
            _serviceProvider = serviceProvider;
            _ = CarregarHistorico();
        }

        partial void OnMensagemSistemaChanged(string value)
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

        [RelayCommand]
        public async Task CarregarHistorico(bool manterMensagem = false)
        {
            EstaCarregando = true;
            if (!manterMensagem) MensagemSistema = "⏳ Atualizando base e limpando arquivos antigos...";
            await Task.Delay(50); // Garante que a UI renderize o loader

            try
            {
                // Dispara a rotina de exclusão (Banco + Disco) silenciosamente
                await Task.Run(() => _mediator.Send(new LimparManifestosAntigosCommand(30)));

                // Fixa o período de busca eternamente para os últimos 30 dias
                var inicio = DateTime.Today.AddDays(-30);
                var fim = DateTime.Today.AddDays(1).AddTicks(-1); // Pega até a meia-noite de hoje

                var listaOrigem = await Task.Run(() => _mediator.Send(new ListarManifestosQuery(inicio, fim)));

                Manifestos = new ObservableCollection<ManifestoListItem>(listaOrigem.Select(m => new ManifestoListItem(m)));
                OnPropertyChanged(nameof(IsListaVazia));
                if (!manterMensagem) MensagemSistema = string.Empty;
            }
            catch (Exception ex) { MensagemSistema = $"❌ Erro ao carregar histórico: {ex.Message}"; }
            finally { EstaCarregando = false; }
        }

        [RelayCommand]
        private void FecharDialogos()
        {
            IsDialogCancelarAberto = false; IsDialogCondutorAberto = false; IsDialogDFeAberto = false; IsDialogExcluirAberto = false; ManifestoSelecionado = null;
        }

        // --- EXTRATOR SEGURO (Impede Erros de Binding) ---
        private ManifestoEletronico? ObterManifesto(object param)
        {
            if (param is ManifestoEletronico m) return m;
            if (param is ManifestoListItem item) return item.Manifesto;
            return null;
        }

        // --- ABERTURA DE DIÁLOGOS ---
        [RelayCommand] private void AbrirDialogCancelar(object param) { if (ObterManifesto(param) is { } m) { ManifestoSelecionado = m; JustificativaCancelamento = string.Empty; IsDialogCancelarAberto = true; } }
        [RelayCommand] private void AbrirDialogCondutor(object param) { if (ObterManifesto(param) is { } m) { ManifestoSelecionado = m; NovoCondutorNome = string.Empty; NovoCondutorCpf = string.Empty; IsDialogCondutorAberto = true; } }

        [RelayCommand]
        private void AbrirDialogDFe(object param)
        {
            if (ObterManifesto(param) is { } m)
            {
                ManifestoSelecionado = m;
                DocumentosInclusao.Clear(); // Limpa a lista antiga ao abrir o ecrã
                IsDialogDFeAberto = true;
            }
        }

        // --- LER XML PARA INCLUSÃO ---
        [RelayCommand]
        private async Task ProcurarXmlsParaInclusao()
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
                    try
                    {
                        var doc = XDocument.Load(file.Path.LocalPath);
                        var infNFe = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infNFe");
                        if (infNFe != null)
                        {
                            var chave = infNFe.Attribute("Id")?.Value.Replace("NFe", "") ?? "";
                            if (DocumentosInclusao.Any(d => d.Chave == chave)) continue;

                            var ide = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "ide");
                            var emit = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "enderEmit");
                            var destTag = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dest");
                            var enderDest = destTag?.Descendants().FirstOrDefault(x => x.Name.LocalName == "enderDest");

                            DocumentosInclusao.Add(new DocumentoMDFeDto
                            {
                                Chave = chave,
                                Tipo = 55,
                                Numero = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "nNF")?.Value ?? "",
                                Serie = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "serie")?.Value ?? "",
                                NomeDestinatario = destTag?.Elements().FirstOrDefault(x => x.Name.LocalName == "xNome")?.Value ?? "Não Informado",
                                IbgeCarregamento = long.TryParse(emit?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMun")?.Value, out var mE) ? mE : 0,
                                MunicipioCarregamento = emit?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMun")?.Value?.ToUpper() ?? "",
                                IbgeDescarga = long.TryParse(enderDest?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMun")?.Value, out var mD) ? mD : 0,
                                MunicipioDescarga = enderDest?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMun")?.Value?.ToUpper() ?? ""
                            });
                        }
                        else
                        {
                            var infCte = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infCte");
                            if (infCte != null)
                            {
                                var chave = infCte.Attribute("Id")?.Value.Replace("CTe", "") ?? "";
                                if (DocumentosInclusao.Any(d => d.Chave == chave)) continue;

                                var ide = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "ide");
                                var destTag = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "dest");

                                DocumentosInclusao.Add(new DocumentoMDFeDto
                                {
                                    Chave = chave,
                                    Tipo = 57,
                                    Numero = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "nCT")?.Value ?? "",
                                    Serie = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "serie")?.Value ?? "",
                                    NomeDestinatario = destTag?.Elements().FirstOrDefault(x => x.Name.LocalName == "xNome")?.Value ?? "Não Informado",
                                    IbgeCarregamento = long.TryParse(ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMunEnv")?.Value, out var cE) ? cE : 0,
                                    MunicipioCarregamento = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMunEnv")?.Value?.ToUpper() ?? "",
                                    IbgeDescarga = long.TryParse(ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMunFim")?.Value, out var cF) ? cF : 0,
                                    MunicipioDescarga = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMunFim")?.Value?.ToUpper() ?? ""
                                });
                            }
                        }
                    }
                    catch (Exception ex) { MensagemSistema = $"Erro ao ler XML: {ex.Message}"; }
                }
            }
        }

        // --- REMOVER E LIMPAR DOCUMENTOS DA LISTA ---
        [RelayCommand]
        private void RemoverDocumentoInclusao(DocumentoMDFeDto documento)
        {
            if (documento != null && DocumentosInclusao.Contains(documento))
            {
                DocumentosInclusao.Remove(documento);
            }
        }

        [RelayCommand]
        private void LimparDocumentosInclusao()
        {
            DocumentosInclusao.Clear();
        }

        // --- ENVIAR AS NOTAS PARA A SEFAZ ---
        [RelayCommand]
        private async Task ConfirmarInclusaoDFe()
        {
            try
            {
                if (ManifestoSelecionado == null) return;
                if (!DocumentosInclusao.Any())
                {
                    MensagemSistema = "⚠️ Adicione pelo menos um XML para enviar à SEFAZ."; return;
                }

                var manifestoId = ManifestoSelecionado.Id;
                var ibgeCarrega = DocumentosInclusao.First().IbgeCarregamento.ToString();
                var munCarrega = DocumentosInclusao.First().MunicipioCarregamento;

                FecharDialogos();
                EstaCarregando = true;
                MensagemSistema = "⏳ Enviando evento de inclusão em lote à SEFAZ. Aguarde...";
                await Task.Delay(50); // Atualiza a UI

                var listaDocsParaEnvio = DocumentosInclusao.Select(d => new DtoInclusaoDFeItem
                {
                    IbgeDescarga = d.IbgeDescarga.ToString(),
                    MunDescarga = d.MunicipioDescarga,
                    ChaveDFe = d.Chave,
                    TipoDFe = d.Tipo
                }).ToList();

                // Dispara comunicação Sefaz em Background Thread
                var result = await Task.Run(() => _mediator.Send(new IncluirDFeManifestoCommand(manifestoId, ibgeCarrega, munCarrega, listaDocsParaEnvio)));

                if (result.Sucesso)
                {
                    MensagemSistema = $"✅ Todos os {DocumentosInclusao.Count} documento(s) foram incluídos com sucesso!";
                    DocumentosInclusao.Clear();
                }
                else { MensagemSistema = $"❌ Falha na Inclusão: {result.Mensagem}"; }
            }
            catch (Exception ex) { MensagemSistema = $"❌ Erro interno: {ex.Message}"; }
            finally { EstaCarregando = false; await CarregarHistorico(manterMensagem: true); }
        }

        [RelayCommand]
        private void EditarRejeitado(object param)
        {
            if (ObterManifesto(param) is not { } m) return;
            try
            {
                var emissaoVm = _serviceProvider.GetRequiredService<EmissaoViewModel>();
                emissaoVm.CarregarRascunhoDeRejeitado(m);
                var dashboardVm = _serviceProvider.GetRequiredService<DashboardViewModel>();
                dashboardVm.ConteudoWorkspace = emissaoVm;
                dashboardVm.MenuAtivo = "Emissao";
            }
            catch (Exception ex) { MensagemSistema = $"❌ Erro ao abrir edição: {ex.Message}"; }
        }

        [RelayCommand]
        private async Task Reenviar(object param)
        {
            if (ObterManifesto(param) is { } m)
            {
                EstaCarregando = true; MensagemSistema = "⏳ Reenviando MDF-e para a SEFAZ..."; await Task.Delay(50);
                var r = await Task.Run(() => _mediator.Send(new ReenviarManifestoCommand(m.Id)));
                MensagemSistema = r.Sucesso ? $"✅ {r.Mensagem}" : $"❌ Erro ao reenviar: {r.Mensagem}";
                EstaCarregando = false; await CarregarHistorico(true);
            }
        }

        [RelayCommand]
        private async Task Encerrar(object param)
        {
            if (ObterManifesto(param) is { } m)
            {
                EstaCarregando = true; MensagemSistema = "⏳ Processando encerramento na SEFAZ..."; await Task.Delay(50);
                var r = await Task.Run(() => _mediator.Send(new EncerrarManifestoCommand(m.Id)));
                MensagemSistema = r.Sucesso ? $"✅ {r.Mensagem}" : $"❌ Erro ao encerrar: {r.Mensagem}";
                EstaCarregando = false; await CarregarHistorico(true);
            }
        }

        [RelayCommand]
        private async Task Imprimir(object param)
        {
            if (ObterManifesto(param) is { } m)
            {
                EstaCarregando = true; MensagemSistema = "⏳ Gerando PDF do DAMDFE..."; await Task.Delay(50);
                var r = await Task.Run(() => _mediator.Send(new GerarPdfManifestoCommand(m.Id)));
                if (r.Sucesso) { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(r.CaminhoPdf) { UseShellExecute = true }); MensagemSistema = "✅ PDF Gerado!"; }
                else MensagemSistema = $"❌ Erro ao gerar PDF: {r.Mensagem}";
                EstaCarregando = false;
            }
        }

        [RelayCommand] private async Task CopiarChave(string chave) { if (!string.IsNullOrEmpty(chave) && Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d && d.MainWindow != null) { var c = d.MainWindow.Clipboard; if (c != null) { await c.SetTextAsync(chave); MensagemSistema = "✅ Chave copiada!"; _ = Task.Delay(3000).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { if (MensagemSistema.Contains("copiada")) MensagemSistema = ""; })); } } }

        [RelayCommand]
        private async Task ConfirmarCancelamento()
        {
            if (ManifestoSelecionado != null)
            {
                var id = ManifestoSelecionado.Id; var just = JustificativaCancelamento;
                FecharDialogos();
                EstaCarregando = true; MensagemSistema = "⏳ Cancelando MDF-e na SEFAZ..."; await Task.Delay(50);
                var r = await Task.Run(() => _mediator.Send(new CancelarManifestoCommand(id, just)));
                MensagemSistema = r.Sucesso ? $"✅ {r.Mensagem}" : $"❌ Erro ao cancelar: {r.Mensagem}";
                EstaCarregando = false; await CarregarHistorico(true);
            }
        }

        [RelayCommand]
        private async Task ConfirmarInclusaoCondutor()
        {
            if (ManifestoSelecionado != null)
            {
                var id = ManifestoSelecionado.Id; var n = NovoCondutorNome; var c = NovoCondutorCpf;
                FecharDialogos();
                EstaCarregando = true; MensagemSistema = "⏳ Registrando novo condutor na SEFAZ..."; await Task.Delay(50);
                var r = await Task.Run(() => _mediator.Send(new IncluirCondutorManifestoCommand(id, n, c)));
                MensagemSistema = r.Sucesso ? $"✅ {r.Mensagem}" : $"❌ Erro ao adicionar condutor: {r.Mensagem}";
                EstaCarregando = false; await CarregarHistorico(true);
            }
        }

        // --- CONTROLE DO DIÁLOGO DE EXCLUSÃO ---
        [ObservableProperty] private bool _isDialogExcluirAberto;

        [RelayCommand]
        private void AbrirDialogExcluir(object param)
        {
            if (ObterManifesto(param) is { } m)
            {
                ManifestoSelecionado = m;
                IsDialogExcluirAberto = true;
            }
        }

        [RelayCommand]
        private async Task ConfirmarExclusao()
        {
            if (ManifestoSelecionado == null) return;
            var idParaExcluir = ManifestoSelecionado.Id;
            FecharDialogos();

            EstaCarregando = true; MensagemSistema = "⏳ Excluindo rascunho do banco de dados..."; await Task.Delay(50);
            try
            {
                var sucesso = await Task.Run(() => _mediator.Send(new ExcluirManifestoCommand(idParaExcluir)));
                MensagemSistema = sucesso ? "✅ MDF-e removido com sucesso." : "❌ Não foi possível excluir o manifesto.";
                await CarregarHistorico(manterMensagem: true);
            }
            catch (Exception ex) { MensagemSistema = $"❌ Erro ao excluir: {ex.Message}"; EstaCarregando = false; }
        }

        [RelayCommand]
        private async Task Excluir(ManifestoEletronico m)
        {
            if (m == null) return;

            // Nota: Em um sistema real, você poderia abrir um diálogo de confirmação aqui
            try
            {
                EstaCarregando = true;
                MensagemSistema = "A excluir MDF-e do banco de dados...";

                var sucesso = await _mediator.Send(new ExcluirManifestoCommand(m.Id));

                if (sucesso)
                {
                    MensagemSistema = "✅ MDF-e removido com sucesso.";
                    await CarregarHistorico(manterMensagem: true);
                }
                else
                {
                    MensagemSistema = "❌ Não foi possível excluir o manifesto.";
                }
            }
            catch (Exception ex)
            {
                MensagemSistema = $"❌ Erro ao excluir: {ex.Message}";
            }
            finally
            {
                EstaCarregando = false;
            }
        }
    }
}