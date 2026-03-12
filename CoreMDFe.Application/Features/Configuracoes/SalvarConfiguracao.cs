using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO; // Adicionado para podermos criar as pastas e copiar o arquivo
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Core.Entities;
using CoreMDFe.Core.Interfaces;
using CoreMDFe.Core.Security;

namespace CoreMDFe.Application.Features.Configuracoes
{
    public record SalvarConfiguracaoCommand(
        string Cnpj, string Nome, string Fantasia, string Ie, string Rntrc,
        string Logradouro, string NumeroEndereco, string Complemento, string Bairro,
        string NomeMunicipio, long CodigoIbgeMunicipio, string Cep, string Telefone, string Email,
        string CaminhoCertificado, string SenhaCertificado,
        int TipoAmbiente, string UfEmitente, long UltimaNumeracao, int Serie, int TimeOut,
        string RespTecCnpj, string RespTecNome, string RespTecTelefone, string RespTecEmail,
        int ModalidadePadrao, int TipoEmissaoPadrao, int TipoEmitentePadrao, int TipoTransportadorPadrao,
        byte[]? Logomarca, bool IsSalvarXml, string DiretorioSalvarXml, string DiretorioSalvarPdf, Guid? VeiculoPadraoId, Guid? CondutorPadraoId,
        string ProdutoTipoCargaPadrao, string ProdutoNomePadrao, string ProdutoEANPadrao, string ProdutoNCMPadrao,
        int SeguroResponsavelPadrao, string SeguroCpfCnpjPadrao, string SeguroNomeSeguradoraPadrao, string SeguroCnpjSeguradoraPadrao, string SeguroApolicePadrao,
        string PagamentoNomeContratantePadrao, string PagamentoCpfCnpjContratantePadrao, int PagamentoIndicadorPadrao, string PagamentoCnpjInstituicaoPadrao,
        string InfoFiscoPadrao, string InfoComplementarPadrao
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
            var empresa = await _dbContext.Empresas.Include(e => e.Configuracao).FirstOrDefaultAsync(cancellationToken);
            if (empresa == null)
            {
                empresa = new Empresa { Configuracao = new ConfiguracaoApp() };
                _dbContext.Empresas.Add(empresa);
            }

            // Dados da Empresa e Endereço
            empresa.Cnpj = request.Cnpj;
            empresa.Nome = request.Nome;
            empresa.NomeFantasia = request.Fantasia;
            empresa.InscricaoEstadual = request.Ie;
            empresa.RNTRC = request.Rntrc;
            empresa.Logradouro = request.Logradouro;
            empresa.Numero = request.NumeroEndereco;
            empresa.Complemento = request.Complemento;
            empresa.Bairro = request.Bairro;
            empresa.NomeMunicipio = request.NomeMunicipio;
            empresa.CodigoIbgeMunicipio = request.CodigoIbgeMunicipio;
            empresa.Cep = request.Cep;
            empresa.Telefone = request.Telefone;
            empresa.Email = request.Email;

            // =========================================================================
            // SEGURANÇA: RENOMEAR E PROTEGER O CERTIFICADO (Igual ao Onboarding)
            // Garante que não ficamos dependentes de um arquivo na pasta "Downloads" do cliente
            // =========================================================================
            string caminhoFinalCertificado = request.CaminhoCertificado;

            if (!string.IsNullOrWhiteSpace(request.CaminhoCertificado) && File.Exists(request.CaminhoCertificado))
            {
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var empresaFolder = Path.Combine(basePath, "CoreMDFe", request.Cnpj);
                var certsFolder = Path.Combine(empresaFolder, "Certificados");

                Directory.CreateDirectory(empresaFolder);
                Directory.CreateDirectory(certsFolder);

                var extensao = Path.GetExtension(request.CaminhoCertificado); // Mantém .pfx ou .p12
                var novoNomeCertificado = $"certificado_digital{extensao}";
                var destinoCertificado = Path.Combine(certsFolder, novoNomeCertificado);

                // Só copia se o caminho de origem for diferente do destino 
                // (Isso evita erro se o usuário clicar em "Salvar" sem ter alterado o certificado)
                if (!request.CaminhoCertificado.Equals(destinoCertificado, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(request.CaminhoCertificado, destinoCertificado, true);
                }

                // Atualizamos a variável com o caminho interno e isolado para salvar no banco
                caminhoFinalCertificado = destinoCertificado;
            }

            // Certificado e Ambiente
            empresa.Configuracao!.CaminhoArquivoCertificado = caminhoFinalCertificado;
            empresa.Configuracao.SenhaCertificado = CryptoService.Encrypt(request.SenhaCertificado);
            empresa.Configuracao.TipoAmbiente = request.TipoAmbiente;
            empresa.Configuracao.UfEmitente = request.UfEmitente;
            empresa.Configuracao.UltimaNumeracao = request.UltimaNumeracao;
            empresa.Configuracao.Serie = request.Serie;
            empresa.Configuracao.TimeOut = request.TimeOut;

            // Padrões de Emissão
            empresa.Configuracao.ModalidadePadrao = request.ModalidadePadrao;
            empresa.Configuracao.TipoEmissaoPadrao = request.TipoEmissaoPadrao;
            empresa.Configuracao.TipoEmitentePadrao = request.TipoEmitentePadrao;
            empresa.Configuracao.TipoTransportadorPadrao = request.TipoTransportadorPadrao;

            // Pastas e Logo
            empresa.Configuracao.Logomarca = request.Logomarca;
            empresa.Configuracao.IsSalvarXml = request.IsSalvarXml;
            empresa.Configuracao.DiretorioSalvarXml = request.DiretorioSalvarXml;
            empresa.Configuracao.DiretorioSalvarPdf = request.DiretorioSalvarPdf;

            empresa.Configuracao.VeiculoPadraoId = request.VeiculoPadraoId;
            empresa.Configuracao.CondutorPadraoId = request.CondutorPadraoId;

            empresa.Configuracao.ProdutoTipoCargaPadrao = request.ProdutoTipoCargaPadrao;
            empresa.Configuracao.ProdutoNomePadrao = request.ProdutoNomePadrao;
            empresa.Configuracao.ProdutoEANPadrao = request.ProdutoEANPadrao;
            empresa.Configuracao.ProdutoNCMPadrao = request.ProdutoNCMPadrao;

            empresa.Configuracao.SeguroResponsavelPadrao = request.SeguroResponsavelPadrao;
            empresa.Configuracao.SeguroCpfCnpjPadrao = request.SeguroCpfCnpjPadrao;
            empresa.Configuracao.SeguroNomeSeguradoraPadrao = request.SeguroNomeSeguradoraPadrao;
            empresa.Configuracao.SeguroCnpjSeguradoraPadrao = request.SeguroCnpjSeguradoraPadrao;
            empresa.Configuracao.SeguroApolicePadrao = request.SeguroApolicePadrao;

            empresa.Configuracao.PagamentoNomeContratantePadrao = request.PagamentoNomeContratantePadrao;
            empresa.Configuracao.PagamentoCpfCnpjContratantePadrao = request.PagamentoCpfCnpjContratantePadrao;
            empresa.Configuracao.PagamentoIndicadorPadrao = request.PagamentoIndicadorPadrao;
            empresa.Configuracao.PagamentoCnpjInstituicaoPadrao = request.PagamentoCnpjInstituicaoPadrao;

            empresa.Configuracao.InfoFiscoPadrao = request.InfoFiscoPadrao;
            empresa.Configuracao.InfoComplementarPadrao = request.InfoComplementarPadrao;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}