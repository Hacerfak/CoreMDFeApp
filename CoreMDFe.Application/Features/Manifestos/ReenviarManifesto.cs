using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using DFe.Utils;
using MDFe.Servicos.RecepcaoMDFe;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MDFeEletronico = MDFe.Classes.Informacoes.MDFe;

namespace CoreMDFe.Application.Features.Manifestos
{
    public record ReenviarManifestoCommand(Guid ManifestoId) : IRequest<ReenviarManifestoResult>;
    public record ReenviarManifestoResult(bool Sucesso, string Mensagem);

    public class ReenviarManifestoHandler : IRequestHandler<ReenviarManifestoCommand, ReenviarManifestoResult>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IMediator _mediator;

        public ReenviarManifestoHandler(IAppDbContext dbContext, IMediator mediator)
        {
            _dbContext = dbContext;
            _mediator = mediator;
        }

        public async Task<ReenviarManifestoResult> Handle(ReenviarManifestoCommand request, CancellationToken cancellationToken)
        {
            // Garante que as configurações de certificado/serviço estão aplicadas
            await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);

            var manifesto = await _dbContext.Manifestos.FirstOrDefaultAsync(m => m.Id == request.ManifestoId, cancellationToken);
            if (manifesto == null || string.IsNullOrEmpty(manifesto.XmlAssinado))
                return new ReenviarManifestoResult(false, "MDF-e ou XML original não encontrado.");

            try
            {
                // 1. Extrai a classe MDFe do XML que já estava assinado no banco
                var doc = XDocument.Parse(manifesto.XmlAssinado);
                var nodeMDFe = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "MDFe");
                var xmlParaDesserializar = nodeMDFe != null ? nodeMDFe.ToString() : manifesto.XmlAssinado;

                var mdfe = FuncoesXml.XmlStringParaClasse<MDFeEletronico>(xmlParaDesserializar);

                // 2. Transmite novamente usando o serviço de recepção síncrona do Zeus
                var servicoRecepcao = new ServicoMDFeRecepcao();
                var retornoEnvio = servicoRecepcao.MDFeRecepcaoSinc(mdfe);

                // 3. Atualiza os dados de retorno no banco
                manifesto.ReciboAutorizacao = retornoEnvio.RetornoXmlString ?? "";
                manifesto.ProtocoloAutorizacao = retornoEnvio.ProtMdFe?.InfProt?.NProt ?? "";
                manifesto.CodigoStatus = retornoEnvio?.CStat.ToString() ?? "0";
                manifesto.MotivoStatus = retornoEnvio?.XMotivo ?? "Sem comunicação";

                // Atualiza o status conforme a resposta da SEFAZ
                manifesto.Status = manifesto.CodigoStatus == "100" ? StatusManifesto.Autorizado : StatusManifesto.Rejeitado;

                await _dbContext.SaveChangesAsync(cancellationToken);

                return new ReenviarManifestoResult(manifesto.Status == StatusManifesto.Autorizado, manifesto.MotivoStatus);
            }
            catch (Exception ex)
            {
                return new ReenviarManifestoResult(false, $"Erro no reenvio: {ex.Message}");
            }
        }
    }
}