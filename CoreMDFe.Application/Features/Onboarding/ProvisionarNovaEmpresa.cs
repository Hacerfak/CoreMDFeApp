using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Core.Entities;
using CoreMDFe.Infrastructure.Data;

namespace CoreMDFe.Application.Features.Onboarding
{
    public record ProvisionarNovaEmpresaCommand(
        string Cnpj, string RazaoSocial, string Fantasia, string Ie, string Rntrc,
        string Email, string Telefone,
        string Logradouro, string Numero, string Bairro, string Cep, string Municipio, long Ibge, string Uf,
        string CaminhoCertificadoOriginal, string SenhaCertificado) : IRequest<ProvisionarNovaEmpresaResult>;

    public record ProvisionarNovaEmpresaResult(bool Sucesso, string Mensagem, string CaminhoBanco);

    public class ProvisionarNovaEmpresaHandler : IRequestHandler<ProvisionarNovaEmpresaCommand, ProvisionarNovaEmpresaResult>
    {
        public async Task<ProvisionarNovaEmpresaResult> Handle(ProvisionarNovaEmpresaCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Cria a estrutura de pastas segura baseada no CNPJ
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var empresaFolder = Path.Combine(basePath, "CoreMDFe", request.Cnpj);
                var certsFolder = Path.Combine(empresaFolder, "Certificados");

                Directory.CreateDirectory(empresaFolder);
                Directory.CreateDirectory(certsFolder);

                // 2. Copia o certificado para a pasta isolada
                var fileName = Path.GetFileName(request.CaminhoCertificadoOriginal);
                var destinoCertificado = Path.Combine(certsFolder, fileName);

                if (request.CaminhoCertificadoOriginal != destinoCertificado)
                {
                    File.Copy(request.CaminhoCertificadoOriginal, destinoCertificado, true);
                }

                // 3. Define o caminho do Banco de Dados SQLite exclusivo desta empresa
                var dbPath = Path.Combine(empresaFolder, "empresa_data.db");

                // 4. Instancia o contexto dinâmico e aplica as Migrations (Cria o banco e as tabelas na hora)
                using var dbContext = new AppDbContext(dbPath);
                await dbContext.Database.MigrateAsync(cancellationToken);

                // 5. Salva os dados na base recém criada
                var novaEmpresa = new Empresa
                {
                    Cnpj = request.Cnpj,
                    Nome = request.RazaoSocial,
                    NomeFantasia = string.IsNullOrWhiteSpace(request.Fantasia) ? request.RazaoSocial : request.Fantasia,
                    InscricaoEstadual = request.Ie,
                    RNTRC = request.Rntrc,
                    Email = request.Email,
                    Telefone = request.Telefone,
                    Logradouro = request.Logradouro,
                    Numero = request.Numero,
                    Bairro = request.Bairro,
                    Cep = request.Cep,
                    NomeMunicipio = request.Municipio,
                    CodigoIbgeMunicipio = request.Ibge,
                    SiglaUf = request.Uf,

                    Configuracao = new ConfiguracaoApp
                    {
                        CaminhoArquivoCertificado = destinoCertificado,
                        SenhaCertificado = request.SenhaCertificado,
                        ManterCertificadoEmCache = true,
                        UfEmitente = request.Uf,
                        TipoAmbiente = 2, // Padrão Homologação
                        TimeOut = 5000
                    }
                };

                dbContext.Empresas.Add(novaEmpresa);
                await dbContext.SaveChangesAsync(cancellationToken);

                return new ProvisionarNovaEmpresaResult(true, "Empresa configurada com sucesso!", dbPath);
            }
            catch (Exception ex)
            {
                return new ProvisionarNovaEmpresaResult(false, $"Erro ao configurar empresa: {ex.Message}", "");
            }
        }
    }
}