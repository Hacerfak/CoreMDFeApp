using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using DFe.Utils;
using MDFe.Servicos.EventosMDFe;
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
    public record CancelarManifestoCommand(Guid ManifestoId, string Justificativa) : IRequest<CancelarManifestoResult>;
    public record CancelarManifestoResult(bool Sucesso, string Mensagem);

    public class CancelarManifestoHandler : IRequestHandler<CancelarManifestoCommand, CancelarManifestoResult>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IMediator _mediator;

        public CancelarManifestoHandler(IAppDbContext dbContext, IMediator mediator) { _dbContext = dbContext; _mediator = mediator; }

        public async Task<CancelarManifestoResult> Handle(CancelarManifestoCommand request, CancellationToken cancellationToken)
        {
            await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);

            var manifesto = await _dbContext.Manifestos.FirstOrDefaultAsync(m => m.Id == request.ManifestoId, cancellationToken);
            if (manifesto == null || string.IsNullOrEmpty(manifesto.XmlAssinado)) return new CancelarManifestoResult(false, "MDF-e não encontrado.");

            if (string.IsNullOrEmpty(manifesto.ProtocoloAutorizacao))
                return new CancelarManifestoResult(false, "O MDF-e não possui um Protocolo de Autorização salvo.");

            try
            {
                var docAssinado = XDocument.Parse(manifesto.XmlAssinado);
                var nodeMDFe = docAssinado.Descendants().FirstOrDefault(x => x.Name.LocalName == "MDFe");
                var mdfe = FuncoesXml.XmlStringParaClasse<MDFeEletronico>(nodeMDFe != null ? nodeMDFe.ToString() : manifesto.XmlAssinado);

                if (mdfe?.InfMDFe == null) return new CancelarManifestoResult(false, "Falha na leitura do XML Assinado.");

                var evento = new ServicoMDFeEvento();
                var retorno = evento.MDFeEventoCancelar(mdfe, 1, manifesto.ProtocoloAutorizacao, request.Justificativa);

                if (retorno.InfEvento.CStat == 135)
                {
                    manifesto.Status = StatusManifesto.Cancelado;
                    manifesto.ProtocoloCancelamento = retorno.InfEvento.NProt ?? "";

                    // SALVANDO O RECIBO DO EVENTO NO BANCO
                    manifesto.ReciboCancelamento = retorno.RetornoXmlString;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return new CancelarManifestoResult(true, "MDF-e Cancelado com Sucesso!");
                }
                return new CancelarManifestoResult(false, retorno.InfEvento.XMotivo ?? "Rejeição Sefaz");
            }
            catch (Exception ex) { return new CancelarManifestoResult(false, $"Erro: {ex.InnerException?.Message ?? ex.Message}"); }
        }
    }
}