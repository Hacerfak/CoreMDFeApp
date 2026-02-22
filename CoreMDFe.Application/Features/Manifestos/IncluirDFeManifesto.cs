using CoreMDFe.Core.Interfaces;
using DFe.Utils;
using MDFe.Classes.Informacoes.Evento.CorpoEvento; // Puxando o MDFeInfDocInc
using MDFe.Servicos.EventosMDFe;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MDFeEletronico = MDFe.Classes.Informacoes.MDFe;

namespace CoreMDFe.Application.Features.Manifestos
{
    public record IncluirDFeManifestoCommand(Guid ManifestoId, string IbgeCarrega, string MunCarrega, string IbgeDescarga, string MunDescarga, string ChaveDFe) : IRequest<IncluirDFeManifestoResult>;
    public record IncluirDFeManifestoResult(bool Sucesso, string Mensagem);

    public class IncluirDFeManifestoHandler : IRequestHandler<IncluirDFeManifestoCommand, IncluirDFeManifestoResult>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IMediator _mediator;

        public IncluirDFeManifestoHandler(IAppDbContext dbContext, IMediator mediator) { _dbContext = dbContext; _mediator = mediator; }

        public async Task<IncluirDFeManifestoResult> Handle(IncluirDFeManifestoCommand request, CancellationToken cancellationToken)
        {
            await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);

            var manifesto = await _dbContext.Manifestos.FirstOrDefaultAsync(m => m.Id == request.ManifestoId, cancellationToken);
            if (manifesto == null || string.IsNullOrEmpty(manifesto.XmlAssinado)) return new IncluirDFeManifestoResult(false, "MDF-e não encontrado.");

            // LÊ DIRETO DA SUA COLUNA DO BANCO!
            if (string.IsNullOrEmpty(manifesto.ProtocoloAutorizacao))
                return new IncluirDFeManifestoResult(false, "O MDF-e não possui um Protocolo de Autorização salvo.");

            try
            {
                var docAssinado = XDocument.Parse(manifesto.XmlAssinado);
                var nodeMDFe = docAssinado.Descendants().FirstOrDefault(x => x.Name.LocalName == "MDFe");
                var mdfe = FuncoesXml.XmlStringParaClasse<MDFeEletronico>(nodeMDFe != null ? nodeMDFe.ToString() : manifesto.XmlAssinado);

                if (mdfe?.InfMDFe == null) return new IncluirDFeManifestoResult(false, "Falha na leitura do XML Assinado.");

                var informacoesDocumentos = new List<MDFeInfDocInc>
                {
                    new MDFeInfDocInc { CMunDescarga = request.IbgeDescarga, XMunDescarga = request.MunDescarga, ChNFe = request.ChaveDFe }
                };

                var evento = new ServicoMDFeEvento();
                var retorno = evento.MDFeEventoIncluirDFe(mdfe, 1, manifesto.ProtocoloAutorizacao, request.IbgeCarrega, request.MunCarrega, informacoesDocumentos);

                if (retorno.InfEvento.CStat == 135) return new IncluirDFeManifestoResult(true, "DF-e incluído com sucesso!");
                return new IncluirDFeManifestoResult(false, retorno.InfEvento.XMotivo ?? "Rejeição Sefaz");
            }
            catch (Exception ex) { return new IncluirDFeManifestoResult(false, $"Erro: {ex.InnerException?.Message ?? ex.Message}"); }
        }
    }
}