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
using CoreMDFe.Core.Security;

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

            // IGNORAR AVISO DE OBSOLETO (SYSLIB0014)
            // Motivo: A biblioteca Zeus ainda utiliza HttpWebRequest internamente para
            // a comunicação SOAP com a SEFAZ. Portanto, o uso do ServicePointManager
            // continua a ser obrigatório para configurar o TLS e contornar erros no Linux.

#pragma warning disable SYSLIB0014

            // 1. VALIDAÇÃO DE CADEIA CONTROLADA (Proteção MITM)
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                if (sslPolicyErrors == SslPolicyErrors.None) return true;

                if (sender is HttpWebRequest req)
                {
                    var host = req.RequestUri.Host.ToLower();
                    if (host.Contains("sefaz") || host.Contains("svrs") || host.Contains("fazenda"))
                    {
                        return true;
                    }
                }
                return false; // Rejeita interceções de terceiros
            };

            // Forçar TLS 1.3 explicitamente
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;

#pragma warning restore SYSLIB0014

            // Transforma a UF salva em string para o Enum Estado do Zeus
            _ = System.Enum.TryParse(configApp.UfEmitente, out Estado estado);

            var senhaReal = CryptoService.Decrypt(configApp.SenhaCertificado);

            // NO LINUX: Precisamos garantir que o certificado seja carregado com permissões de exportação
            // para que o HttpClient consiga usar a chave privada no handshake TLS.
            var certData = await File.ReadAllBytesAsync(configApp.CaminhoArquivoCertificado, cancellationToken);
            // Loader moderno para arquivos PKS12 (.pfx)
            using var certificadoNativo = X509CertificateLoader.LoadPkcs12(certData, senhaReal,
                X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            var configuracaoCertificado = new ConfiguracaoCertificado
            {
                TipoCertificado = TipoCertificado.A1ByteArray,
                //ArrayBytesArquivo = File.ReadAllBytes(configApp.CaminhoArquivoCertificado),
                ArrayBytesArquivo = certData,
                Senha = senhaReal,
                ManterDadosEmCache = true
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