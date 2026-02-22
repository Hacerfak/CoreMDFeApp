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
    public record EncerrarManifestoCommand(Guid ManifestoId) : IRequest<EncerrarManifestoResult>;
    public record EncerrarManifestoResult(bool Sucesso, string Mensagem);

    public class EncerrarManifestoHandler : IRequestHandler<EncerrarManifestoCommand, EncerrarManifestoResult>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IMediator _mediator;

        public EncerrarManifestoHandler(IAppDbContext dbContext, IMediator mediator) { _dbContext = dbContext; _mediator = mediator; }

        public async Task<EncerrarManifestoResult> Handle(EncerrarManifestoCommand request, CancellationToken cancellationToken)
        {
            await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);

            var manifesto = await _dbContext.Manifestos.FirstOrDefaultAsync(m => m.Id == request.ManifestoId, cancellationToken);
            if (manifesto == null || string.IsNullOrEmpty(manifesto.XmlAssinado))
                return new EncerrarManifestoResult(false, "MDF-e ou XML Assinado não encontrado no banco de dados.");

            if (string.IsNullOrEmpty(manifesto.ProtocoloAutorizacao))
                return new EncerrarManifestoResult(false, "O MDF-e não possui um Protocolo de Autorização salvo.");

            try
            {
                var docAssinado = XDocument.Parse(manifesto.XmlAssinado);
                var nodeMDFe = docAssinado.Descendants().FirstOrDefault(x => x.Name.LocalName == "MDFe");
                var mdfe = FuncoesXml.XmlStringParaClasse<MDFeEletronico>(nodeMDFe != null ? nodeMDFe.ToString() : manifesto.XmlAssinado);

                if (mdfe?.InfMDFe == null) return new EncerrarManifestoResult(false, "Falha na leitura do XML Assinado.");

                var evento = new ServicoMDFeEvento();
                var retorno = evento.MDFeEventoEncerramentoMDFeEventoEncerramento(mdfe, 1, manifesto.ProtocoloAutorizacao);

                if (retorno.InfEvento.CStat == 135)
                {
                    manifesto.Status = StatusManifesto.Encerrado;
                    manifesto.ProtocoloEncerramento = retorno.InfEvento.NProt ?? "";

                    // SALVANDO O RECIBO DO EVENTO NO BANCO
                    manifesto.ReciboEncerramento = retorno.RetornoXmlString;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return new EncerrarManifestoResult(true, "MDF-e Encerrado com Sucesso!");
                }
                return new EncerrarManifestoResult(false, retorno.InfEvento.XMotivo ?? "Rejeição Sefaz");
            }
            catch (Exception ex) { return new EncerrarManifestoResult(false, $"Erro interno: {ex.InnerException?.Message ?? ex.Message}"); }
        }
    }
}