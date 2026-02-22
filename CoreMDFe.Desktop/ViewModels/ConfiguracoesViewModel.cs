using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Configuracoes;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class ConfiguracoesViewModel : ObservableObject
    {
        private readonly IMediator _mediator;
        private readonly IAppDbContext _dbContext;

        [ObservableProperty] private string _cnpj = string.Empty;
        [ObservableProperty] private string _nome = string.Empty;
        [ObservableProperty] private string _fantasia = string.Empty;
        [ObservableProperty] private string _ie = string.Empty;
        [ObservableProperty] private string _rntrc = string.Empty;

        [ObservableProperty] private string _caminhoCertificado = string.Empty;
        [ObservableProperty] private string _senhaCertificado = string.Empty;
        [ObservableProperty] private bool _manterCertificadoCache;

        [ObservableProperty] private int _tipoAmbiente = 2; // Padrão Homologação
        [ObservableProperty] private string _ufEmitente = "SP";

        [ObservableProperty] private string _mensagemSistema = string.Empty;

        public ConfiguracoesViewModel(IMediator mediator, IAppDbContext dbContext)
        {
            _mediator = mediator;
            _dbContext = dbContext;
            _ = CarregarDadosAtuais();
        }

        private async Task CarregarDadosAtuais()
        {
            var empresa = await _dbContext.Empresas.Include(e => e.Configuracao).FirstOrDefaultAsync();
            if (empresa != null)
            {
                Cnpj = empresa.Cnpj;
                Nome = empresa.Nome;
                Fantasia = empresa.NomeFantasia;
                Ie = empresa.InscricaoEstadual;
                Rntrc = empresa.RNTRC;

                if (empresa.Configuracao != null)
                {
                    CaminhoCertificado = empresa.Configuracao.CaminhoArquivoCertificado;
                    SenhaCertificado = empresa.Configuracao.SenhaCertificado;
                    ManterCertificadoCache = empresa.Configuracao.ManterCertificadoEmCache;
                    TipoAmbiente = empresa.Configuracao.TipoAmbiente;
                    UfEmitente = empresa.Configuracao.UfEmitente;
                }
            }
        }

        [RelayCommand]
        public async Task SalvarConfiguracoes()
        {
            MensagemSistema = "Salvando...";

            var command = new SalvarConfiguracaoCommand(
                Cnpj, Nome, Fantasia, Ie, Rntrc,
                CaminhoCertificado, SenhaCertificado, ManterCertificadoCache,
                TipoAmbiente, UfEmitente, 0, 5000
            );

            var sucesso = await _mediator.Send(command);

            MensagemSistema = sucesso ? "Configurações atualizadas com sucesso!" : "Erro ao salvar configurações.";
        }
    }
}