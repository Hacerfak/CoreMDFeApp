using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using DFe.Utils;
using FastReport;
using FastReport.Export.PdfSimple;
using MDFe.Classes.Retorno;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CoreMDFe.Application.Features.Manifestos
{
    public record GerarPdfManifestoCommand(Guid ManifestoId) : IRequest<GerarPdfManifestoResult>;
    public record GerarPdfManifestoResult(bool Sucesso, string Mensagem, string CaminhoPdf = "");

    public class GerarPdfManifestoHandler : IRequestHandler<GerarPdfManifestoCommand, GerarPdfManifestoResult>
    {
        private readonly IAppDbContext _dbContext;

        public GerarPdfManifestoHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<GerarPdfManifestoResult> Handle(GerarPdfManifestoCommand request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"\n[PDF] Iniciando requisição de PDF para o Manifesto ID: {request.ManifestoId}");

            var manifesto = await _dbContext.Manifestos
                .Include(m => m.Empresa)
                .ThenInclude(e => e.Configuracao)
                .FirstOrDefaultAsync(m => m.Id == request.ManifestoId, cancellationToken);

            if (manifesto == null || string.IsNullOrEmpty(manifesto.XmlAssinado))
            {
                Console.WriteLine("[PDF - ERRO] MDF-e não encontrado no banco ou não possui XML Assinado.");
                return new GerarPdfManifestoResult(false, "MDF-e não encontrado ou não assinado.");
            }

            try
            {
                Console.WriteLine("[PDF - ETAPA 1] Lendo o ReciboAutorizacao e extraindo <protMDFe>...");
                var docRetorno = XDocument.Parse(manifesto.ReciboAutorizacao);
                var protNode = docRetorno.Descendants().FirstOrDefault(x => x.Name.LocalName == "protMDFe");

                if (protNode == null)
                {
                    Console.WriteLine("[PDF - ERRO] A tag <protMDFe> não foi encontrada dentro do Recibo de Autorização.");
                    return new GerarPdfManifestoResult(false, "Protocolo de autorização não encontrado no XML.");
                }

                Console.WriteLine("[PDF - ETAPA 2] Construindo envelope <mdfeProc>...");
                string xmlProc = $"<mdfeProc versao=\"3.00\" xmlns=\"http://www.portalfiscal.inf.br/mdfe\">{manifesto.XmlAssinado}{protNode}</mdfeProc>";

                Console.WriteLine("[PDF - ETAPA 3] Desserializando a string XML para a classe MDFeProcMDFe...");
                var proc = FuncoesXml.XmlStringParaClasse<MDFeProcMDFe>(xmlProc);

                if (proc == null || proc.MDFe == null)
                {
                    Console.WriteLine("[PDF - ERRO] Falha interna do Zeus ao converter o XML de volta para objeto.");
                    return new GerarPdfManifestoResult(false, "Falha na estrutura do objeto do MDF-e.");
                }

                Console.WriteLine("[PDF - ETAPA 4] Iniciando FastReport e localizando o template FRX...");
                var report = new Report();

                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "MDFeRetrato.frx");
                Console.WriteLine($"[PDF] Buscando FRX em: {templatePath}");

                if (!File.Exists(templatePath))
                {
                    Console.WriteLine("[PDF - ERRO] Arquivo de template não encontrado no disco.");
                    return new GerarPdfManifestoResult(false, $"Template não encontrado em: {templatePath}");
                }

                report.Load(templatePath);

                Console.WriteLine("[PDF - ETAPA 5] Registrando a fonte de dados (MDFeProcMDFe) no Relatório...");
                report.RegisterData(new[] { proc }, "MDFeProcMDFe", 20);
                report.GetDataSource("MDFeProcMDFe").Enabled = true;

                Console.WriteLine("[PDF - ETAPA 6] Injetando parâmetros adicionais...");
                report.SetParameterValue("NewLine", Environment.NewLine);
                report.SetParameterValue("Tabulation", "\t");
                report.SetParameterValue("DocumentoCancelado", manifesto.Status == StatusManifesto.Cancelado);
                report.SetParameterValue("DocumentoEncerrado", manifesto.Status == StatusManifesto.Encerrado);

                string respTec = "Eder Gross Cichelero";
                report.SetParameterValue("Desenvolvedor", respTec);
                report.SetParameterValue("QuebrarLinhasObservacao", true);

                // --- INJEÇÃO DA LOGOMARCA ---
                var pictureObject = report.FindObject("poEmitLogo") as PictureObject;
                if (pictureObject != null && manifesto.Empresa?.Configuracao?.Logomarca != null && manifesto.Empresa.Configuracao.Logomarca.Length > 0)
                {
                    using (var ms = new MemoryStream(manifesto.Empresa.Configuracao.Logomarca))
                    {
                        pictureObject.Image = System.Drawing.Image.FromStream(ms);
                    }
                }

                Console.WriteLine("[PDF - ETAPA 7] Preparando o Relatório (Processamento Interno do FastReport)...");
                report.Prepare();

                // --- DEFINIÇÃO DO DIRETÓRIO DE SAÍDA ---
                Console.WriteLine("[PDF - ETAPA 8] Definindo diretório de salvamento...");
                string diretorioBase = Path.GetTempPath(); // Padrão de segurança (Temp)

                if (manifesto.Empresa?.Configuracao != null && !string.IsNullOrWhiteSpace(manifesto.Empresa.Configuracao.DiretorioSalvarPdf))
                {
                    diretorioBase = manifesto.Empresa.Configuracao.DiretorioSalvarPdf;

                    // Garante que a pasta existe. Se não existir, o sistema cria automaticamente.
                    if (!Directory.Exists(diretorioBase))
                    {
                        Directory.CreateDirectory(diretorioBase);
                    }
                }

                string nomeArquivo = $"DAMDFE_{manifesto.ChaveAcesso}.pdf";
                string caminhoFinalPdf = Path.Combine(diretorioBase, nomeArquivo);

                Console.WriteLine($"[PDF - ETAPA 9] Exportando o arquivo final para: {caminhoFinalPdf}");

                using (var pdfExport = new PDFSimpleExport())
                {
                    report.Export(pdfExport, caminhoFinalPdf);
                }

                report.Dispose();
                Console.WriteLine("[PDF] SUCESSO ABSOLUTO! Arquivo salvo e pronto para ser aberto.");

                return new GerarPdfManifestoResult(true, "PDF gerado com sucesso!", caminhoFinalPdf);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n==========================================");
                Console.WriteLine($"[PDF - EXCEPTION CRÍTICA]");
                Console.WriteLine($"Mensagem: {ex.Message}");
                Console.WriteLine($"InnerException: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
                Console.WriteLine($"==========================================\n");

                return new GerarPdfManifestoResult(false, $"Erro fatal ao gerar PDF: {ex.Message}");
            }
        }
    }
}