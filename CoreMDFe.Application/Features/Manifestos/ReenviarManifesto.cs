using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using DFe.Utils;
using MDFe.Servicos.RecepcaoMDFe;
using CoreMDFe.Application.Mediator;
using Microsoft.EntityFrameworkCore;
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
            await _mediator.Send(new Configuracoes.AplicarConfiguracaoZeusCommand(), cancellationToken);

            // Carrega o manifesto e os dados da empresa para obtermos a última numeração atualizada
            var manifesto = await _dbContext.Manifestos
                .Include(m => m.Empresa)
                .ThenInclude(e => e.Configuracao)
                .FirstOrDefaultAsync(m => m.Id == request.ManifestoId, cancellationToken);

            if (manifesto == null || manifesto.Empresa?.Configuracao == null || string.IsNullOrEmpty(manifesto.XmlAssinado))
                return new ReenviarManifestoResult(false, "MDF-e ou configuração não encontrado.");

            try
            {
                var mdfe = FuncoesXml.XmlStringParaClasse<MDFeEletronico>(manifesto.XmlAssinado);

                // ====================================================================
                // LÓGICA DE ATUALIZAÇÃO DA NUMERAÇÃO NO REENVIO
                // ====================================================================
                // Garante que o documento pegará a próxima numeração válida antes de reenviar
                long novoNumero = manifesto.Empresa.Configuracao.UltimaNumeracao + 1;

                mdfe.InfMDFe.Ide.NMDF = novoNumero;
                mdfe.InfMDFe.Ide.CMDF = new Random().Next(11111111, 99999999); // Atualiza o código aleatório (Segurança)

                // IMPORTANTE: Limpamos a assinatura e o ID antigos para forçar o componente Zeus 
                // a gerar uma nova chave de acesso (baseada no novo número) e a assinar o XML novamente.
                mdfe.Signature = null;
                mdfe.InfMDFe.Id = null;

                var servicoRecepcao = new ServicoMDFeRecepcao();
                var retornoEnvio = servicoRecepcao.MDFeRecepcaoSinc(mdfe);

                manifesto.ReciboAutorizacao = retornoEnvio?.RetornoXmlString ?? "";
                manifesto.ProtocoloAutorizacao = retornoEnvio?.ProtMdFe?.InfProt?.NProt ?? "";
                manifesto.CodigoStatus = retornoEnvio?.CStat.ToString() ?? "0";
                manifesto.MotivoStatus = retornoEnvio?.XMotivo ?? "Sem comunicação";

                manifesto.Status = manifesto.CodigoStatus == "100" ? StatusManifesto.Autorizado : StatusManifesto.Rejeitado;

                // ATUALIZA AS INFORMAÇÕES DO DOCUMENTO NO BANCO (Número, Chave e XML Corrigido)
                manifesto.Numero = (int)novoNumero;
                manifesto.ChaveAcesso = mdfe.InfMDFe.Id?.Length >= 47 ? mdfe.InfMDFe.Id.Substring(4) : "";
                manifesto.XmlAssinado = retornoEnvio?.EnvioXmlString ?? "";

                // Assim como na Emissão Original, só "queima" o número se a SEFAZ autorizar de fato
                if (manifesto.Status == StatusManifesto.Autorizado)
                {
                    manifesto.Empresa.Configuracao.UltimaNumeracao = novoNumero;
                }

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