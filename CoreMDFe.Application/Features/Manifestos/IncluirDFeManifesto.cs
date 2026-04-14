using CoreMDFe.Core.Interfaces;
using DFe.Utils;
using MDFe.Classes.Informacoes.Evento.CorpoEvento;
using MDFe.Servicos.EventosMDFe;
using CoreMDFe.Application.Mediator;
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
    // Classe auxiliar para trafegar os dados na lista
    public class DtoInclusaoDFeItem
    {
        public string IbgeDescarga { get; set; } = string.Empty;
        public string MunDescarga { get; set; } = string.Empty;
        public string ChaveDFe { get; set; } = string.Empty;
        public int TipoDFe { get; set; }
    }

    // O Command agora recebe uma Lista de Documentos!
    public record IncluirDFeManifestoCommand(Guid ManifestoId, string IbgeCarrega, string MunCarrega, List<DtoInclusaoDFeItem> Documentos) : IRequest<IncluirDFeManifestoResult>;
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
            if (manifesto == null || string.IsNullOrEmpty(manifesto.XmlAssinado))
                return new IncluirDFeManifestoResult(false, "MDF-e não encontrado.");

            if (string.IsNullOrEmpty(manifesto.ProtocoloAutorizacao))
                return new IncluirDFeManifestoResult(false, "O MDF-e não possui um Protocolo de Autorização salvo.");

            if (request.Documentos == null || !request.Documentos.Any())
                return new IncluirDFeManifestoResult(false, "Nenhum documento informado para inclusão.");

            try
            {
                var docAssinado = XDocument.Parse(manifesto.XmlAssinado);
                var nodeMDFe = docAssinado.Descendants().FirstOrDefault(x => x.Name.LocalName == "MDFe");
                var mdfe = FuncoesXml.XmlStringParaClasse<MDFeEletronico>(nodeMDFe != null ? nodeMDFe.ToString() : manifesto.XmlAssinado);

                if (mdfe?.InfMDFe == null) return new IncluirDFeManifestoResult(false, "Falha na leitura do XML Assinado.");

                // 1. O PULO DO GATO: Define a sequência do evento atual somando 1 ao que está no banco
                int sequenciaEventoAtual = manifesto.SequencialEventoInclusao + 1;

                var informacoesDocumentos = new List<MDFeInfDocInc>();
                foreach (var doc in request.Documentos)
                {
                    var docInc = new MDFeInfDocInc
                    {
                        CMunDescarga = doc.IbgeDescarga,
                        XMunDescarga = doc.MunDescarga
                    };

                    if (doc.TipoDFe == 55) docInc.ChNFe = doc.ChaveDFe;
                    else if (doc.TipoDFe == 57) docInc.ChNFe = doc.ChaveDFe;

                    informacoesDocumentos.Add(docInc);
                }

                var evento = new ServicoMDFeEvento();

                // 2. Injeta a sequência calculada dinamicamente
                var retorno = evento.MDFeEventoIncluirDFe(mdfe, (byte)sequenciaEventoAtual, manifesto.ProtocoloAutorizacao, request.IbgeCarrega, request.MunCarrega, informacoesDocumentos);

                // 135 = Evento registrado e vinculado a DF-e
                if (retorno.InfEvento.CStat == 135)
                {
                    // 3. SALVA O PROGRESSO DA SEQUÊNCIA NO BANCO DE DADOS
                    manifesto.SequencialEventoInclusao = sequenciaEventoAtual;

                    _dbContext.Manifestos.Update(manifesto);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    return new IncluirDFeManifestoResult(true, "DF-e(s) incluído(s) com sucesso na Sefaz!");
                }

                // Tratamento elegante caso ocorra erro de sincronismo e a Sefaz acuse duplicidade
                if (retorno.InfEvento.CStat == 573)
                {
                    return new IncluirDFeManifestoResult(false, $"Rejeição Sefaz: Duplicidade de Evento. A Sefaz já possui um evento na sequência {sequenciaEventoAtual}. Feche e abra a tela para tentar novamente.");
                }

                return new IncluirDFeManifestoResult(false, retorno.InfEvento.XMotivo ?? "Rejeição Sefaz");
            }
            catch (Exception ex)
            {
                return new IncluirDFeManifestoResult(false, $"Erro: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}