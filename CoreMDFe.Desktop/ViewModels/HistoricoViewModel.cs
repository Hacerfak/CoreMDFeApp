using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Manifestos;
using CoreMDFe.Core.Entities;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class HistoricoViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        [ObservableProperty] private ObservableCollection<ManifestoEletronico> _manifestos = new();
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

        public HistoricoViewModel(IMediator mediator)
        {
            _mediator = mediator;
            _ = CarregarHistorico();
        }

        [RelayCommand]
        public async Task CarregarHistorico(bool manterMensagem = false) // <-- CORREÇÃO: Parâmetro para não apagar o aviso
        {
            Console.WriteLine("[UI - HISTÓRICO] Carregando manifestos do banco de dados...");
            EstaCarregando = true;

            if (!manterMensagem) MensagemErro = string.Empty;

            try
            {
                var lista = await _mediator.Send(new ListarManifestosQuery());
                Manifestos = new ObservableCollection<ManifestoEletronico>(lista);
                OnPropertyChanged(nameof(IsListaVazia));
                Console.WriteLine($"[UI - HISTÓRICO] Foram carregados {lista.Count} registros.");
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao carregar do banco: {ex.Message}";
                Console.WriteLine($"[UI - HISTÓRICO - ERRO] {ex}");
            }
            finally { EstaCarregando = false; }
        }

        // --- ABERTURA DOS DIÁLOGOS ---
        [RelayCommand]
        private void FecharDialogos()
        {
            Console.WriteLine("[UI - MODAL] Fechando modais.");
            IsDialogCancelarAberto = false;
            IsDialogCondutorAberto = false;
            IsDialogDFeAberto = false;
            ManifestoSelecionado = null;
        }

        [RelayCommand]
        private void AbrirDialogCancelar(ManifestoEletronico m)
        {
            ManifestoSelecionado = m; JustificativaCancelamento = string.Empty; IsDialogCancelarAberto = true;
            Console.WriteLine($"[UI - MODAL] Abriu modal de Cancelamento para MDF-e {m.Numero}");
        }

        [RelayCommand]
        private void AbrirDialogCondutor(ManifestoEletronico m)
        {
            ManifestoSelecionado = m; NovoCondutorNome = string.Empty; NovoCondutorCpf = string.Empty; IsDialogCondutorAberto = true;
            Console.WriteLine($"[UI - MODAL] Abriu modal de Condutor para MDF-e {m.Numero}");
        }

        [RelayCommand]
        private void AbrirDialogDFe(ManifestoEletronico m)
        {
            ManifestoSelecionado = m; NovaChaveDFe = string.Empty; NovoIbgeCarrega = string.Empty; NovoMunCarrega = string.Empty; NovoIbgeDescarga = string.Empty; NovoMunDescarga = string.Empty; IsDialogDFeAberto = true;
            Console.WriteLine($"[UI - MODAL] Abriu modal de DF-e para MDF-e {m.Numero}");
        }

        [RelayCommand]
        private async Task Reenviar(ManifestoEletronico m)
        {
            try
            {
                Console.WriteLine($"[AÇÃO] Reenviando MDF-e {m.Numero} para a SEFAZ...");
                EstaCarregando = true;
                MensagemErro = "Reenviando manifesto, aguarde...";

                var result = await _mediator.Send(new ReenviarManifestoCommand(m.Id));

                MensagemErro = result.Sucesso ? "✅ " + result.Mensagem : "❌ " + result.Mensagem;
                Console.WriteLine($"[AÇÃO] Resultado Reenvio: {result.Mensagem}");
            }
            catch (Exception ex)
            {
                MensagemErro = $"❌ Erro interno: {ex.Message}";
                Console.WriteLine($"[AÇÃO - ERRO] {ex}");
            }
            finally
            {
                EstaCarregando = false;
                await CarregarHistorico(manterMensagem: true);
            }
        }

        [RelayCommand]
        private async Task Imprimir(ManifestoEletronico m)
        {
            try
            {
                Console.WriteLine($"[AÇÃO] Gerando PDF para MDF-e {m.Numero}...");
                EstaCarregando = true; MensagemErro = "Gerando DAMDFE...";

                var result = await _mediator.Send(new GerarPdfManifestoCommand(m.Id));

                if (result.Sucesso)
                {
                    // Abre o PDF nativamente no visualizador do Linux ou Windows
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(result.CaminhoPdf) { UseShellExecute = true });
                    MensagemErro = string.Empty;
                }
                else
                {
                    MensagemErro = $"❌ {result.Mensagem}";
                }
            }
            catch (Exception ex)
            {
                MensagemErro = $"❌ Erro interno: {ex.Message}";
                Console.WriteLine($"[AÇÃO - ERRO] {ex}");
            }
            finally { EstaCarregando = false; }
        }

        // --- EXECUÇÃO DOS EVENTOS ---

        [RelayCommand]
        private async Task Encerrar(ManifestoEletronico m)
        {
            try
            {
                Console.WriteLine($"[AÇÃO] Solicitando Encerramento do MDF-e {m.Numero}...");
                EstaCarregando = true; MensagemErro = "Enviando evento de encerramento...";

                var result = await _mediator.Send(new EncerrarManifestoCommand(m.Id));
                MensagemErro = result.Sucesso ? "✅ " + result.Mensagem : "❌ " + result.Mensagem;
                Console.WriteLine($"[AÇÃO] Resultado Encerramento: {result.Mensagem}");
            }
            catch (Exception ex) { MensagemErro = $"❌ Erro interno: {ex.Message}"; Console.WriteLine($"[AÇÃO - ERRO] {ex}"); }
            finally { EstaCarregando = false; await CarregarHistorico(manterMensagem: true); }
        }

        [RelayCommand]
        private async Task ConfirmarCancelamento()
        {
            try
            {
                if (ManifestoSelecionado == null) return;

                // 1. SALVA OS DADOS ANTES DE DESTRUIR A REFERÊNCIA
                var manifestoId = ManifestoSelecionado.Id;
                var numero = ManifestoSelecionado.Numero;
                var justificativa = JustificativaCancelamento;

                Console.WriteLine($"[AÇÃO] Solicitando Cancelamento do MDF-e {numero}...");

                // 2. AGORA PODE FECHAR O MODAL (Isso anula o ManifestoSelecionado)
                FecharDialogos();
                EstaCarregando = true; MensagemErro = "Enviando cancelamento...";

                // 3. USA A VARIÁVEL SALVA
                var result = await _mediator.Send(new CancelarManifestoCommand(manifestoId, justificativa));
                MensagemErro = result.Sucesso ? "✅ " + result.Mensagem : "❌ " + result.Mensagem;
                Console.WriteLine($"[AÇÃO] Resultado Cancelamento: {result.Mensagem}");
            }
            catch (Exception ex) { MensagemErro = $"❌ Erro interno: {ex.Message}"; Console.WriteLine($"[AÇÃO - ERRO] {ex}"); }
            finally { EstaCarregando = false; await CarregarHistorico(manterMensagem: true); }
        }

        [RelayCommand]
        private async Task ConfirmarInclusaoCondutor()
        {
            try
            {
                if (ManifestoSelecionado == null) return;

                // 1. SALVA OS DADOS
                var manifestoId = ManifestoSelecionado.Id;
                var numero = ManifestoSelecionado.Numero;
                var nome = NovoCondutorNome;
                var cpf = NovoCondutorCpf;

                Console.WriteLine($"[AÇÃO] Iniciando inclusão de condutor ({cpf}) no MDF-e {numero}...");

                // 2. FECHA O MODAL
                FecharDialogos();
                EstaCarregando = true; MensagemErro = "Incluindo condutor, aguarde...";

                // 3. USA AS VARIÁVEIS SALVAS NO MEDIATOR
                var result = await _mediator.Send(new IncluirCondutorManifestoCommand(manifestoId, nome, cpf));
                MensagemErro = result.Sucesso ? "✅ " + result.Mensagem : "❌ " + result.Mensagem;
                Console.WriteLine($"[AÇÃO] Resultado Inclusão Condutor: {result.Mensagem}");
            }
            catch (Exception ex) { MensagemErro = $"❌ Erro interno: {ex.Message}"; Console.WriteLine($"[AÇÃO - ERRO] {ex}"); }
            finally { EstaCarregando = false; await CarregarHistorico(manterMensagem: true); }
        }

        [RelayCommand]
        private async Task ConfirmarInclusaoDFe()
        {
            try
            {
                if (ManifestoSelecionado == null) return;

                // 1. SALVA OS DADOS
                var manifestoId = ManifestoSelecionado.Id;
                var numero = ManifestoSelecionado.Numero;
                var chave = NovaChaveDFe;
                var ibgeCarrega = NovoIbgeCarrega;
                var munCarrega = NovoMunCarrega;
                var ibgeDescarga = NovoIbgeDescarga;
                var munDescarga = NovoMunDescarga;

                Console.WriteLine($"[AÇÃO] Incluindo novo DF-e ({chave}) no MDF-e {numero}...");

                // 2. FECHA O MODAL
                FecharDialogos();
                EstaCarregando = true; MensagemErro = "Incluindo DF-e...";

                // 3. USA AS VARIÁVEIS SALVAS
                var result = await _mediator.Send(new IncluirDFeManifestoCommand(manifestoId, ibgeCarrega, munCarrega, ibgeDescarga, munDescarga, chave));
                MensagemErro = result.Sucesso ? "✅ " + result.Mensagem : "❌ " + result.Mensagem;
                Console.WriteLine($"[AÇÃO] Resultado Inclusão DF-e: {result.Mensagem}");
            }
            catch (Exception ex) { MensagemErro = $"❌ Erro interno: {ex.Message}"; Console.WriteLine($"[AÇÃO - ERRO] {ex}"); }
            finally { EstaCarregando = false; await CarregarHistorico(manterMensagem: true); }
        }
    }
}