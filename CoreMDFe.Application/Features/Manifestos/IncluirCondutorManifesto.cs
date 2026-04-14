using CoreMDFe.Core.Interfaces;
using DFe.Utils;
using MDFe.Servicos.EventosMDFe;
using CoreMDFe.Application.Mediator;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MDFeEletronico = MDFe.Classes.Informacoes.MDFe;

namespace CoreMDFe.Application.Features.Manifestos
{
    public record IncluirCondutorManifestoCommand(Guid ManifestoId, string NomeCondutor, string CpfCondutor) : IRequest<IncluirCondutorManifestoResult>;
    public record IncluirCondutorManifestoResult(bool Sucesso, string Mensagem);

    public class IncluirCondutorManifestoHandler : IRequestHandler<IncluirCondutorManifestoCommand, IncluirCondutorManifestoResult>
    {
        private readonly IAppDbContext _dbContext;
        private readonly IMediator _mediator;

        public IncluirCondutorManifestoHandler(IAppDbContext dbContext, IMediator mediator) { _dbContext = dbContext; _mediator = mediator; }

        public async Task<IncluirCondutorManifestoResult> Handle(IncluirCondutorManifestoCommand request, CancellationToken cancellationToken)
        {
            await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);

            var manifesto = await _dbContext.Manifestos.FirstOrDefaultAsync(m => m.Id == request.ManifestoId, cancellationToken);
            if (manifesto == null) return new IncluirCondutorManifestoResult(false, "MDF-e não encontrado no banco.");

            var cpfLimpo = (request.CpfCondutor ?? "").Replace(".", "").Replace("-", "").Trim();
            var nomeLimpo = (request.NomeCondutor ?? "").Trim();

            if (string.IsNullOrEmpty(cpfLimpo) || string.IsNullOrEmpty(nomeLimpo))
                return new IncluirCondutorManifestoResult(false, "Nome e CPF do condutor são obrigatórios.");
            try
            {
                var docAssinado = XDocument.Parse(manifesto.XmlAssinado);
                var nodeMDFe = docAssinado.Descendants().FirstOrDefault(x => x.Name.LocalName == "MDFe");

                var mdfe = FuncoesXml.XmlStringParaClasse<MDFeEletronico>(nodeMDFe != null ? nodeMDFe.ToString() : manifesto.XmlAssinado);

                if (mdfe?.InfMDFe == null)
                {
                    return new IncluirCondutorManifestoResult(false, "Falha na leitura do XML. Estrutura inválida.");
                }

                var evento = new ServicoMDFeEvento();
                var retorno = evento.MDFeEventoIncluirCondutor(mdfe, 1, nomeLimpo, cpfLimpo);

                if (retorno.InfEvento.CStat == 135)
                    return new IncluirCondutorManifestoResult(true, "Condutor incluído com sucesso!");

                return new IncluirCondutorManifestoResult(false, retorno.InfEvento.XMotivo ?? "Rejeição Sefaz");
            }
            catch (Exception ex)
            {
                return new IncluirCondutorManifestoResult(false, $"Erro na montagem do evento: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}