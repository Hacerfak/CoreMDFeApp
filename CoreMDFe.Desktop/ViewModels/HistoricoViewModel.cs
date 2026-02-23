using Avalonia.Controls.ApplicationLifetimes;
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
    // --- CLASSE DTO PARA A INTERFACE (Lê a placa e condutor do XML) ---
    public class ManifestoListItem
    {
        public ManifestoEletronico Manifesto { get; }
        public string Placa { get; }
        public string Condutor { get; }

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

        // CORREÇÃO DO FILTRO DE DATA: Usando DateTime? em vez de DateTimeOffset
        [ObservableProperty] private DateTime? _dataInicio = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime? _dataFim = DateTime.Today;

        // AGORA A LISTA É DO NOSSO ITEM VISUAL
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

        [ObservableProperty] private bool _isDialogDFeAberto;
        [ObservableProperty] private string _novaChaveDFe = string.Empty;
        [ObservableProperty] private string _novoIbgeCarrega = string.Empty;
        [ObservableProperty] private string _novoMunCarrega = string.Empty;
        [ObservableProperty] private string _novoIbgeDescarga = string.Empty;
        [ObservableProperty] private string _novoMunDescarga = string.Empty;

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

                // Mapeia para o item visual que contém Placa e Condutor
                var listaMapeada = listaOrigem.Select(m => new ManifestoListItem(m)).ToList();

                Manifestos = new ObservableCollection<ManifestoListItem>(listaMapeada);
                OnPropertyChanged(nameof(IsListaVazia));
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao carregar histórico: {ex.Message}";
            }
            finally { EstaCarregando = false; }
        }

        // --- NOVO: COPIAR CHAVE DE ACESSO ---
        [RelayCommand]
        private async Task CopiarChave(string chave)
        {
            if (string.IsNullOrEmpty(chave)) return;

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var clipboard = desktop.MainWindow.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(chave);
                    MensagemErro = "✅ Chave de Acesso copiada com sucesso!";

                    // Limpa a mensagem de sucesso automaticamente após 3 segundos
                    _ = Task.Delay(3000).ContinueWith(_ =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (MensagemErro.Contains("copiada")) MensagemErro = string.Empty;
                        });
                    });
                }
            }
        }

        // --- ABERTURA DOS DIÁLOGOS ---
        [RelayCommand]
        private void FecharDialogos()
        {
            IsDialogCancelarAberto = false;
            IsDialogCondutorAberto = false;
            IsDialogDFeAberto = false;
            ManifestoSelecionado = null;
        }

        [RelayCommand]
        private void AbrirDialogCancelar(ManifestoEletronico m)
        {
            ManifestoSelecionado = m; JustificativaCancelamento = string.Empty; IsDialogCancelarAberto = true;
        }

        [RelayCommand]
        private void AbrirDialogCondutor(ManifestoEletronico m)
        {
            ManifestoSelecionado = m; NovoCondutorNome = string.Empty; NovoCondutorCpf = string.Empty; IsDialogCondutorAberto = true;
        }

        [RelayCommand]
        private void AbrirDialogDFe(ManifestoEletronico m)
        {
            ManifestoSelecionado = m; NovaChaveDFe = string.Empty; NovoIbgeCarrega = string.Empty; NovoMunCarrega = string.Empty; NovoIbgeDescarga = string.Empty; NovoMunDescarga = string.Empty; IsDialogDFeAberto = true;
        }

        [RelayCommand]
        private void EditarRejeitado(ManifestoEletronico m)
        {
            var emissaoVm = _serviceProvider.GetRequiredService<EmissaoViewModel>();
            emissaoVm.CarregarRascunhoDeRejeitado(m);
            var dashboardVm = _serviceProvider.GetRequiredService<DashboardViewModel>();
            dashboardVm.ConteudoWorkspace = emissaoVm;
            dashboardVm.MenuAtivo = "Emissao";
        }

        [RelayCommand]
        private async Task Reenviar(ManifestoEletronico m)
        {
            try
            {
                EstaCarregando = true; MensagemErro = "Reenviando manifesto, aguarde...";
                var result = await _mediator.Send(new ReenviarManifestoCommand(m.Id));
                MensagemErro = result.Sucesso ? "✅ " + result.Mensagem : "❌ " + result.Mensagem;
            }
            catch (Exception ex) { MensagemErro = $"❌ Erro interno: {ex.Message}"; }
            finally { EstaCarregando = false; await CarregarHistorico(manterMensagem: true); }
        }

        [RelayCommand]
        private async Task Imprimir(ManifestoEletronico m)
        {
            try
            {
                EstaCarregando = true; MensagemErro = "Gerando DAMDFE...";
                var result = await _mediator.Send(new GerarPdfManifestoCommand(m.Id));
                if (result.Sucesso) { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(result.CaminhoPdf) { UseShellExecute = true }); MensagemErro = string.Empty; }
                else { MensagemErro = $"❌ {result.Mensagem}"; }
            }
            catch (Exception ex) { MensagemErro = $"❌ Erro interno: {ex.Message}"; }
            finally { EstaCarregando = false; }
        }

        [RelayCommand]
        private async Task Encerrar(ManifestoEletronico m)
        {
            try
            {
                EstaCarregando = true; MensagemErro = "Enviando evento de encerramento...";
                var result = await _mediator.Send(new EncerrarManifestoCommand(m.Id));
                MensagemErro = result.Sucesso ? "✅ " + result.Mensagem : "❌ " + result.Mensagem;
            }
            catch (Exception ex) { MensagemErro = $"❌ Erro interno: {ex.Message}"; }
            finally { EstaCarregando = false; await CarregarHistorico(manterMensagem: true); }
        }

        [RelayCommand]
        private async Task ConfirmarCancelamento()
        {
            try
            {
                if (ManifestoSelecionado == null) return;
                var manifestoId = ManifestoSelecionado.Id;
                var justificativa = JustificativaCancelamento;

                FecharDialogos();
                EstaCarregando = true; MensagemErro = "Enviando cancelamento...";

                var result = await _mediator.Send(new CancelarManifestoCommand(manifestoId, justificativa));
                MensagemErro = result.Sucesso ? "✅ " + result.Mensagem : "❌ " + result.Mensagem;
            }
            catch (Exception ex) { MensagemErro = $"❌ Erro interno: {ex.Message}"; }
            finally { EstaCarregando = false; await CarregarHistorico(manterMensagem: true); }
        }

        [RelayCommand]
        private async Task ConfirmarInclusaoCondutor()
        {
            try
            {
                if (ManifestoSelecionado == null) return;
                var manifestoId = ManifestoSelecionado.Id;
                var nome = NovoCondutorNome; var cpf = NovoCondutorCpf;

                FecharDialogos();
                EstaCarregando = true; MensagemErro = "Incluindo condutor, aguarde...";

                var result = await _mediator.Send(new IncluirCondutorManifestoCommand(manifestoId, nome, cpf));
                MensagemErro = result.Sucesso ? "✅ " + result.Mensagem : "❌ " + result.Mensagem;
            }
            catch (Exception ex) { MensagemErro = $"❌ Erro interno: {ex.Message}"; }
            finally { EstaCarregando = false; await CarregarHistorico(manterMensagem: true); }
        }

        [RelayCommand]
        private async Task ConfirmarInclusaoDFe()
        {
            try
            {
                if (ManifestoSelecionado == null) return;
                var manifestoId = ManifestoSelecionado.Id;
                var chave = NovaChaveDFe; var ibgeCarrega = NovoIbgeCarrega; var munCarrega = NovoMunCarrega;
                var ibgeDescarga = NovoIbgeDescarga; var munDescarga = NovoMunDescarga;

                FecharDialogos();
                EstaCarregando = true; MensagemErro = "Incluindo DF-e...";

                var result = await _mediator.Send(new IncluirDFeManifestoCommand(manifestoId, ibgeCarrega, munCarrega, ibgeDescarga, munDescarga, chave));
                MensagemErro = result.Sucesso ? "✅ " + result.Mensagem : "❌ " + result.Mensagem;
            }
            catch (Exception ex) { MensagemErro = $"❌ Erro interno: {ex.Message}"; }
            finally { EstaCarregando = false; await CarregarHistorico(manterMensagem: true); }
        }
    }
}