using MediatR;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DFe.Classes.Flags;
using DFe.Classes.Entidades;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Classes.Servicos.ConsultaCadastro;
using NFe.Servicos;
using NFe.Utils;

namespace CoreMDFe.Application.Features.Onboarding
{
    public record ConsultarDadosSefazCommand(string CaminhoCertificado, string Senha, string Uf) : IRequest<ConsultarDadosSefazResult>;

    public record ConsultarDadosSefazResult(bool Sucesso, string Mensagem, EmpresaSefazDto? DadosEmpresa);

    public record EmpresaSefazDto(string Cnpj, string RazaoSocial, string Ie, string Logradouro, string Numero, string Bairro, string Cep, string Municipio, long Ibge);

    public class ConsultarDadosSefazHandler : IRequestHandler<ConsultarDadosSefazCommand, ConsultarDadosSefazResult>
    {
        public async Task<ConsultarDadosSefazResult> Handle(ConsultarDadosSefazCommand request, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Heurística de Prevenção de Erros: Valida certificado antes de chamar a rede
                    X509Certificate2 certificado;
                    try
                    {
                        certificado = X509CertificateLoader.LoadPkcs12FromFile(request.CaminhoCertificado, request.Senha);
                    }
                    catch (Exception)
                    {
                        return new ConsultarDadosSefazResult(false, "Senha incorreta ou certificado inválido.", null);
                    }

                    // Extrai CNPJ
                    string cnpjEmitente;
                    Match match = Regex.Match(certificado.Subject, @"([0-9]{14})");
                    if (match.Success)
                    {
                        cnpjEmitente = match.Groups[1].Value;
                    }
                    else
                    {
                        return new ConsultarDadosSefazResult(false, "CNPJ não encontrado dentro do Certificado Digital.", null);
                    }

                    // Configura Zeus NFe para consulta de cadastro
                    var cfg = new ConfiguracaoServico
                    {
                        tpAmb = TipoAmbiente.Producao, // Consulta de cadastro é sempre em produção
                        tpEmis = TipoEmissao.teNormal,
                        ProtocoloDeSeguranca = System.Net.SecurityProtocolType.Tls12,
                        cUF = (Estado)Enum.Parse(typeof(Estado), request.Uf),
                        VersaoLayout = VersaoServico.Versao400,
                        ModeloDocumento = ModeloDocumento.NFe,
                        VersaoNfeConsultaCadastro = VersaoServico.Versao400,
                        DiretorioSchemas = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas"),
                        ValidarSchemas = false
                    };

                    using var servicoSefaz = new ServicosNFe(cfg, certificado);
                    var retornoSefaz = servicoSefaz.NfeConsultaCadastro(request.Uf, ConsultaCadastroTipoDocumento.Cnpj, cnpjEmitente);

                    if (retornoSefaz?.Retorno?.infCons?.infCad != null)
                    {
                        var cad = retornoSefaz.Retorno.infCons.infCad;
                        var dto = new EmpresaSefazDto(
                            Cnpj: cnpjEmitente,
                            RazaoSocial: cad.xNome ?? "",
                            Ie: cad.IE ?? "",
                            Logradouro: cad.ender?.xLgr ?? "",
                            Numero: cad.ender?.nro ?? "",
                            Bairro: cad.ender?.xBairro ?? "",
                            Cep: cad.ender?.CEP?.ToString() ?? "",
                            Municipio: cad.ender?.xMun ?? "",
                            Ibge: cad.ender?.cMun != null ? long.Parse(cad.ender.cMun) : 0
                        );

                        return new ConsultarDadosSefazResult(true, "Dados obtidos com sucesso!", dto);
                    }

                    return new ConsultarDadosSefazResult(false, retornoSefaz?.Retorno?.infCons?.xMotivo ?? "A SEFAZ não retornou os dados para este CNPJ/UF.", null);
                }
                catch (Exception ex)
                {
                    // Heurística de Recuperação de Erros: Mensagem clara do motivo da falha de rede
                    return new ConsultarDadosSefazResult(false, $"Erro na comunicação com a SEFAZ: {ex.Message}", null);
                }
            }, cancellationToken);
        }
    }
}