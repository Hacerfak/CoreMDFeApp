using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreMDFe.Core.Entities
{
    // Classe base para todas as entidades
    public abstract class EntityBase
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Representa os dados do Emitente (tag <emit> do XSD)
    /// </summary>
    public class Empresa : EntityBase
    {
        [Required, MaxLength(14)] public string Cnpj { get; set; } = string.Empty;
        [MaxLength(14)] public string InscricaoEstadual { get; set; } = string.Empty;
        [Required, MaxLength(60)] public string Nome { get; set; } = string.Empty;
        [MaxLength(60)] public string NomeFantasia { get; set; } = string.Empty;

        // Endereço
        [MaxLength(60)] public string Logradouro { get; set; } = string.Empty;
        [MaxLength(60)] public string Numero { get; set; } = string.Empty;
        [MaxLength(60)] public string Complemento { get; set; } = string.Empty;
        [MaxLength(60)] public string Bairro { get; set; } = string.Empty;
        public long CodigoIbgeMunicipio { get; set; }
        [MaxLength(60)] public string NomeMunicipio { get; set; } = string.Empty;
        [MaxLength(8)] public string Cep { get; set; } = string.Empty;
        [MaxLength(2)] public string SiglaUf { get; set; } = string.Empty;
        [MaxLength(14)] public string Telefone { get; set; } = string.Empty;
        [MaxLength(60)] public string Email { get; set; } = string.Empty;

        // Específico MDF-e
        [MaxLength(8)] public string RNTRC { get; set; } = string.Empty;

        // Relacionamentos: Uma empresa tem configurações específicas
        public ConfiguracaoApp? Configuracao { get; set; }
    }

    /// <summary>
    /// Substitui o antigo arquivo XML de configurações
    /// </summary>
    public class ConfiguracaoApp : EntityBase
    {
        public Guid EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public Empresa Empresa { get; set; } = null!;

        // Pastas e arquivos
        public string DiretorioSalvarXml { get; set; } = string.Empty;
        public string CaminhoSchemas { get; set; } = string.Empty;
        public bool IsSalvarXml { get; set; }

        // WebService (Ide do XSD)
        public int TipoAmbiente { get; set; } // 1 - Produção, 2 - Homologação
        [MaxLength(2)] public string UfEmitente { get; set; } = string.Empty;
        public int VersaoLayout { get; set; } // Enum de Versão do Zeus
        public int Serie { get; set; }
        public long UltimaNumeracao { get; set; }
        public int TimeOut { get; set; } = 5000;

        // Certificado Digital
        [MaxLength(100)] public string NumeroSerieCertificado { get; set; } = string.Empty;
        public string CaminhoArquivoCertificado { get; set; } = string.Empty;
        public string SenhaCertificado { get; set; } = string.Empty; // Em um app real, idealmente deve ser criptografada no banco
        public bool ManterCertificadoEmCache { get; set; }
    }

    /// <summary>
    /// Cadastro de Condutores (tag <condutor> no XSD)
    /// </summary>
    public class Condutor : EntityBase
    {
        [Required, MaxLength(60)] public string Nome { get; set; } = string.Empty;
        [Required, MaxLength(11)] public string Cpf { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cadastro de Veículos (Tração e Reboque - tag <veicTracao> e <veicReboque>)
    /// </summary>
    public class Veiculo : EntityBase
    {
        [Required, MaxLength(7)] public string Placa { get; set; } = string.Empty;
        [MaxLength(11)] public string Renavam { get; set; } = string.Empty;
        [MaxLength(2)] public string UfLicenciamento { get; set; } = string.Empty;

        public int TaraKg { get; set; }
        public int CapacidadeKg { get; set; }
        public int CapacidadeM3 { get; set; }

        public int TipoVeiculo { get; set; } // 0 - Tração, 1 - Reboque
        public string TipoRodado { get; set; } = string.Empty; // 01 - Truck, 02 - Toco, etc... (tpRod)
        public string TipoCarroceria { get; set; } = string.Empty; // 00 - N/A, 01 - Aberta, 02 - Fechada (tpCar)
    }

    /// <summary>
    /// Histórico de Emissão e Guarda do XML (Baseado nas tags <ide>, <protMDFe> e processamento)
    /// </summary>
    public class ManifestoEletronico : EntityBase
    {
        public Guid EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public Empresa Empresa { get; set; } = null!;

        // Identificação (Ide)
        public int Numero { get; set; }
        public int Serie { get; set; }
        public int Modelo { get; set; } = 58;
        public DateTime DataEmissao { get; set; }

        [MaxLength(2)] public string UfOrigem { get; set; } = string.Empty;
        [MaxLength(2)] public string UfDestino { get; set; } = string.Empty;

        // Modal e Emissão
        public int Modalidade { get; set; } // 1 - Rodoviário, 2 - Aéreo, 3 - Aquaviário, 4 - Ferroviário
        public int TipoEmissao { get; set; } // 1 - Normal, 2 - Contingência
        public int TipoAmbiente { get; set; } // 1 - Produção, 2 - Homologação

        // Retornos do Fisco
        [MaxLength(44)] public string ChaveAcesso { get; set; } = string.Empty;
        [MaxLength(15)] public string ProtocoloAutorizacao { get; set; } = string.Empty;
        [MaxLength(15)] public string ProtocoloEncerramento { get; set; } = string.Empty;
        [MaxLength(15)] public string ProtocoloCancelamento { get; set; } = string.Empty;
        [MaxLength(15)] public string Recibo { get; set; } = string.Empty;
        [MaxLength(3)] public string CodigoStatus { get; set; } = string.Empty; // cStat (Ex: 100 - Autorizado)
        [MaxLength(255)] public string MotivoStatus { get; set; } = string.Empty; // xMotivo

        // Situação interna do Sistema
        public StatusManifesto Status { get; set; } = StatusManifesto.EmDigitacao;

        // XMLs Completos (Guardados no banco para não depender apenas de arquivos em disco)
        public string XmlAssinado { get; set; } = string.Empty;
        public string ReciboAutorizacao { get; set; } = string.Empty;
        public string ReciboEncerramento { get; set; } = string.Empty;
        public string ReciboCancelamento { get; set; } = string.Empty;
    }

    public enum StatusManifesto
    {
        EmDigitacao,
        Assinado,
        Enviado,
        Autorizado,
        Rejeitado,
        Encerrado,
        Cancelado
    }
}