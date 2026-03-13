using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CoreMDFe.Core.Validations
{
    public class RenavamAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var renavam = value as string;

            if (string.IsNullOrWhiteSpace(renavam))
                return ValidationResult.Success; // O [Required] lida com o vazio

            // Remove qualquer não-número (caso o usuário cole com pontos/traços)
            renavam = new string(renavam.Where(char.IsDigit).ToArray());

            // Renavam moderno tem 11 dígitos. Os antigos tinham 9.
            // Para validar a matemática, preenchemos com zeros à esquerda até formar 11.
            renavam = renavam.PadLeft(11, '0');

            if (renavam.Length != 11 || renavam.Distinct().Count() == 1)
                return new ValidationResult(ErrorMessage ?? "O Renavam informado é inválido.");

            // Cálculo do Dígito Verificador do Renavam (Módulo 11 com multiplicadores específicos)
            string renavamSemDigito = renavam.Substring(0, 10);
            string digitoInformado = renavam.Substring(10, 1);

            int soma = 0;
            int[] multiplicadores = { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            for (int i = 0; i < 10; i++)
            {
                soma += int.Parse(renavamSemDigito[i].ToString()) * multiplicadores[i];
            }

            int resto = (soma * 10) % 11;
            int digitoCalculado = resto == 10 ? 0 : resto;

            if (digitoCalculado.ToString() != digitoInformado)
                return new ValidationResult(ErrorMessage ?? "Dígito verificador do Renavam inválido.");

            return ValidationResult.Success;
        }
    }
}