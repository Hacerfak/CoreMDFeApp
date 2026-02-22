using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using DFe.Utils;
using MDFe.Utils.Configuracoes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using CoreMDFe.Core.Interfaces;
using VersaoServico = MDFe.Utils.Flags.VersaoServico;
using System.Net.Security;
using System.Net;

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

            // 1. DESATIVAR VALIDAÇÃO DE CADEIA (ROOT CA)
            // Isso resolve o erro no Linux de "The SSL connection could not be established"
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            // Forçar TLS 1.2 explicitamente (Sefaz não aceita inferior)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Transforma a UF salva em string para o Enum Estado do Zeus
            _ = System.Enum.TryParse(configApp.UfEmitente, out Estado estado);

            // NO LINUX: Precisamos garantir que o certificado seja carregado com permissões de exportação
            // para que o HttpClient consiga usar a chave privada no handshake TLS.
            var certData = await File.ReadAllBytesAsync(configApp.CaminhoArquivoCertificado, cancellationToken);
            // Loader moderno para arquivos PKS12 (.pfx)
            using var certificadoNativo = X509CertificateLoader.LoadPkcs12(certData, configApp.SenhaCertificado,
                X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            var configuracaoCertificado = new ConfiguracaoCertificado
            {
                TipoCertificado = TipoCertificado.A1ByteArray,
                //ArrayBytesArquivo = File.ReadAllBytes(configApp.CaminhoArquivoCertificado),
                ArrayBytesArquivo = certData,
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

            MDFeConfiguracao.Instancia.VersaoWebService.VersaoLayout = VersaoServico.Versao300;
            MDFeConfiguracao.Instancia.VersaoWebService.TipoAmbiente = (TipoAmbiente)configApp.TipoAmbiente;
            MDFeConfiguracao.Instancia.VersaoWebService.UfEmitente = estado;
            MDFeConfiguracao.Instancia.VersaoWebService.TimeOut = configApp.TimeOut;
            MDFeConfiguracao.Instancia.IsAdicionaQrCode = true;

            return true;
        }
    }
}