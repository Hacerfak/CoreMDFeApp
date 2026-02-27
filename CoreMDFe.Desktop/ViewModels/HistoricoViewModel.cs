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

        [ObservableProperty] private DateTime? _dataInicio = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime? _dataFim = DateTime.Today;
        [ObservableProperty] private ObservableCollection<ManifestoListItem> _manifestos = new();
        [ObservableProperty] private bool _estaCarregando;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(HasErro))] private string _mensagemErro = string.Empty;

        public bool HasErro => !string.IsNullOrEmpty(MensagemErro);
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

        [RelayCommand]
        public async Task CarregarHistorico(bool manterMensagem = false)
        {
            EstaCarregando = true;
            if (!manterMensagem) MensagemErro = string.Empty;

            try
            {
                var inicio = DataInicio ?? DateTime.Today.AddDays(-7);
                var fim = DataFim ?? DateTime.Today;
                var listaOrigem = await _mediator.Send(new ListarManifestosQuery(inicio, fim));
                Manifestos = new ObservableCollection<ManifestoListItem>(listaOrigem.Select(m => new ManifestoListItem(m)));
                OnPropertyChanged(nameof(IsListaVazia));
            }
            catch (Exception ex) { MensagemErro = $"Erro ao carregar histórico: {ex.Message}"; }
            finally { EstaCarregando = false; }
        }

        [RelayCommand]
        private void FecharDialogos()
        {
            IsDialogCancelarAberto = false; IsDialogCondutorAberto = false; IsDialogDFeAberto = false; ManifestoSelecionado = null;
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

        // --- MÁGICA: LER XML PARA INCLUSÃO ---
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

                            var emit = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "enderEmit");
                            var dest = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "enderDest");

                            DocumentosInclusao.Add(new DocumentoMDFeDto
                            {
                                Chave = chave,
                                Tipo = 55,
                                IbgeCarregamento = long.TryParse(emit?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMun")?.Value, out var mE) ? mE : 0,
                                MunicipioCarregamento = emit?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMun")?.Value?.ToUpper() ?? "",
                                IbgeDescarga = long.TryParse(dest?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMun")?.Value, out var mD) ? mD : 0,
                                MunicipioDescarga = dest?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMun")?.Value?.ToUpper() ?? ""
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
                                DocumentosInclusao.Add(new DocumentoMDFeDto
                                {
                                    Chave = chave,
                                    Tipo = 57,
                                    IbgeCarregamento = long.TryParse(ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMunEnv")?.Value, out var cE) ? cE : 0,
                                    MunicipioCarregamento = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMunEnv")?.Value?.ToUpper() ?? "",
                                    IbgeDescarga = long.TryParse(ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "cMunFim")?.Value, out var cF) ? cF : 0,
                                    MunicipioDescarga = ide?.Elements().FirstOrDefault(x => x.Name.LocalName == "xMunFim")?.Value?.ToUpper() ?? ""
                                });
                            }
                        }
                    }
                    catch (Exception ex) { MensagemErro = $"Erro ao ler XML: {ex.Message}"; }
                }
            }
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
                    MensagemErro = "⚠️ Adicione pelo menos um XML para enviar à SEFAZ.";
                    return;
                }

                var manifestoId = ManifestoSelecionado.Id;
                FecharDialogos();
                EstaCarregando = true; MensagemErro = "Enviando evento(s) de inclusão à SEFAZ...";

                int sucesso = 0; int falha = 0; string ultimaFalha = "";

                // A SEFAZ exige que a inclusão seja feita nota a nota (1 evento por chave). 
                // Então o sistema faz um laço automático de eventos para facilitar a vida do utilizador!
                foreach (var doc in DocumentosInclusao)
                {
                    var result = await _mediator.Send(new IncluirDFeManifestoCommand(
                        manifestoId, doc.IbgeCarregamento.ToString(), doc.MunicipioCarregamento,
                        doc.IbgeDescarga.ToString(), doc.MunicipioDescarga, doc.Chave));

                    if (result.Sucesso) sucesso++;
                    else { falha++; ultimaFalha = result.Mensagem; }
                }

                MensagemErro = falha == 0
                    ? $"✅ {sucesso} documento(s) incluído(s) com sucesso!"
                    : $"⚠️ Finalizado com {sucesso} acertos e {falha} falhas. Último erro: {ultimaFalha}";
            }
            catch (Exception ex) { MensagemErro = $"❌ Erro interno: {ex.Message}"; }
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
            catch (Exception ex) { MensagemErro = $"❌ Erro ao abrir edição: {ex.Message}"; }
        }

        [RelayCommand] private async Task Reenviar(object param) { if (ObterManifesto(param) is { } m) { EstaCarregando = true; MensagemErro = "Reenviando..."; var r = await _mediator.Send(new ReenviarManifestoCommand(m.Id)); MensagemErro = r.Sucesso ? "✅ " + r.Mensagem : "❌ " + r.Mensagem; EstaCarregando = false; await CarregarHistorico(true); } }
        [RelayCommand] private async Task Imprimir(object param) { if (ObterManifesto(param) is { } m) { EstaCarregando = true; MensagemErro = "Gerando PDF..."; var r = await _mediator.Send(new GerarPdfManifestoCommand(m.Id)); if (r.Sucesso) { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(r.CaminhoPdf) { UseShellExecute = true }); MensagemErro = ""; } else MensagemErro = "❌ " + r.Mensagem; EstaCarregando = false; } }
        [RelayCommand] private async Task Encerrar(object param) { if (ObterManifesto(param) is { } m) { EstaCarregando = true; MensagemErro = "Encerrando..."; var r = await _mediator.Send(new EncerrarManifestoCommand(m.Id)); MensagemErro = r.Sucesso ? "✅ " + r.Mensagem : "❌ " + r.Mensagem; EstaCarregando = false; await CarregarHistorico(true); } }
        [RelayCommand] private async Task CopiarChave(string chave) { if (!string.IsNullOrEmpty(chave) && Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d && d.MainWindow != null) { var c = d.MainWindow.Clipboard; if (c != null) { await c.SetTextAsync(chave); MensagemErro = "✅ Chave copiada!"; _ = Task.Delay(3000).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { if (MensagemErro.Contains("copiada")) MensagemErro = ""; })); } } }
        [RelayCommand] private async Task ConfirmarCancelamento() { if (ManifestoSelecionado != null) { var id = ManifestoSelecionado.Id; var just = JustificativaCancelamento; FecharDialogos(); EstaCarregando = true; MensagemErro = "Cancelando..."; var r = await _mediator.Send(new CancelarManifestoCommand(id, just)); MensagemErro = r.Sucesso ? "✅ " + r.Mensagem : "❌ " + r.Mensagem; EstaCarregando = false; await CarregarHistorico(true); } }
        [RelayCommand] private async Task ConfirmarInclusaoCondutor() { if (ManifestoSelecionado != null) { var id = ManifestoSelecionado.Id; var n = NovoCondutorNome; var c = NovoCondutorCpf; FecharDialogos(); EstaCarregando = true; MensagemErro = "Incluindo condutor..."; var r = await _mediator.Send(new IncluirCondutorManifestoCommand(id, n, c)); MensagemErro = r.Sucesso ? "✅ " + r.Mensagem : "❌ " + r.Mensagem; EstaCarregando = false; await CarregarHistorico(true); } }

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
            try
            {
                if (ManifestoSelecionado == null) return;
                var idParaExcluir = ManifestoSelecionado.Id;

                FecharDialogos(); // Fecha o modal imediatamente
                IsDialogExcluirAberto = false;

                EstaCarregando = true;
                MensagemErro = "A excluir MDF-e do banco de dados...";

                var sucesso = await _mediator.Send(new ExcluirManifestoCommand(idParaExcluir));

                if (sucesso)
                {
                    MensagemErro = "✅ MDF-e removido com sucesso.";
                    await CarregarHistorico(manterMensagem: true);
                }
                else
                {
                    MensagemErro = "❌ Não foi possível excluir o manifesto.";
                }
            }
            catch (Exception ex)
            {
                MensagemErro = $"❌ Erro ao excluir: {ex.Message}";
            }
            finally
            {
                EstaCarregando = false;
            }
        }

        [RelayCommand]
        private async Task Excluir(ManifestoEletronico m)
        {
            if (m == null) return;

            // Nota: Em um sistema real, você poderia abrir um diálogo de confirmação aqui
            try
            {
                EstaCarregando = true;
                MensagemErro = "A excluir MDF-e do banco de dados...";

                var sucesso = await _mediator.Send(new ExcluirManifestoCommand(m.Id));

                if (sucesso)
                {
                    MensagemErro = "✅ MDF-e removido com sucesso.";
                    await CarregarHistorico(manterMensagem: true);
                }
                else
                {
                    MensagemErro = "❌ Não foi possível excluir o manifesto.";
                }
            }
            catch (Exception ex)
            {
                MensagemErro = $"❌ Erro ao excluir: {ex.Message}";
            }
            finally
            {
                EstaCarregando = false;
            }
        }
    }
}