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

    public class AutorizadosDownload
    {
        public string CNPJ { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
    }

    public class Lacre
    {
        public string Numero { get; set; } = string.Empty;
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

        [MaxLength(8)] public string RNTRC { get; set; } = string.Empty;

        public ConfiguracaoApp? Configuracao { get; set; }
    }

    public class ConfiguracaoApp : EntityBase
    {
        public Guid EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public Empresa Empresa { get; set; } = null!;

        public string DiretorioSalvarXml { get; set; } = string.Empty;
        public bool IsSalvarXml { get; set; }
        public string DiretorioSalvarPdf { get; set; } = string.Empty;

        public int TipoAmbiente { get; set; }
        [MaxLength(2)] public string UfEmitente { get; set; } = string.Empty;
        public int Serie { get; set; }
        public long UltimaNumeracao { get; set; }
        public int TimeOut { get; set; } = 5000;
        public int CodigoUnidadePesoPadrao { get; set; } = 1; // 1-KG, 2-TON


        public string CaminhoArquivoCertificado { get; set; } = string.Empty;
        public string SenhaCertificado { get; set; } = string.Empty;
        public bool ManterCertificadoEmCache { get; set; }

        public bool GerarQrCode { get; set; } = true;
        public int ModalidadePadrao { get; set; } = 1;
        public int TipoEmissaoPadrao { get; set; } = 1;
        public int TipoEmitentePadrao { get; set; } = 1;
        public int TipoTransportadorPadrao { get; set; } = 1;
        public byte[]? Logomarca { get; set; }

        // --- NOVOS PADRÕES AVANÇADOS DE EMISSÃO ---
        public Guid? VeiculoPadraoId { get; set; }
        public Guid? CondutorPadraoId { get; set; }

        // Seguro Padrão
        public int SeguroResponsavelPadrao { get; set; } = 1; // 1-Emitente, 2-Contratante
        [MaxLength(14)] public string SeguroCpfCnpjPadrao { get; set; } = string.Empty;
        [MaxLength(60)] public string SeguroNomeSeguradoraPadrao { get; set; } = string.Empty;
        [MaxLength(14)] public string SeguroCnpjSeguradoraPadrao { get; set; } = string.Empty;
        [MaxLength(20)] public string SeguroApolicePadrao { get; set; } = string.Empty;

        // Pagamento/Frete Padrão
        [MaxLength(60)] public string PagamentoNomeContratantePadrao { get; set; } = string.Empty;
        [MaxLength(14)] public string PagamentoCpfCnpjContratantePadrao { get; set; } = string.Empty;
        public int PagamentoIndicadorPadrao { get; set; } = 0; // 0-A vista, 1-A prazo
        [MaxLength(14)] public string PagamentoCnpjInstituicaoPadrao { get; set; } = string.Empty;

        // Textos Frequentes
        [MaxLength(2000)] public string InfoFiscoPadrao { get; set; } = string.Empty;
        [MaxLength(5000)] public string InfoComplementarPadrao { get; set; } = string.Empty;

        // Autorizados para Download
        public bool isAutorizadosDownload { get; set; } = false;
        public ICollection<AutorizadosDownload> AutorizadosDownload { get; set; } = new List<AutorizadosDownload>();

    }

    public class Condutor : EntityBase
    {
        [Required, MaxLength(60)] public string Nome { get; set; } = string.Empty;
        [Required, MaxLength(11)] public string Cpf { get; set; } = string.Empty;
    }

    public class Veiculo : EntityBase
    {
        [Required, MaxLength(7)] public string Placa { get; set; } = string.Empty;
        [MaxLength(11)] public string Renavam { get; set; } = string.Empty;
        [MaxLength(2)] public string UfLicenciamento { get; set; } = string.Empty;

        public int TaraKg { get; set; }
        public int CapacidadeKg { get; set; }
        public int CapacidadeM3 { get; set; }

        public int TipoVeiculo { get; set; } // 0 - Tração, 1 - Reboque
        [NotMapped] public string TipoVeiculoDescricao => TipoVeiculo == 0 ? "Tração" : "Reboque";

        [MaxLength(2)] public string TipoRodado { get; set; } = string.Empty; // 01, 02, 03...
        [MaxLength(2)] public string TipoCarroceria { get; set; } = string.Empty; // 00, 01, 02...

        // Dados do Proprietário (Se for de terceiro)
        [MaxLength(14)] public string ProprietarioCpfCnpj { get; set; } = string.Empty;
        [MaxLength(60)] public string ProprietarioNome { get; set; } = string.Empty;
        [MaxLength(8)] public string ProprietarioRNTRC { get; set; } = string.Empty;
        [MaxLength(14)] public string ProprietarioIE { get; set; } = string.Empty;
        [MaxLength(2)] public string ProprietarioUF { get; set; } = string.Empty;
        public int ProprietarioTipo { get; set; } // 0-TAC Agregado, 1-TAC Independente, 2-Outros
    }

    /// <summary>
    /// Entidade Raiz do MDF-e (Contém os totalizadores e configurações base)
    /// </summary>
    public class ManifestoEletronico : EntityBase
    {
        public Guid EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))] public Empresa Empresa { get; set; } = null!;

        // --- Identificação (Ide) ---
        public int Numero { get; set; }
        public int Serie { get; set; }
        public int Modelo { get; set; } = 58;
        public DateTime DataEmissao { get; set; }
        public DateTime DataHoraInicioViagem { get; set; }

        [MaxLength(2)] public string UfOrigem { get; set; } = string.Empty;
        [MaxLength(2)] public string UfDestino { get; set; } = string.Empty;

        public int Modalidade { get; set; } // 1 - Rodoviário
        public int TipoEmissao { get; set; } // 1 - Normal, 2 - Contingência
        public int TipoAmbiente { get; set; } // 1 - Produção, 2 - Homologação
        public int TipoEmitente { get; set; } // 1-Prestador, 2-Transp Carga Própria
        public int TipoTransportador { get; set; } // 1-ETC, 2-TAC, 3-CTC

        public bool IndicadorCarregamentoPosterior { get; set; }

        // --- Produto Predominante (prod) ---
        [MaxLength(2)] public string ProdutoTipoCarga { get; set; } = string.Empty;
        [MaxLength(120)] public string ProdutoNome { get; set; } = string.Empty;
        [MaxLength(14)] public string ProdutoEAN { get; set; } = string.Empty;
        [MaxLength(8)] public string ProdutoNCM { get; set; } = string.Empty;

        // --- Totais (tot) ---
        public int QtdCTe { get; set; }
        public int QtdNFe { get; set; }
        public int QtdMDFe { get; set; }
        public decimal ValorTotalCarga { get; set; }
        public decimal PesoTotalCarga { get; set; }
        public int CodigoUnidadePeso { get; set; } = 1; // 1-KG, 2-TON

        // --- Informações Adicionais (infAdic) ---
        [MaxLength(2000)] public string InformacoesFisco { get; set; } = string.Empty;
        [MaxLength(5000)] public string InformacoesComplementares { get; set; } = string.Empty;

        // --- Retornos e Status ---
        [MaxLength(44)] public string ChaveAcesso { get; set; } = string.Empty;
        [MaxLength(15)] public string ProtocoloAutorizacao { get; set; } = string.Empty;
        [MaxLength(15)] public string ProtocoloEncerramento { get; set; } = string.Empty;
        [MaxLength(15)] public string ProtocoloCancelamento { get; set; } = string.Empty;
        [MaxLength(15)] public string Recibo { get; set; } = string.Empty;
        [MaxLength(3)] public string CodigoStatus { get; set; } = string.Empty;
        [MaxLength(255)] public string MotivoStatus { get; set; } = string.Empty;

        public StatusManifesto Status { get; set; } = StatusManifesto.EmDigitacao;

        // XMLs Completos
        public string XmlAssinado { get; set; } = string.Empty;
        public string ReciboAutorizacao { get; set; } = string.Empty;
        public string ReciboEncerramento { get; set; } = string.Empty;
        public string ReciboCancelamento { get; set; } = string.Empty;

        // --- RELACIONAMENTOS (Hierarquia do MDF-e) ---
        public ICollection<ManifestoPercurso> Percursos { get; set; } = new List<ManifestoPercurso>();
        public ICollection<ManifestoMunicipioCarregamento> MunicipiosCarregamento { get; set; } = new List<ManifestoMunicipioCarregamento>();
        public ICollection<ManifestoMunicipioDescarregamento> MunicipiosDescarregamento { get; set; } = new List<ManifestoMunicipioDescarregamento>();
        public ICollection<ManifestoVeiculo> Veiculos { get; set; } = new List<ManifestoVeiculo>();
        public ICollection<ManifestoCondutor> Condutores { get; set; } = new List<ManifestoCondutor>();
        public ICollection<ManifestoCiot> Ciots { get; set; } = new List<ManifestoCiot>();
        public ICollection<ManifestoValePedagio> ValesPedagio { get; set; } = new List<ManifestoValePedagio>();
        public ICollection<ManifestoContratante> Contratantes { get; set; } = new List<ManifestoContratante>();
        public ICollection<ManifestoSeguro> Seguros { get; set; } = new List<ManifestoSeguro>();
        public ICollection<ManifestoPagamento> Pagamentos { get; set; } = new List<ManifestoPagamento>();
        public ICollection<ManifestoAutorizadoDownload> AutorizadosDownload { get; set; } = new List<ManifestoAutorizadoDownload>();
        public ICollection<ManifestoLacre> Lacres { get; set; } = new List<ManifestoLacre>();
    }

    // --- TABELAS FILHAS DO MANIFESTO ---

    public class ManifestoPercurso : EntityBase
    {
        public Guid ManifestoId { get; set; }
        [ForeignKey(nameof(ManifestoId))] public ManifestoEletronico Manifesto { get; set; } = null!;
        public int Ordem { get; set; }
        [MaxLength(2)] public string UF { get; set; } = string.Empty;
    }

    public class ManifestoMunicipioCarregamento : EntityBase
    {
        public Guid ManifestoId { get; set; }
        [ForeignKey(nameof(ManifestoId))] public ManifestoEletronico Manifesto { get; set; } = null!;
        public long CodigoIbge { get; set; }
        [MaxLength(60)] public string NomeMunicipio { get; set; } = string.Empty;
    }

    public class ManifestoMunicipioDescarregamento : EntityBase
    {
        public Guid ManifestoId { get; set; }
        [ForeignKey(nameof(ManifestoId))] public ManifestoEletronico Manifesto { get; set; } = null!;
        public long CodigoIbge { get; set; }
        [MaxLength(60)] public string NomeMunicipio { get; set; } = string.Empty;

        // Notas fiscais pertencem ao município onde serão descarregadas
        public ICollection<ManifestoDocumentoFiscal> Documentos { get; set; } = new List<ManifestoDocumentoFiscal>();
    }

    public class ManifestoDocumentoFiscal : EntityBase
    {
        public Guid MunicipioDescarregamentoId { get; set; }
        [ForeignKey(nameof(MunicipioDescarregamentoId))] public ManifestoMunicipioDescarregamento Municipio { get; set; } = null!;

        public int TipoDocumento { get; set; } // 55-NFe, 57-CTe
        [MaxLength(44)] public string ChaveAcesso { get; set; } = string.Empty;
        [MaxLength(44)] public string SegundoCodigoBarra { get; set; } = string.Empty; // Opcional
        public bool IndicadorReentrega { get; set; }
    }

    public class ManifestoVeiculo : EntityBase
    {
        public Guid ManifestoId { get; set; }
        [ForeignKey(nameof(ManifestoId))] public ManifestoEletronico Manifesto { get; set; } = null!;

        public Guid? VeiculoBaseId { get; set; } // Opcional: Link para o cadastro

        public int Tipo { get; set; } // 0-Tração, 1-Reboque
        [MaxLength(7)] public string Placa { get; set; } = string.Empty;
        [MaxLength(11)] public string Renavam { get; set; } = string.Empty;
        public int TaraKg { get; set; }
        public int CapacidadeKg { get; set; }
        public int CapacidadeM3 { get; set; }
        [MaxLength(2)] public string TipoRodado { get; set; } = string.Empty;
        [MaxLength(2)] public string TipoCarroceria { get; set; } = string.Empty;
        [MaxLength(2)] public string UfLicenciamento { get; set; } = string.Empty;

        // Proprietário
        [MaxLength(14)] public string PropCpfCnpj { get; set; } = string.Empty;
        [MaxLength(60)] public string PropNome { get; set; } = string.Empty;
        [MaxLength(8)] public string PropRNTRC { get; set; } = string.Empty;
        [MaxLength(14)] public string PropIE { get; set; } = string.Empty;
        [MaxLength(2)] public string PropUF { get; set; } = string.Empty;
        public int PropTipo { get; set; }
    }

    public class ManifestoCondutor : EntityBase
    {
        public Guid ManifestoId { get; set; }
        [ForeignKey(nameof(ManifestoId))] public ManifestoEletronico Manifesto { get; set; } = null!;

        public Guid? CondutorBaseId { get; set; } // Opcional: Link para o cadastro
        [MaxLength(11)] public string Cpf { get; set; } = string.Empty;
        [MaxLength(60)] public string Nome { get; set; } = string.Empty;
    }

    public class ManifestoCiot : EntityBase
    {
        public Guid ManifestoId { get; set; }
        [ForeignKey(nameof(ManifestoId))] public ManifestoEletronico Manifesto { get; set; } = null!;

        [MaxLength(12)] public string Ciot { get; set; } = string.Empty;
        [MaxLength(14)] public string CpfCnpj { get; set; } = string.Empty;
    }

    public class ManifestoValePedagio : EntityBase
    {
        public Guid ManifestoId { get; set; }
        [ForeignKey(nameof(ManifestoId))] public ManifestoEletronico Manifesto { get; set; } = null!;

        [MaxLength(14)] public string CnpjFornecedor { get; set; } = string.Empty;
        [MaxLength(14)] public string CpfCnpjPagador { get; set; } = string.Empty;
        [MaxLength(20)] public string NumeroCompra { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }

    public class ManifestoContratante : EntityBase
    {
        public Guid ManifestoId { get; set; }
        [ForeignKey(nameof(ManifestoId))] public ManifestoEletronico Manifesto { get; set; } = null!;

        [MaxLength(14)] public string CpfCnpj { get; set; } = string.Empty;
        [MaxLength(60)] public string Nome { get; set; } = string.Empty;
    }

    public class ManifestoSeguro : EntityBase
    {
        public Guid ManifestoId { get; set; }
        [ForeignKey(nameof(ManifestoId))] public ManifestoEletronico Manifesto { get; set; } = null!;

        public int Responsavel { get; set; } // 1-Emitente, 2-Contratante
        [MaxLength(14)] public string CpfCnpjResponsavel { get; set; } = string.Empty;
        [MaxLength(60)] public string NomeSeguradora { get; set; } = string.Empty;
        [MaxLength(14)] public string CnpjSeguradora { get; set; } = string.Empty;
        [MaxLength(20)] public string NumeroApolice { get; set; } = string.Empty;
        [MaxLength(40)] public string NumeroAverbacao { get; set; } = string.Empty;
    }

    public class ManifestoPagamento : EntityBase
    {
        public Guid ManifestoId { get; set; }
        [ForeignKey(nameof(ManifestoId))] public ManifestoEletronico Manifesto { get; set; } = null!;

        [MaxLength(60)] public string NomeContratante { get; set; } = string.Empty;
        [MaxLength(14)] public string CpfCnpjContratante { get; set; } = string.Empty;
        public decimal ValorTotalViagem { get; set; }
        public decimal ValorAdiantamento { get; set; }
        public int IndicadorPagamento { get; set; } // 0-A Vista, 1-A Prazo

        // Dados Bancários ou PIX
        [MaxLength(14)] public string CnpjInstituicaoPagamento { get; set; } = string.Empty;
        [MaxLength(60)] public string ChavePix { get; set; } = string.Empty;
    }

    public class ManifestoAutorizadoDownload : EntityBase
    {
        public Guid ManifestoId { get; set; }
        [ForeignKey(nameof(ManifestoId))] public ManifestoEletronico Manifesto { get; set; } = null!;
        [MaxLength(14)] public string CpfCnpj { get; set; } = string.Empty;
    }

    public class ManifestoLacre : EntityBase
    {
        public Guid ManifestoId { get; set; }
        [ForeignKey(nameof(ManifestoId))] public ManifestoEletronico Manifesto { get; set; } = null!;
        [MaxLength(20)] public string Numero { get; set; } = string.Empty;
    }

    public enum StatusManifesto
    {
        EmDigitacao, Assinado, Enviado, Autorizado, Rejeitado, Encerrado, Cancelado
    }
}