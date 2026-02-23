using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;

namespace CoreMDFe.Application.Features.Configuracoes
{
    public record SalvarConfiguracaoCommand(
        string Cnpj, string Nome, string Fantasia, string Ie, string Rntrc,
        string CaminhoCertificado, string SenhaCertificado, bool ManterCertificadoCache,
        int TipoAmbiente, string UfEmitente, int VersaoLayout, int TimeOut,
        string RespTecCnpj, string RespTecNome, string RespTecTelefone, string RespTecEmail,
        bool GerarQrCode, int ModalidadePadrao, int TipoEmissaoPadrao, int TipoEmitentePadrao, int TipoTransportadorPadrao,
        byte[]? Logomarca // AQUI ESTÁ A LOGO
    ) : IRequest<bool>;

    public class SalvarConfiguracaoHandler : IRequestHandler<SalvarConfiguracaoCommand, bool>
    {
        private readonly IAppDbContext _dbContext;

        public SalvarConfiguracaoHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(SalvarConfiguracaoCommand request, CancellationToken cancellationToken)
        {
            var empresa = await _dbContext.Empresas
                .Include(e => e.Configuracao)
                .FirstOrDefaultAsync(cancellationToken);

            if (empresa == null)
            {
                empresa = new Empresa { Configuracao = new ConfiguracaoApp() };
                _dbContext.Empresas.Add(empresa);
            }

            // Dados da Empresa
            empresa.Cnpj = request.Cnpj;
            empresa.Nome = request.Nome;
            empresa.NomeFantasia = request.Fantasia;
            empresa.InscricaoEstadual = request.Ie;
            empresa.RNTRC = request.Rntrc;

            // Certificado e Ambiente
            empresa.Configuracao!.CaminhoArquivoCertificado = request.CaminhoCertificado;
            empresa.Configuracao.SenhaCertificado = request.SenhaCertificado;
            empresa.Configuracao.ManterCertificadoEmCache = request.ManterCertificadoCache;
            empresa.Configuracao.TipoAmbiente = request.TipoAmbiente;
            empresa.Configuracao.UfEmitente = request.UfEmitente;
            empresa.Configuracao.VersaoLayout = request.VersaoLayout;
            empresa.Configuracao.TimeOut = request.TimeOut;

            // Responsável Técnico
            empresa.Configuracao.RespTecCnpj = request.RespTecCnpj;
            empresa.Configuracao.RespTecNome = request.RespTecNome;
            empresa.Configuracao.RespTecTelefone = request.RespTecTelefone;
            empresa.Configuracao.RespTecEmail = request.RespTecEmail;

            // Padrões de Emissão
            empresa.Configuracao.GerarQrCode = request.GerarQrCode;
            empresa.Configuracao.ModalidadePadrao = request.ModalidadePadrao;
            empresa.Configuracao.TipoEmissaoPadrao = request.TipoEmissaoPadrao;
            empresa.Configuracao.TipoEmitentePadrao = request.TipoEmitentePadrao;
            empresa.Configuracao.TipoTransportadorPadrao = request.TipoTransportadorPadrao;

            // SALVANDO A LOGOMARCA NO BANCO
            empresa.Configuracao.Logomarca = request.Logomarca;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}