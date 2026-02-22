using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using MDFe.Utils.Configuracoes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Core.Interfaces;
using VersaoServico = MDFe.Utils.Flags.VersaoServico;

namespace CoreMDFe.Application.Features.Configuracoes
{
    public record AplicarConfiguracaoZeusCommand() : IRequest<bool>;

    public class AplicarConfiguracaoZeusHandler : IRequestHandler<AplicarConfiguracaoZeusCommand, bool>
    {
        private readonly IAppDbContext _dbContext;

        public AplicarConfiguracaoZeusHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(AplicarConfiguracaoZeusCommand request, CancellationToken cancellationToken)
        {
            var configApp = await _dbContext.Configuracoes
                .Include(c => c.Empresa)
                .FirstOrDefaultAsync(cancellationToken);

            if (configApp == null) return false;

            // Transforma a UF salva em string para o Enum Estado do Zeus
            _ = System.Enum.TryParse(configApp.UfEmitente, out Estado estado);

            var configuracaoCertificado = new ConfiguracaoCertificado
            {
                TipoCertificado = TipoCertificado.A1Arquivo, // Ou de acordo com o que for salvo
                Arquivo = configApp.CaminhoArquivoCertificado,
                Senha = configApp.SenhaCertificado,
                ManterDadosEmCache = configApp.ManterCertificadoEmCache
            };

            // Determina o caminho dinâmico da pasta Schemas junto ao executável (Linux ou Windows)
            string diretorioBase = AppDomain.CurrentDomain.BaseDirectory;
            string caminhoSchemas = Path.Combine(diretorioBase, "Schemas");

            // Aplica ao Singleton da biblioteca do Zeus
            MDFeConfiguracao.Instancia.ConfiguracaoCertificado = configuracaoCertificado;
            MDFeConfiguracao.Instancia.CaminhoSchemas = caminhoSchemas;
            MDFeConfiguracao.Instancia.CaminhoSalvarXml = configApp.DiretorioSalvarXml;
            MDFeConfiguracao.Instancia.IsSalvarXml = configApp.IsSalvarXml;

            MDFeConfiguracao.Instancia.VersaoWebService.VersaoLayout = (VersaoServico)configApp.VersaoLayout;
            MDFeConfiguracao.Instancia.VersaoWebService.TipoAmbiente = (TipoAmbiente)configApp.TipoAmbiente;
            MDFeConfiguracao.Instancia.VersaoWebService.UfEmitente = estado;
            MDFeConfiguracao.Instancia.VersaoWebService.TimeOut = configApp.TimeOut;
            MDFeConfiguracao.Instancia.IsAdicionaQrCode = true;

            return true;
        }
    }
}